using System.Diagnostics;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalGroups = await _context.Groups.CountAsync();
            ViewBag.TotalExpensesCount = await _context.Expenses.CountAsync();
            ViewBag.TotalVolume = (await _context.Expenses.Select(e => e.Amount).ToListAsync()).Sum();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
