using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Infrastructure;
using MeraKharcha.Models;
using MeraKharcha.Services;
using MeraKharcha.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExpenseService _expenseService;
        private readonly IFileStorageService _fileStorage;

        public ExpensesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IExpenseService expenseService,
            IFileStorageService fileStorage)
        {
            _context = context;
            _userManager = userManager;
            _expenseService = expenseService;
            _fileStorage = fileStorage;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            if (!group.Members.Any(m => m.UserId == user.Id) && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "You do not have access to this group.";
                return RedirectToAction("Index", "Groups");
            }

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var members = group.Members.Select(m => new GroupMemberOptionDto
            {
                UserId = m.UserId,
                FullName = m.User.FullName ?? m.User.UserName ?? "Member",
                AvatarUrl = m.User.AvatarUrl,
                UpiId = m.User.UpiId
            }).ToList();

            var model = new AddExpenseViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                PaidByUserId = user.Id,
                ExpenseDate = DateTime.Today,
                AvailableCategories = categories,
                AvailableMembers = members,
                SplitType = SplitType.Equal,
                Participants = members.Select(m => new ParticipantShareInputDto
                {
                    UserId = m.UserId,
                    IsIncluded = true,
                    SharesCount = 1
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddExpenseViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Handle optional receipt upload
            string? receiptUrl = null;
            if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveReceiptAsync(model.ReceiptFile);
                if (uploadResult.Success)
                {
                    receiptUrl = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("ReceiptFile", uploadResult.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                // Repopulate options
                model.AvailableCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                var group = await _context.Groups
                    .Include(g => g.Members).ThenInclude(m => m.User)
                    .FirstOrDefaultAsync(g => g.Id == model.GroupId);
                if (group != null)
                {
                    model.GroupName = group.Name;
                    model.AvailableMembers = group.Members.Select(m => new GroupMemberOptionDto
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName ?? m.User.UserName ?? "Member",
                        AvatarUrl = m.User.AvatarUrl,
                        UpiId = m.User.UpiId
                    }).ToList();
                }
                return View(model);
            }

            var dto = new CreateExpenseDto
            {
                GroupId = model.GroupId,
                Title = model.Title.Trim(),
                BaseAmount = model.BaseAmount,
                TaxAmount = model.TaxAmount,
                TipAmount = model.TipAmount,
                ServiceCharge = model.ServiceCharge,
                DiscountAmount = model.DiscountAmount,
                PaidByUserId = model.PaidByUserId,
                ExpenseDate = model.ExpenseDate,
                CategoryId = model.CategoryId,
                SplitType = model.SplitType,
                Notes = model.Notes?.Trim(),
                Tags = model.Tags?.Trim(),
                ReceiptUrl = receiptUrl,
                Participants = model.Participants
            };

            var result = await _expenseService.CreateExpenseAsync(dto, user.Id);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.AvailableCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                var group = await _context.Groups.Include(g => g.Members).ThenInclude(m => m.User).FirstOrDefaultAsync(g => g.Id == model.GroupId);
                if (group != null)
                {
                    model.GroupName = group.Name;
                    model.AvailableMembers = group.Members.Select(m => new GroupMemberOptionDto
                    {
                        UserId = m.UserId,
                        FullName = m.User.FullName ?? m.User.UserName ?? "Member",
                        AvatarUrl = m.User.AvatarUrl,
                        UpiId = m.User.UpiId
                    }).ToList();
                }
                return View(model);
            }

            TempData["SuccessMessage"] = $"Expense '{model.Title}' (₹{dto.CalculateFinalTotal():N2}) added successfully!";
            return RedirectToAction("Details", "Groups", new { id = model.GroupId, tab = "expenses" });
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var expense = await _expenseService.GetExpenseDetailsAsync(id);
            if (expense == null) return NotFound();

            var isMember = expense.Group.Members.Any(m => m.UserId == user.Id) || User.IsInRole("SystemAdmin");
            if (!isMember)
            {
                TempData["ErrorMessage"] = "You do not have access to view this expense.";
                return RedirectToAction("Index", "Groups");
            }

            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            var groupId = expense.GroupId;
            var result = await _expenseService.DeleteExpenseAsync(id, user.Id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "Groups", new { id = groupId, tab = "expenses" });
        }

        [HttpPost]
        public async Task<IActionResult> ScanReceiptMock(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No receipt image provided." });
            }

            // Simulated AI/OCR extraction logic
            await Task.Delay(400); // realistic scan delay
            var random = new Random();
            var merchants = new[] { "Cafe Coffee Day", "Haldiram's", "Nature's Basket", "Pizza Hut", "Shell Fuel Station", "Apollo Pharmacy", "Zomato Dine" };
            var selectedMerchant = merchants[random.Next(merchants.Length)];
            var baseAmt = random.Next(400, 2800);
            var taxAmt = Math.Round((decimal)baseAmt * 0.05m, 2);

            return Json(new
            {
                success = true,
                merchant = selectedMerchant,
                baseAmount = baseAmt,
                taxAmount = taxAmt,
                date = DateTime.Today.ToString("yyyy-MM-dd"),
                notes = $"Extracted via Receipt OCR from '{Path.GetFileName(file.FileName)}'"
            });
        }
    }
}
