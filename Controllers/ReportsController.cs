using System;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Infrastructure;
using MeraKharcha.Models;
using MeraKharcha.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBalanceService _balanceService;
        private readonly IExportService _exportService;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBalanceService balanceService,
            IExportService exportService)
        {
            _context = context;
            _userManager = userManager;
            _balanceService = balanceService;
            _exportService = exportService;
        }

        public async Task<IActionResult> GroupReport(int groupId, string format = "view")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.Expenses).ThenInclude(e => e.Category)
                .Include(g => g.Expenses).ThenInclude(e => e.PaidByUser)
                .Include(g => g.Expenses).ThenInclude(e => e.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            if (!group.Members.Any(m => m.UserId == user.Id) && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "You do not have access to this group report.";
                return RedirectToAction("Index", "Groups");
            }

            if (format.ToLowerInvariant() == "csv")
            {
                var csvBytes = _exportService.GenerateExpensesCsv(group.Expenses);
                var safeName = string.Join("_", group.Name.Split(System.IO.Path.GetInvalidFileNameChars()));
                return File(csvBytes, "text/csv", $"{safeName}_Expenses_Report_{DateTime.Today:yyyyMMdd}.csv");
            }

            var summary = await _balanceService.GetGroupFinancialSummaryAsync(groupId);
            ViewBag.Summary = summary;
            ViewBag.GeneratedAt = DateTime.Now;

            return View(group);
        }

        public async Task<IActionResult> ExportSettlementsCsv(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound();

            var settlements = await _context.Settlements
                .Include(s => s.PayerUser)
                .Include(s => s.ReceiverUser)
                .Where(s => s.GroupId == groupId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var csvBytes = _exportService.GenerateSettlementsCsv(settlements);
            var safeName = string.Join("_", group.Name.Split(System.IO.Path.GetInvalidFileNameChars()));
            return File(csvBytes, "text/csv", $"{safeName}_Settlements_{DateTime.Today:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> PersonalReport(string format = "view")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userGroupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == user.Id)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var expensesPaid = await _context.Expenses
                .Include(e => e.Group)
                .Include(e => e.Category)
                .Where(e => userGroupIds.Contains(e.GroupId) && e.PaidByUserId == user.Id)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            if (format.ToLowerInvariant() == "csv")
            {
                var csvBytes = _exportService.GenerateExpensesCsv(expensesPaid);
                return File(csvBytes, "text/csv", $"Personal_Expenses_{DateTime.Today:yyyyMMdd}.csv");
            }

            var totalShare = (await _context.ExpenseParticipants
                .Where(ep => userGroupIds.Contains(ep.Expense.GroupId) && ep.UserId == user.Id)
                .Select(ep => ep.ShareAmount)
                .ToListAsync()).Sum();

            ViewBag.TotalPaid = expensesPaid.Sum(e => e.Amount);
            ViewBag.TotalShare = totalShare;
            ViewBag.NetOverall = (decimal)ViewBag.TotalPaid - totalShare;
            ViewBag.User = user;

            return View(expensesPaid);
        }
    }
}
