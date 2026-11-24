using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using MeraKharcha.Services;
using MeraKharcha.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IBalanceService _balanceService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IBalanceService balanceService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _balanceService = balanceService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = user.Id;

            // Get user's groups
            var userGroupMembers = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Members)
                .Include(gm => gm.Group.Expenses)
                .Where(gm => gm.UserId == currentUserId && !gm.Group.IsArchived)
                .ToListAsync();

            decimal totalYouOwe = 0m;
            decimal totalYouReceive = 0m;
            var groupCards = new List<GroupCardViewModel>();

            foreach (var gm in userGroupMembers)
            {
                var gBalances = await _balanceService.GetMemberBalancesAsync(gm.GroupId);
                var userBal = gBalances.FirstOrDefault(b => b.UserId == currentUserId);
                var net = userBal?.NetBalance ?? 0m;

                if (net < 0) totalYouOwe += Math.Abs(net);
                else if (net > 0) totalYouReceive += net;

                groupCards.Add(new GroupCardViewModel
                {
                    Id = gm.GroupId,
                    Name = gm.Group.Name,
                    Description = gm.Group.Description,
                    ImageUrl = gm.Group.ImageUrl,
                    MemberCount = gm.Group.Members.Count,
                    TotalSpending = gm.Group.Expenses.Sum(e => e.Amount),
                    UserNetBalance = net,
                    UserRole = gm.Role
                });
            }

            // User's total paid and share
            var userGroupIds = userGroupMembers.Select(gm => gm.GroupId).ToList();
            var totalExpensesPaid = (await _context.Expenses
                .Where(e => userGroupIds.Contains(e.GroupId) && e.PaidByUserId == currentUserId)
                .Select(e => e.Amount)
                .ToListAsync()).Sum();

            var totalExpenseShare = (await _context.ExpenseParticipants
                .Where(ep => userGroupIds.Contains(ep.Expense.GroupId) && ep.UserId == currentUserId)
                .Select(ep => ep.ShareAmount)
                .ToListAsync()).Sum();

            // Pending Settlements
            var pendingSettlements = await _context.Settlements
                .Include(s => s.Group)
                .Include(s => s.PayerUser)
                .Include(s => s.ReceiverUser)
                .Where(s => userGroupIds.Contains(s.GroupId) && 
                           (s.PayerUserId == currentUserId || s.ReceiverUserId == currentUserId) && 
                           s.Status == SettlementStatus.Pending)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Recent Expenses
            var recentExpenses = await _context.Expenses
                .Include(e => e.Group)
                .Include(e => e.Category)
                .Include(e => e.PaidByUser)
                .Where(e => userGroupIds.Contains(e.GroupId))
                .OrderByDescending(e => e.ExpenseDate)
                .Take(6)
                .ToListAsync();

            // Recent Activities
            var recentActivities = await _context.ActivityLogs
                .Include(a => a.Group)
                .Include(a => a.User)
                .Where(a => a.GroupId.HasValue && userGroupIds.Contains(a.GroupId.Value))
                .OrderByDescending(a => a.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Monthly spending chart data (Past 6 months)
            var now = DateTime.UtcNow;
            var monthlyLabels = new List<string>();
            var monthlyData = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var label = monthDate.ToString("MMM yyyy");
                monthlyLabels.Add(label);

                var sum = (await _context.Expenses
                    .Where(e => userGroupIds.Contains(e.GroupId) &&
                                e.ExpenseDate.Year == monthDate.Year &&
                                e.ExpenseDate.Month == monthDate.Month)
                    .Select(e => e.Amount)
                    .ToListAsync()).Sum();

                monthlyData.Add(sum);
            }

            // Category breakdown chart data
            var userExpenses = await _context.Expenses
                .Where(e => userGroupIds.Contains(e.GroupId))
                .Select(e => new
                {
                    CategoryName = e.Category != null ? e.Category.Name : "Uncategorized",
                    ColorHex = e.Category != null ? e.Category.ColorHex : "#6B7280",
                    e.Amount
                })
                .ToListAsync();

            var categoryStats = userExpenses
                .GroupBy(e => new { e.CategoryName, e.ColorHex })
                .Select(g => new
                {
                    Category = g.Key.CategoryName,
                    Color = g.Key.ColorHex,
                    Total = g.Sum(e => e.Amount)
                })
                .OrderByDescending(x => x.Total)
                .Take(6)
                .ToList();

            var catLabels = categoryStats.Select(c => c.Category).ToList();
            var catData = categoryStats.Select(c => c.Total).ToList();
            var catColors = categoryStats.Select(c => c.Color).ToList();

            if (!catLabels.Any())
            {
                catLabels.Add("No Expenses Yet");
                catData.Add(1);
                catColors.Add("#9CA3AF");
            }

            var vm = new DashboardViewModel
            {
                UserName = user.FullName ?? user.UserName ?? "Friend",
                TotalExpensesPaid = totalExpensesPaid,
                TotalExpenseShare = totalExpenseShare,
                YouOwe = totalYouOwe,
                YouReceive = totalYouReceive,
                ActiveGroupsCount = userGroupMembers.Count,
                PendingSettlementsCount = pendingSettlements.Count,
                RecentExpenses = recentExpenses,
                RecentActivities = recentActivities,
                PendingSettlements = pendingSettlements,
                Groups = groupCards,
                MonthlySpendingLabelsJson = JsonSerializer.Serialize(monthlyLabels),
                MonthlySpendingDataJson = JsonSerializer.Serialize(monthlyData),
                CategoryLabelsJson = JsonSerializer.Serialize(catLabels),
                CategoryDataJson = JsonSerializer.Serialize(catData),
                CategoryColorsJson = JsonSerializer.Serialize(catColors)
            };

            return View(vm);
        }
    }
}
