using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MeraKharcha.Models;
using MeraKharcha.Services;

namespace MeraKharcha.ViewModels
{
    // ----------------- Settlement ViewModels -----------------
    public class MarkSettlementPaidViewModel
    {
        public int SettlementId { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverUpiId { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "UPI"; // UPI, Cash, Bank Transfer, Google Pay, PhonePe, Paytm, Other

        [Display(Name = "Transaction Reference / UTR Number")]
        public string? TransactionReference { get; set; }

        [Display(Name = "Payment Notes")]
        public string? Notes { get; set; }
    }

    public class CreateSettlementRequestViewModel
    {
        public int GroupId { get; set; }
        public string ReceiverUserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "UPI";
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
    }

    // ----------------- Recurring ViewModels -----------------
    public class CreateRecurringExpenseViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Bill Title (e.g. WiFi Bill, Flat Rent, Electricity)")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000000)]
        [Display(Name = "Estimated Amount (₹)")]
        public decimal Amount { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Required]
        [Display(Name = "Paid By Default")]
        public string PaidByUserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Split Method")]
        public SplitType SplitType { get; set; } = SplitType.Equal;

        [Required]
        [Display(Name = "Frequency")]
        public string Frequency { get; set; } = "Monthly"; // Weekly, Monthly, Yearly

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "First Due Date")]
        public DateTime NextDueDate { get; set; } = DateTime.Today.AddDays(7);

        public List<GroupMemberOptionDto> AvailableMembers { get; set; } = new();
        public List<Category> AvailableCategories { get; set; } = new();
    }

    // ----------------- Admin ViewModels -----------------
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalGroups { get; set; }
        public int TotalExpensesCount { get; set; }
        public decimal TotalExpenseVolume { get; set; }
        public int TotalSettlementsCount { get; set; }
        public decimal TotalSettledVolume { get; set; }

        public List<ApplicationUser> RecentUsers { get; set; } = new();
        public List<Group> RecentGroups { get; set; } = new();
        public List<ActivityLog> RecentLogs { get; set; } = new();
    }

    public class UserManagementItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? UpiId { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsLockedOut { get; set; }
    }
}
