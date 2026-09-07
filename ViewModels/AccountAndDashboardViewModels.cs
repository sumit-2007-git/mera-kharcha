using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MeraKharcha.Models;
using MeraKharcha.Services;

namespace MeraKharcha.ViewModels
{
    // ----------------- Account ViewModels -----------------
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = false;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "UPI ID (Optional, for instant settlements)")]
        public string? UpiId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "UPI ID")]
        public string? UpiId { get; set; }

        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ----------------- Dashboard ViewModel -----------------
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public decimal TotalExpensesPaid { get; set; }
        public decimal TotalExpenseShare { get; set; }
        public decimal YouOwe { get; set; }
        public decimal YouReceive { get; set; }
        public decimal NetBalance => YouReceive - YouOwe;
        public int ActiveGroupsCount { get; set; }
        public int PendingSettlementsCount { get; set; }

        public List<Expense> RecentExpenses { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
        public List<Settlement> PendingSettlements { get; set; } = new();
        public List<GroupCardViewModel> Groups { get; set; } = new();

        // Chart Data
        public string MonthlySpendingLabelsJson { get; set; } = "[]";
        public string MonthlySpendingDataJson { get; set; } = "[]";
        public string CategoryLabelsJson { get; set; } = "[]";
        public string CategoryDataJson { get; set; } = "[]";
        public string CategoryColorsJson { get; set; } = "[]";
    }

    public class GroupCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int MemberCount { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal UserNetBalance { get; set; }
        public GroupRole UserRole { get; set; }
    }
}
