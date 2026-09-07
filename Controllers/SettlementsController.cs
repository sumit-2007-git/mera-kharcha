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
    public class SettlementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBalanceService _balanceService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogger _activityLogger;

        public SettlementsController(
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

        public async Task<IActionResult> Optimize(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var summary = await _balanceService.GetGroupFinancialSummaryAsync(groupId);
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound();

            ViewBag.Group = group;
            ViewBag.CurrentUserId = user.Id;
            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSettlementRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (model.Amount <= 0)
            {
                TempData["ErrorMessage"] = "Settlement amount must be greater than zero.";
                return RedirectToAction("Details", "Groups", new { id = model.GroupId, tab = "settlements" });
            }

            var receiver = await _context.Users.FindAsync(model.ReceiverUserId);
            var group = await _context.Groups.FindAsync(model.GroupId);

            if (receiver == null || group == null)
            {
                TempData["ErrorMessage"] = "Invalid settlement recipient or group.";
                return RedirectToAction("Details", "Groups", new { id = model.GroupId, tab = "settlements" });
            }

            var settlement = new Settlement
            {
                GroupId = model.GroupId,
                PayerUserId = user.Id,
                ReceiverUserId = model.ReceiverUserId,
                Amount = model.Amount,
                Status = SettlementStatus.Paid, // When initiated via pay modal, mark as Paid awaiting confirmation
                PaymentMethod = model.PaymentMethod,
                TransactionReference = model.TransactionReference,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };

            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                model.GroupId,
                user.Id,
                "SettlementRecorded",
                $"{user.FullName} paid ₹{settlement.Amount:N2} to {receiver.FullName} via {settlement.PaymentMethod}."
            );

            await _notificationService.CreateNotificationAsync(
                model.ReceiverUserId,
                "Payment Received!",
                $"{user.FullName} marked a payment of ₹{settlement.Amount:N2} as paid to you via {settlement.PaymentMethod}. Please confirm.",
                "info",
                $"/Groups/Details/{group.Id}?tab=settlements"
            );

            await _notificationService.NotifyGroupAsync(model.GroupId, "SettlementUpdated", new
            {
                settlementId = settlement.Id,
                payer = user.FullName,
                receiver = receiver.FullName,
                amount = settlement.Amount
            });

            TempData["SuccessMessage"] = $"Payment of ₹{settlement.Amount:N2} recorded and waiting for confirmation from {receiver.FullName}.";
            return RedirectToAction("Details", "Groups", new { id = model.GroupId, tab = "settlements" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var settlement = await _context.Settlements
                .Include(s => s.PayerUser)
                .Include(s => s.ReceiverUser)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null) return NotFound();

            // Only the receiver can confirm they received the money
            if (settlement.ReceiverUserId != user.Id && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "Only the payee can confirm receipt of this settlement.";
                return RedirectToAction("Details", "Groups", new { id = settlement.GroupId, tab = "settlements" });
            }

            settlement.Status = SettlementStatus.Confirmed;
            settlement.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                settlement.GroupId,
                user.Id,
                "SettlementConfirmed",
                $"{user.FullName} confirmed receiving payment of ₹{settlement.Amount:N2} from {settlement.PayerUser.FullName}."
            );

            await _notificationService.CreateNotificationAsync(
                settlement.PayerUserId,
                "Settlement Confirmed!",
                $"{user.FullName} confirmed your payment of ₹{settlement.Amount:N2}. Balances updated!",
                "success",
                $"/Groups/Details/{settlement.GroupId}?tab=settlements"
            );

            await _notificationService.NotifyGroupAsync(settlement.GroupId, "SettlementUpdated", new
            {
                settlementId = settlement.Id,
                status = "Confirmed"
            });

            TempData["SuccessMessage"] = $"Settlement of ₹{settlement.Amount:N2} successfully confirmed!";
            return RedirectToAction("Details", "Groups", new { id = settlement.GroupId, tab = "settlements" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var settlement = await _context.Settlements.FindAsync(id);
            if (settlement == null) return NotFound();

            if (settlement.PayerUserId != user.Id && settlement.ReceiverUserId != user.Id && !User.IsInRole("SystemAdmin"))
            {
                TempData["ErrorMessage"] = "You do not have permission to cancel this settlement.";
                return RedirectToAction("Details", "Groups", new { id = settlement.GroupId, tab = "settlements" });
            }

            settlement.Status = SettlementStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Settlement was cancelled.";
            return RedirectToAction("Details", "Groups", new { id = settlement.GroupId, tab = "settlements" });
        }

        public async Task<IActionResult> History(int groupId)
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

            ViewBag.Group = group;
            ViewBag.CurrentUserId = user.Id;
            return View(settlements);
        }
    }
}
