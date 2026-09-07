using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var notifications = await _context.InAppNotifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var list = await _context.InAppNotifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.LinkUrl,
                    n.IsRead,
                    TimeAgo = n.CreatedAt.ToString("MMM dd, HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _context.InAppNotifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .CountAsync();

            return Json(new { unreadCount, notifications = list });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.InAppNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var unread = await _context.InAppNotifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
