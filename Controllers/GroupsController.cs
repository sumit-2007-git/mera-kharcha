using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBalanceService _balanceService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogger _activityLogger;

        public GroupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBalanceService balanceService,
            INotificationService notificationService,
            IActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _balanceService = balanceService;
            _notificationService = notificationService;
            _activityLogger = activityLogger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var memberships = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Members)
                .Include(gm => gm.Group.Expenses)
                .Where(gm => gm.UserId == user.Id)
                .OrderByDescending(gm => gm.JoinedAt)
                .ToListAsync();

            var cards = new List<GroupCardViewModel>();
            foreach (var m in memberships)
            {
                var balances = await _balanceService.GetMemberBalancesAsync(m.GroupId);
                var userBal = balances.FirstOrDefault(b => b.UserId == user.Id);

                cards.Add(new GroupCardViewModel
                {
                    Id = m.GroupId,
                    Name = m.Group.Name,
                    Description = m.Group.Description,
                    ImageUrl = m.Group.ImageUrl,
                    MemberCount = m.Group.Members.Count,
                    TotalSpending = m.Group.Expenses.Sum(e => e.Amount),
                    UserNetBalance = userBal?.NetBalance ?? 0m,
                    UserRole = m.Role
                });
            }

            return View(cards);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateGroupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var inviteCode = GenerateInviteCode();

            var group = new Group
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "bi-people" : model.ImageUrl.Trim(),
                InviteCode = inviteCode,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            group.Members.Add(new GroupMember
            {
                UserId = user.Id,
                Role = GroupRole.Owner,
                JoinedAt = DateTime.UtcNow
            });

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                group.Id,
                user.Id,
                "GroupCreated",
                $"{user.FullName ?? user.UserName} created the group '{group.Name}'."
            );

            TempData["SuccessMessage"] = $"Group '{group.Name}' created successfully! Invite friends using code: {group.InviteCode}";
            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        public async Task<IActionResult> Details(int id, string tab = "overview")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.CreatedBy)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
            bool isSystemAdmin = User.IsInRole("SystemAdmin");

            if (membership == null && !isSystemAdmin)
            {
                TempData["ErrorMessage"] = "You do not have access to this group.";
                return RedirectToAction(nameof(Index));
            }

            // Financial summary and settlement optimization
            var financialSummary = await _balanceService.GetGroupFinancialSummaryAsync(id);
            var userBalance = financialSummary.MemberBalances.FirstOrDefault(b => b.UserId == user.Id)?.NetBalance ?? 0m;

            // Expenses
            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.PaidByUser)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.User)
                .Include(e => e.Receipts)
                .Where(e => e.GroupId == id)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            // Settlements
            var settlements = await _context.Settlements
                .Include(s => s.PayerUser)
                .Include(s => s.ReceiverUser)
                .Where(s => s.GroupId == id)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Recurring
            var recurring = await _context.RecurringExpenses
                .Include(r => r.Category)
                .Include(r => r.PaidByUser)
                .Where(r => r.GroupId == id)
                .OrderBy(r => r.NextDueDate)
                .ToListAsync();

            // Activity Logs
            var activityLogs = await _context.ActivityLogs
                .Include(a => a.User)
                .Where(a => a.GroupId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(25)
                .ToListAsync();

            // Categories
            var categories = await _context.Categories.ToListAsync();

            // Analytics charts data
            var catStats = expenses
                .GroupBy(e => new { e.Category?.Name, e.Category?.ColorHex })
                .Select(g => new
                {
                    Name = g.Key.Name ?? "Other",
                    Color = g.Key.ColorHex ?? "#6B7280",
                    Total = g.Sum(e => e.Amount)
                })
                .ToList();

            var memberStats = financialSummary.MemberBalances.Select(b => new
            {
                Name = b.UserName,
                Spent = b.TotalPaid,
                Share = b.TotalShare
            }).ToList();

            var vm = new GroupDetailsViewModel
            {
                Group = group,
                ActiveTab = tab.ToLowerInvariant(),
                CurrentUserId = user.Id,
                CurrentUserRole = membership?.Role ?? GroupRole.Member,
                CurrentUserNetBalance = userBalance,
                Expenses = expenses,
                Members = group.Members.ToList(),
                Balances = financialSummary.MemberBalances,
                RawPairwiseDebts = financialSummary.RawPairwiseDebts,
                OptimizationResult = financialSummary.OptimizationResult,
                Settlements = settlements,
                RecurringExpenses = recurring,
                ActivityLogs = activityLogs,
                Categories = categories,
                CategoryLabelsJson = JsonSerializer.Serialize(catStats.Select(c => c.Name)),
                CategoryDataJson = JsonSerializer.Serialize(catStats.Select(c => c.Total)),
                CategoryColorsJson = JsonSerializer.Serialize(catStats.Select(c => c.Color)),
                MemberSpendingLabelsJson = JsonSerializer.Serialize(memberStats.Select(m => m.Name)),
                MemberSpendingDataJson = JsonSerializer.Serialize(memberStats.Select(m => m.Spent))
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Join()
        {
            return View(new JoinGroupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinGroupViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            return await ProcessJoinCode(model.InviteCode.Trim().ToUpperInvariant());
        }

        [HttpGet]
        public async Task<IActionResult> JoinCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return RedirectToAction(nameof(Index));
            }

            return await ProcessJoinCode(code.Trim().ToUpperInvariant());
        }

        private async Task<IActionResult> ProcessJoinCode(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.InviteCode.ToUpper() == code);

            if (group == null)
            {
                TempData["ErrorMessage"] = $"No group found with invite code '{code}'.";
                return RedirectToAction(nameof(Join));
            }

            if (group.Members.Any(m => m.UserId == user.Id))
            {
                TempData["InfoMessage"] = $"You are already a member of '{group.Name}'.";
                return RedirectToAction(nameof(Details), new { id = group.Id });
            }

            group.Members.Add(new GroupMember
            {
                UserId = user.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                group.Id,
                user.Id,
                "MemberJoined",
                $"{user.FullName ?? user.UserName} joined the group using invite code."
            );

            await _notificationService.NotifyGroupAsync(group.Id, "MemberJoined", new
            {
                userName = user.FullName ?? user.UserName,
                message = $"{user.FullName ?? user.UserName} has joined the group!"
            });

            TempData["SuccessMessage"] = $"Successfully joined '{group.Name}'!";
            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMemberByEmail(int groupId, string email)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            var callerMembership = group.Members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (callerMembership == null && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "You do not have permission to add members.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var targetUser = await _userManager.FindByEmailAsync(email?.Trim() ?? string.Empty);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = $"User with email '{email}' was not found in the system.";
                return RedirectToAction(nameof(Details), new { id = groupId, tab = "members" });
            }

            if (group.Members.Any(m => m.UserId == targetUser.Id))
            {
                TempData["InfoMessage"] = $"{targetUser.FullName} is already a member of this group.";
                return RedirectToAction(nameof(Details), new { id = groupId, tab = "members" });
            }

            group.Members.Add(new GroupMember
            {
                UserId = targetUser.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                groupId,
                currentUser.Id,
                "MemberAdded",
                $"{currentUser.FullName} added {targetUser.FullName} to the group."
            );

            await _notificationService.CreateNotificationAsync(
                targetUser.Id,
                "Added to Group",
                $"{currentUser.FullName} added you to '{group.Name}'.",
                "success",
                $"/Groups/Details/{group.Id}"
            );

            TempData["SuccessMessage"] = $"{targetUser.FullName} was added to the group.";
            return RedirectToAction(nameof(Details), new { id = groupId, tab = "members" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBudget(int groupId, decimal budgetAmount, string budgetPeriod = "Total")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (membership == null && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "You do not have permission to modify this group.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            group.BudgetAmount = Math.Max(0, budgetAmount);
            group.BudgetPeriod = string.IsNullOrWhiteSpace(budgetPeriod) ? "Total" : budgetPeriod;
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                groupId,
                user.Id,
                "BudgetUpdated",
                $"{user.FullName ?? user.UserName} updated the group budget to ₹{group.BudgetAmount:N2} ({group.BudgetPeriod})."
            );

            TempData["SuccessMessage"] = $"Group budget updated to ₹{group.BudgetAmount:N2} successfully.";
            return RedirectToAction(nameof(Details), new { id = groupId, tab = "overview" });
        }

        private static string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}
