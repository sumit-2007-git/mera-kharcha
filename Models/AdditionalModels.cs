using System;

namespace MeraKharcha.Models
{
    public class RecurringExpense
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public string PaidByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser PaidByUser { get; set; } = null!;

        public SplitType SplitType { get; set; } = SplitType.Equal;
        public string Frequency { get; set; } = "Monthly"; // Weekly, Monthly, Yearly

        public DateTime NextDueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastGeneratedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Receipt
    {
        public int Id { get; set; }
        public int? ExpenseId { get; set; }
        public virtual Expense? Expense { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public class InAppNotification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, alert
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ActivityLog
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public string Action { get; set; } = string.Empty; // ExpenseAdded, SettlementPaid, MemberJoined, etc.
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
