using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Services
{
    public class ParticipantShareInputDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsIncluded { get; set; } = true;
        public decimal? ExactAmount { get; set; }
        public decimal? Percentage { get; set; }
        public decimal? SharesCount { get; set; }
    }

    public class CreateExpenseDto
    {
        public int GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal TipAmount { get; set; } = 0;
        public decimal ServiceCharge { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public string PaidByUserId { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public int? CategoryId { get; set; }
        public SplitType SplitType { get; set; } = SplitType.Equal;
        public string? Notes { get; set; }
        public string? Tags { get; set; }
        public string? ReceiptUrl { get; set; }
        public List<ParticipantShareInputDto> Participants { get; set; } = new();

        public decimal CalculateFinalTotal()
        {
            return Math.Max(0, BaseAmount + TaxAmount + TipAmount + ServiceCharge - DiscountAmount);
        }
    }

    public interface IExpenseService
    {
        Task<(bool Success, string Message, Expense? Expense)> CreateExpenseAsync(CreateExpenseDto dto, string currentUserId);
        Task<(bool Success, string Message)> DeleteExpenseAsync(int expenseId, string currentUserId);
        Task<Expense?> GetExpenseDetailsAsync(int expenseId);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogger _activityLogger;

        public ExpenseService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IActivityLogger activityLogger)
        {
            _context = context;
            _notificationService = notificationService;
            _activityLogger = activityLogger;
        }

        public async Task<Expense?> GetExpenseDetailsAsync(int expenseId)
        {
            return await _context.Expenses
                .Include(e => e.Group)
                .Include(e => e.Category)
                .Include(e => e.PaidByUser)
                .Include(e => e.Receipts)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.Id == expenseId);
        }

        public async Task<(bool Success, string Message, Expense? Expense)> CreateExpenseAsync(CreateExpenseDto dto, string currentUserId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == dto.GroupId);

            if (group == null)
            {
                return (false, "Group not found.", null);
            }

            // Check if current user is a member of group
            if (!group.Members.Any(m => m.UserId == currentUserId))
            {
                return (false, "You are not an authorized member of this group.", null);
            }

            var finalTotal = dto.CalculateFinalTotal();
            if (finalTotal <= 0)
            {
                return (false, "Total expense amount must be greater than zero.", null);
            }

            var activeParticipants = dto.Participants.Where(p => p.IsIncluded).ToList();
            if (!activeParticipants.Any())
            {
                return (false, "Please select at least one participant for the expense split.", null);
            }

            var calculatedShares = new List<(string UserId, decimal Share, decimal? Pct, decimal? Shares, decimal? Exact)>();

            switch (dto.SplitType)
            {
                case SplitType.Equal:
                    var count = activeParticipants.Count;
                    var equalShare = Math.Round(finalTotal / count, 2);
                    decimal accumulated = 0;

                    for (int i = 0; i < count; i++)
                    {
                        var p = activeParticipants[i];
                        decimal share;
                        if (i == count - 1)
                        {
                            share = finalTotal - accumulated; // Absorb any rounding penny
                        }
                        else
                        {
                            share = equalShare;
                            accumulated += share;
                        }
                        calculatedShares.Add((p.UserId, share, null, null, null));
                    }
                    break;

                case SplitType.ExactAmount:
                    var sumExact = activeParticipants.Sum(p => p.ExactAmount ?? 0);
                    if (Math.Abs(sumExact - finalTotal) > 0.05m)
                    {
                        return (false, $"Sum of exact amounts ({sumExact:C}) does not match final total ({finalTotal:C}).", null);
                    }

                    foreach (var p in activeParticipants)
                    {
                        calculatedShares.Add((p.UserId, p.ExactAmount ?? 0, null, null, p.ExactAmount));
                    }
                    break;

                case SplitType.Percentage:
                    var sumPct = activeParticipants.Sum(p => p.Percentage ?? 0);
                    if (Math.Abs(sumPct - 100m) > 0.1m)
                    {
                        return (false, $"Sum of percentages ({sumPct}%) must equal 100%.", null);
                    }

                    decimal runningTotal = 0;
                    for (int i = 0; i < activeParticipants.Count; i++)
                    {
                        var p = activeParticipants[i];
                        var pct = p.Percentage ?? 0;
                        decimal share;
                        if (i == activeParticipants.Count - 1)
                        {
                            share = finalTotal - runningTotal;
                        }
                        else
                        {
                            share = Math.Round(finalTotal * (pct / 100m), 2);
                            runningTotal += share;
                        }
                        calculatedShares.Add((p.UserId, share, pct, null, null));
                    }
                    break;

                case SplitType.Shares:
                    var totalShares = activeParticipants.Sum(p => p.SharesCount ?? 1);
                    if (totalShares <= 0)
                    {
                        return (false, "Total shares must be greater than zero.", null);
                    }

                    decimal sharesRunningTotal = 0;
                    for (int i = 0; i < activeParticipants.Count; i++)
                    {
                        var p = activeParticipants[i];
                        var sh = p.SharesCount ?? 1;
                        decimal share;
                        if (i == activeParticipants.Count - 1)
                        {
                            share = finalTotal - sharesRunningTotal;
                        }
                        else
                        {
                            share = Math.Round(finalTotal * (sh / totalShares), 2);
                            sharesRunningTotal += share;
                        }
                        calculatedShares.Add((p.UserId, share, null, sh, null));
                    }
                    break;
            }

            // Create Expense entity
            var expense = new Expense
            {
                GroupId = dto.GroupId,
                Title = dto.Title,
                Amount = finalTotal,
                BaseAmount = dto.BaseAmount,
                TaxAmount = dto.TaxAmount,
                TipAmount = dto.TipAmount,
                ServiceCharge = dto.ServiceCharge,
                DiscountAmount = dto.DiscountAmount,
                PaidByUserId = dto.PaidByUserId,
                ExpenseDate = dto.ExpenseDate,
                CategoryId = dto.CategoryId,
                SplitType = dto.SplitType,
                Notes = dto.Notes,
                Tags = dto.Tags,
                ReceiptUrl = dto.ReceiptUrl,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var cs in calculatedShares)
            {
                expense.Participants.Add(new ExpenseParticipant
                {
                    UserId = cs.UserId,
                    ShareAmount = cs.Share,
                    Percentage = cs.Pct,
                    SharesCount = cs.Shares,
                    ExactAmount = cs.Exact
                });
            }

            if (!string.IsNullOrEmpty(dto.ReceiptUrl))
            {
                expense.Receipts.Add(new Receipt
                {
                    FileName = System.IO.Path.GetFileName(dto.ReceiptUrl),
                    FilePath = dto.ReceiptUrl,
                    FileSize = 1024,
                    ContentType = "image/jpeg",
                    UploadedAt = DateTime.UtcNow
                });
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Activity Log
            var paidByUser = await _context.Users.FindAsync(dto.PaidByUserId);
            var paidByName = paidByUser?.FullName ?? "Someone";
            await _activityLogger.LogAsync(
                dto.GroupId,
                currentUserId,
                "ExpenseAdded",
                $"{paidByName} added expense '{expense.Title}' of ₹{expense.Amount:N2} split {expense.SplitType}."
            );

            // Group Notification & SignalR Broadcast
            await _notificationService.NotifyGroupAsync(dto.GroupId, "ExpenseAdded", new
            {
                expenseId = expense.Id,
                title = expense.Title,
                amount = expense.Amount,
                paidBy = paidByName,
                date = expense.ExpenseDate.ToString("MMM dd, yyyy")
            });

            // In-app notifications to all group members (except creator)
            foreach (var m in group.Members.Where(m => m.UserId != currentUserId))
            {
                await _notificationService.CreateNotificationAsync(
                    m.UserId,
                    "New Expense Added",
                    $"{paidByName} added '{expense.Title}' (₹{expense.Amount:N2}) in {group.Name}.",
                    "info",
                    $"/Groups/Details/{group.Id}?tab=expenses"
                );
            }

            return (true, "Expense created successfully.", expense);
        }

        public async Task<(bool Success, string Message)> DeleteExpenseAsync(int expenseId, string currentUserId)
        {
            var expense = await _context.Expenses
                .Include(e => e.Group)
                    .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense == null)
            {
                return (false, "Expense not found.");
            }

            // Check authorization: Must be payer, group admin/owner, or system admin
            var member = expense.Group.Members.FirstOrDefault(m => m.UserId == currentUserId);
            bool isAuthorized = (expense.PaidByUserId == currentUserId) ||
                                (member != null && (member.Role == GroupRole.Owner || member.Role == GroupRole.Admin));

            if (!isAuthorized)
            {
                return (false, "You do not have permission to delete this expense.");
            }

            var groupTitle = expense.Group.Name;
            var expenseTitle = expense.Title;
            var groupId = expense.GroupId;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                groupId,
                currentUserId,
                "ExpenseDeleted",
                $"Expense '{expenseTitle}' was deleted."
            );

            await _notificationService.NotifyGroupAsync(groupId, "ExpenseDeleted", new { expenseId });

            return (true, "Expense deleted successfully.");
        }
    }
}
