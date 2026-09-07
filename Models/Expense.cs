using System;
using System.Collections.Generic;

namespace MeraKharcha.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        // Financial breakdowns
        public decimal Amount { get; set; } // Total final amount
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal TipAmount { get; set; } = 0;
        public decimal ServiceCharge { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;

        public string PaidByUserId { get; set; } = string.Empty;
        public virtual ApplicationUser PaidByUser { get; set; } = null!;

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public SplitType SplitType { get; set; } = SplitType.Equal;

        public string? Notes { get; set; }
        public string? Tags { get; set; }
        public string? ReceiptUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<ExpenseParticipant> Participants { get; set; } = new List<ExpenseParticipant>();
        public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
    }

    public class ExpenseParticipant
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public virtual Expense Expense { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public decimal ShareAmount { get; set; } // Net share this user is responsible for
        public decimal? Percentage { get; set; }
        public decimal? SharesCount { get; set; }
        public decimal? ExactAmount { get; set; }
    }
}
