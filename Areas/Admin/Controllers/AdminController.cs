using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalGroups = await _context.Groups.CountAsync();
            var totalExpensesCount = await _context.Expenses.CountAsync();
            var totalExpenseVolume = (await _context.Expenses.Select(e => e.Amount).ToListAsync()).Sum();
            var totalSettlementsCount = await _context.Settlements.CountAsync();
            var totalSettledVolume = (await _context.Settlements
                .Where(s => s.Status == Models.SettlementStatus.Confirmed)
                .Select(s => s.Amount)
                .ToListAsync()).Sum();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentGroups = await _context.Groups
                .Include(g => g.CreatedBy)
                .Include(g => g.Members)
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentLogs = await _context.ActivityLogs
                .Include(a => a.User)
                .Include(a => a.Group)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalGroups = totalGroups,
                TotalExpensesCount = totalExpensesCount,
                TotalExpenseVolume = totalExpenseVolume,
                TotalSettlementsCount = totalSettlementsCount,
                TotalSettledVolume = totalSettledVolume,
                RecentUsers = recentUsers,
                RecentGroups = recentGroups,
                RecentLogs = recentLogs
            };

            return View(vm);
        }
    }
}
