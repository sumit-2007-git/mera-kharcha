using System;
using System.Collections.Generic;
using System.Linq;

namespace MeraKharcha.Services
{
    public class MemberBalanceDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal TotalShare { get; set; }
        public decimal NetBalance => TotalPaid - TotalShare;
    }

    public class OptimizedTransactionDto
    {
        public string PayerUserId { get; set; } = string.Empty;
        public string PayerName { get; set; } = string.Empty;
        public string ReceiverUserId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class SettlementOptimizationResultDto
    {
        public int BeforeTransactionCount { get; set; }
        public int AfterTransactionCount => Transactions.Count;
        public decimal TotalAmountSettled => Transactions.Sum(t => t.Amount);
        public List<OptimizedTransactionDto> Transactions { get; set; } = new();
        public List<MemberBalanceDto> MemberBalances { get; set; } = new();
    }

    public interface ISettlementOptimizerService
    {
        SettlementOptimizationResultDto OptimizeSettlements(
            List<MemberBalanceDto> balances,
            int rawPairwiseDebtCount = 0);
    }

    public class SettlementOptimizerService : ISettlementOptimizerService
    {
        private const decimal Epsilon = 0.009m;

        public SettlementOptimizationResultDto OptimizeSettlements(
            List<MemberBalanceDto> balances,
            int rawPairwiseDebtCount = 0)
        {
            var result = new SettlementOptimizationResultDto
            {
                MemberBalances = balances
            };

            // Calculate debtors (net < 0) and creditors (net > 0)
            var debtors = new List<(string Id, string Name, decimal Amount)>();
            var creditors = new List<(string Id, string Name, decimal Amount)>();

            foreach (var member in balances)
            {
                var net = Math.Round(member.NetBalance, 2);
                if (net < -Epsilon)
                {
                    debtors.Add((member.UserId, member.UserName, -net)); // store as positive debt
                }
                else if (net > Epsilon)
                {
                    creditors.Add((member.UserId, member.UserName, net));
                }
            }

            // Estimate before transaction count: if not passed, pairwise debts = debtors.Count * creditors.Count
            result.BeforeTransactionCount = rawPairwiseDebtCount > 0 
                ? rawPairwiseDebtCount 
                : Math.Max(debtors.Count * creditors.Count, debtors.Count + creditors.Count);

            // Greedy Min Cash Flow matching
            while (debtors.Count > 0 && creditors.Count > 0)
            {
                // Sort to always pick largest debtor and largest creditor
                debtors = debtors.OrderByDescending(d => d.Amount).ToList();
                creditors = creditors.OrderByDescending(c => c.Amount).ToList();

                var debtor = debtors[0];
                var creditor = creditors[0];

                debtors.RemoveAt(0);
                creditors.RemoveAt(0);

                var transferAmount = Math.Round(Math.Min(debtor.Amount, creditor.Amount), 2);

                if (transferAmount > Epsilon)
                {
                    result.Transactions.Add(new OptimizedTransactionDto
                    {
                        PayerUserId = debtor.Id,
                        PayerName = debtor.Name,
                        ReceiverUserId = creditor.Id,
                        ReceiverName = creditor.Name,
                        Amount = transferAmount
                    });

                    var debtorRemaining = debtor.Amount - transferAmount;
                    var creditorRemaining = creditor.Amount - transferAmount;

                    if (debtorRemaining > Epsilon)
                    {
                        debtors.Add((debtor.Id, debtor.Name, debtorRemaining));
                    }

                    if (creditorRemaining > Epsilon)
                    {
                        creditors.Add((creditor.Id, creditor.Name, creditorRemaining));
                    }
                }
            }

            return result;
        }
    }
}
