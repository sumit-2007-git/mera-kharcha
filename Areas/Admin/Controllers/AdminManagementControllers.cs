using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Data;
using MeraKharcha.Models;
using MeraKharcha.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var list = new List<UserManagementItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var isLocked = await _userManager.IsLockedOutAsync(u);

                list.Add(new UserManagementItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber,
                    UpiId = u.UpiId,
                    Roles = roles.ToList(),
                    CreatedAt = u.CreatedAt,
                    IsLockedOut = isLocked
                });
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Unlocked account for {user.FullName}.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(10));
                TempData["SuccessMessage"] = $"Locked account for {user.FullName}.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            bool isCurrentlyAdmin = await _userManager.IsInRoleAsync(user, "SystemAdmin");
            if (isCurrentlyAdmin)
            {
                await _userManager.RemoveFromRoleAsync(user, "SystemAdmin");
                TempData["SuccessMessage"] = $"Removed SystemAdmin privileges from {user.FullName}.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "SystemAdmin");
                TempData["SuccessMessage"] = $"Granted SystemAdmin privileges to {user.FullName}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.CreatedBy)
                .Include(g => g.Members)
                .Include(g => g.Expenses)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(groups);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleArchive(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            group.IsArchived = !group.IsArchived;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Group '{group.Name}' archive status updated to: {(group.IsArchived ? "Archived" : "Active")}.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Expenses)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string icon, string colorHex)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Category name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var cat = new Category
            {
                Name = name.Trim(),
                Icon = string.IsNullOrWhiteSpace(icon) ? "bi-tag" : icon.Trim(),
                ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#3B82F6" : colorHex.Trim(),
                IsDefault = false
            };

            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{cat.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.Include(c => c.Expenses).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            if (cat.Expenses.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{cat.Name}' because {cat.Expenses.Count} expenses are assigned to it.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{cat.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Roles = "SystemAdmin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.ActivityLogs
                .Include(a => a.User)
                .Include(a => a.Group)
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}
