using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Services
{
    public class UserDebtMatrixDto
    {
        public string FromUserId { get; set; } = string.Empty;
        public string FromUserName { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;
        public string ToUserName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class GroupFinancialSummaryDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal TotalGroupSpending { get; set; }
        public int TotalExpenseCount { get; set; }
        public List<MemberBalanceDto> MemberBalances { get; set; } = new();
        public List<UserDebtMatrixDto> RawPairwiseDebts { get; set; } = new();
        public SettlementOptimizationResultDto OptimizationResult { get; set; } = new();
        public decimal TotalPendingSettlements { get; set; }

        // Budget properties
        public decimal BudgetAmount { get; set; }
        public string BudgetPeriod { get; set; } = "Total";
        public decimal BudgetPercentageUsed => BudgetAmount > 0 ? Math.Round((TotalGroupSpending / BudgetAmount) * 100, 1) : 0;
        public decimal BudgetRemaining => BudgetAmount > 0 ? Math.Max(0, BudgetAmount - TotalGroupSpending) : 0;
        public bool IsOverBudget => BudgetAmount > 0 && TotalGroupSpending > BudgetAmount;
        public bool IsNearBudgetLimit => BudgetAmount > 0 && TotalGroupSpending >= (BudgetAmount * 0.8m) && !IsOverBudget;
    }

    public interface IBalanceService
    {
        Task<GroupFinancialSummaryDto> GetGroupFinancialSummaryAsync(int groupId);
        Task<List<MemberBalanceDto>> GetMemberBalancesAsync(int groupId);
        Task<decimal> GetUserPersonalNetBalanceAsync(string userId);
    }

    public class BalanceService : IBalanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettlementOptimizerService _optimizer;

        public BalanceService(ApplicationDbContext context, ISettlementOptimizerService optimizer)
        {
            _context = context;
            _optimizer = optimizer;
        }

        public async Task<List<MemberBalanceDto>> GetMemberBalancesAsync(int groupId)
        {
            var members = await _context.GroupMembers
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == groupId)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Include(e => e.Participants)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();

            // Settlements that are Paid or Confirmed
            var settlements = await _context.Settlements
                .Where(s => s.GroupId == groupId && (s.Status == SettlementStatus.Confirmed || s.Status == SettlementStatus.Paid))
                .ToListAsync();

            var balances = new List<MemberBalanceDto>();

            foreach (var gm in members)
            {
                var expensePaid = expenses.Where(e => e.PaidByUserId == gm.UserId).Sum(e => e.Amount);
                var settlementPaid = settlements.Where(s => s.PayerUserId == gm.UserId).Sum(s => s.Amount);

                var expenseShare = expenses
                    .SelectMany(e => e.Participants)
                    .Where(p => p.UserId == gm.UserId)
                    .Sum(p => p.ShareAmount);
                var settlementReceived = settlements.Where(s => s.ReceiverUserId == gm.UserId).Sum(s => s.Amount);

                balances.Add(new MemberBalanceDto
                {
                    UserId = gm.UserId,
                    UserName = gm.User.FullName ?? gm.User.UserName ?? "User",
                    TotalPaid = expensePaid + settlementPaid,
                    TotalShare = expenseShare + settlementReceived
                });
            }

            return balances;
        }

        public async Task<GroupFinancialSummaryDto> GetGroupFinancialSummaryAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            var balances = await GetMemberBalancesAsync(groupId);
            var expenses = await _context.Expenses
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Include(e => e.PaidByUser)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();

            var pendingSettlements = (await _context.Settlements
                .Where(s => s.GroupId == groupId && s.Status == SettlementStatus.Pending)
                .Select(s => s.Amount)
                .ToListAsync()).Sum();

            // Calculate pairwise direct debts without optimization to showcase "before"
            var pairwiseDebts = new List<UserDebtMatrixDto>();
            var pairwiseMap = new Dictionary<(string Payer, string Receiver), decimal>();

            foreach (var exp in expenses)
            {
                foreach (var p in exp.Participants)
                {
                    if (p.UserId != exp.PaidByUserId && p.ShareAmount > 0)
                    {
                        var key = (p.UserId, exp.PaidByUserId);
                        if (!pairwiseMap.ContainsKey(key))
                        {
                            pairwiseMap[key] = 0;
                        }
                        pairwiseMap[key] += p.ShareAmount;
                    }
                }
            }

            // Adjust by completed settlements between pairs
            var confirmedSettlements = await _context.Settlements
                .Where(s => s.GroupId == groupId && (s.Status == SettlementStatus.Confirmed || s.Status == SettlementStatus.Paid))
                .ToListAsync();

            foreach (var set in confirmedSettlements)
            {
                var key = (set.PayerUserId, set.ReceiverUserId);
                if (pairwiseMap.ContainsKey(key))
                {
                    pairwiseMap[key] -= set.Amount;
                    if (pairwiseMap[key] <= 0) pairwiseMap.Remove(key);
                }
            }

            var memberLookup = balances.ToDictionary(b => b.UserId, b => b.UserName);
            foreach (var kvp in pairwiseMap.Where(k => k.Value > 0.01m))
            {
                pairwiseDebts.Add(new UserDebtMatrixDto
                {
                    FromUserId = kvp.Key.Payer,
                    FromUserName = memberLookup.TryGetValue(kvp.Key.Payer, out var fName) ? fName : "User",
                    ToUserId = kvp.Key.Receiver,
                    ToUserName = memberLookup.TryGetValue(kvp.Key.Receiver, out var tName) ? tName : "User",
                    Amount = Math.Round(kvp.Value, 2)
                });
            }

            var optimizationResult = _optimizer.OptimizeSettlements(balances, pairwiseDebts.Count);

            return new GroupFinancialSummaryDto
            {
                GroupId = groupId,
                GroupName = group?.Name ?? "Group",
                TotalGroupSpending = expenses.Sum(e => e.Amount),
                TotalExpenseCount = expenses.Count,
                MemberBalances = balances,
                RawPairwiseDebts = pairwiseDebts,
                OptimizationResult = optimizationResult,
                TotalPendingSettlements = pendingSettlements,
                BudgetAmount = group?.BudgetAmount ?? 0m,
                BudgetPeriod = group?.BudgetPeriod ?? "Total"
            };
        }

        public async Task<decimal> GetUserPersonalNetBalanceAsync(string userId)
        {
            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            decimal totalNet = 0;
            foreach (var gId in userGroupIds)
            {
                var balances = await GetMemberBalancesAsync(gId);
                var userBal = balances.FirstOrDefault(b => b.UserId == userId);
                if (userBal != null)
                {
                    totalNet += userBal.NetBalance;
                }
            }

            return totalNet;
        }
    }
}
