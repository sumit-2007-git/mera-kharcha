using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MeraKharcha.Models;
using MeraKharcha.Services;
using Microsoft.AspNetCore.Http;

namespace MeraKharcha.ViewModels
{
    // ----------------- Group ViewModels -----------------
    public class CreateGroupViewModel
    {
        [Required(ErrorMessage = "Group name is required")]
        [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters")]
        [Display(Name = "Group Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description / Purpose")]
        public string? Description { get; set; }

        [Display(Name = "Group Avatar Icon / URL")]
        public string? ImageUrl { get; set; }
    }

    public class JoinGroupViewModel
    {
        [Required(ErrorMessage = "Invite code is required")]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "Invite code is typically 6-8 characters")]
        [Display(Name = "6-Digit Group Invite Code")]
        public string InviteCode { get; set; } = string.Empty;
    }

    public class GroupDetailsViewModel
    {
        public Group Group { get; set; } = null!;
        public string ActiveTab { get; set; } = "overview";
        public string CurrentUserId { get; set; } = string.Empty;
        public GroupRole CurrentUserRole { get; set; }
        public decimal CurrentUserNetBalance { get; set; }

        public List<Expense> Expenses { get; set; } = new();
        public List<GroupMember> Members { get; set; } = new();
        public List<MemberBalanceDto> Balances { get; set; } = new();
        public List<UserDebtMatrixDto> RawPairwiseDebts { get; set; } = new();
        public SettlementOptimizationResultDto OptimizationResult { get; set; } = new();
        public List<Settlement> Settlements { get; set; } = new();
        public List<RecurringExpense> RecurringExpenses { get; set; } = new();
        public List<ActivityLog> ActivityLogs { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Analytics JSON
        public string CategoryLabelsJson { get; set; } = "[]";
        public string CategoryDataJson { get; set; } = "[]";
        public string CategoryColorsJson { get; set; } = "[]";
        public string MemberSpendingLabelsJson { get; set; } = "[]";
        public string MemberSpendingDataJson { get; set; } = "[]";
    }

    // ----------------- Expense ViewModels -----------------
    public class AddExpenseViewModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expense title is required")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 10000000.00, ErrorMessage = "Amount must be greater than zero")]
        [Display(Name = "Base Amount (₹)")]
        public decimal BaseAmount { get; set; }

        [Range(0, 1000000, ErrorMessage = "Tax must be positive")]
        [Display(Name = "Tax (₹)")]
        public decimal TaxAmount { get; set; } = 0;

        [Range(0, 1000000, ErrorMessage = "Tip must be positive")]
        [Display(Name = "Tip / Gratuity (₹)")]
        public decimal TipAmount { get; set; } = 0;

        [Range(0, 1000000, ErrorMessage = "Service charge must be positive")]
        [Display(Name = "Service Charge (₹)")]
        public decimal ServiceCharge { get; set; } = 0;

        [Range(0, 1000000, ErrorMessage = "Discount must be positive")]
        [Display(Name = "Discount (₹)")]
        public decimal DiscountAmount { get; set; } = 0;

        // Multi-currency support
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "INR";

        [Display(Name = "Exchange Rate to INR")]
        public decimal ExchangeRate { get; set; } = 1.0m;

        [Display(Name = "Original Foreign Amount")]
        public decimal OriginalAmount { get; set; } = 0;

        [Required(ErrorMessage = "Paid by is required")]
        [Display(Name = "Paid By")]
        public string PaidByUserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Expense Date")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Required]
        [Display(Name = "Split Method")]
        public SplitType SplitType { get; set; } = SplitType.Equal;

        public string? Notes { get; set; }
        public string? Tags { get; set; }

        [Display(Name = "Upload Receipt (Image / PDF)")]
        public IFormFile? ReceiptFile { get; set; }

        public List<GroupMemberOptionDto> AvailableMembers { get; set; } = new();
        public List<Category> AvailableCategories { get; set; } = new();
        public List<ParticipantShareInputDto> Participants { get; set; } = new();
    }

    public class GroupMemberOptionDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? UpiId { get; set; }
    }
}
