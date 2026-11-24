using System.Diagnostics;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                // If cookie exists but user not in DB, clear stale cookie!
                await _signInManager.SignOutAsync();
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
