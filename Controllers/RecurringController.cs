using System;
using System.Linq;
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
    public class RecurringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExpenseService _expenseService;

        public RecurringController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IExpenseService expenseService)
        {
            _context = context;
            _userManager = userManager;
            _expenseService = expenseService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == user.Id)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var list = await _context.RecurringExpenses
                .Include(r => r.Group)
                .Include(r => r.Category)
                .Include(r => r.PaidByUser)
                .Where(r => userGroupIds.Contains(r.GroupId))
                .OrderBy(r => r.NextDueDate)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var members = group.Members.Select(m => new GroupMemberOptionDto
            {
                UserId = m.UserId,
                FullName = m.User.FullName ?? m.User.UserName ?? "Member",
                AvatarUrl = m.User.AvatarUrl,
                UpiId = m.User.UpiId
            }).ToList();

            var model = new CreateRecurringExpenseViewModel
            {
                GroupId = groupId,
                GroupName = group.Name,
                AvailableCategories = categories,
                AvailableMembers = members
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecurringExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var group = await _context.Groups.Include(g => g.Members).ThenInclude(m => m.User).FirstOrDefaultAsync(g => g.Id == model.GroupId);
                model.AvailableCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                if (group != null)
                {
                    model.GroupName = group.Name;
                    model.AvailableMembers = group.Members.Select(m => new GroupMemberOptionDto
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName ?? m.User.UserName ?? "Member"
                    }).ToList();
                }
                return View(model);
            }

            var recurring = new RecurringExpense
            {
                GroupId = model.GroupId,
                Title = model.Title.Trim(),
                Amount = model.Amount,
                CategoryId = model.CategoryId,
                PaidByUserId = model.PaidByUserId,
                SplitType = model.SplitType,
                Frequency = model.Frequency,
                NextDueDate = model.NextDueDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RecurringExpenses.Add(recurring);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Recurring expense '{recurring.Title}' set up successfully!";
            return RedirectToAction("Details", "Groups", new { id = model.GroupId, tab = "overview" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateExpenseNow(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var recurring = await _context.RecurringExpenses
                .Include(r => r.Group).ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recurring == null) return NotFound();

            var participants = recurring.Group.Members.Select(m => new ParticipantShareInputDto
            {
                UserId = m.UserId,
                IsIncluded = true,
                SharesCount = 1
            }).ToList();

            var dto = new CreateExpenseDto
            {
                GroupId = recurring.GroupId,
                Title = $"{recurring.Title} ({DateTime.Today:MMM yyyy})",
                BaseAmount = recurring.Amount,
                PaidByUserId = recurring.PaidByUserId,
                ExpenseDate = DateTime.Today,
                CategoryId = recurring.CategoryId,
                SplitType = recurring.SplitType,
                Notes = $"Auto-generated from recurring template '{recurring.Title}' (Frequency: {recurring.Frequency})",
                Participants = participants
            };

            var result = await _expenseService.CreateExpenseAsync(dto, user.Id);
            if (result.Success)
            {
                recurring.LastGeneratedDate = DateTime.UtcNow;
                if (recurring.Frequency == "Weekly") recurring.NextDueDate = recurring.NextDueDate.AddDays(7);
                else if (recurring.Frequency == "Yearly") recurring.NextDueDate = recurring.NextDueDate.AddYears(1);
                else recurring.NextDueDate = recurring.NextDueDate.AddMonths(1);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Expense successfully posted for '{recurring.Title}'! Next due date updated to {recurring.NextDueDate:MMM dd, yyyy}.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
