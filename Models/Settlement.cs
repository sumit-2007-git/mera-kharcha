using System;

namespace MeraKharcha.Models
{
    public enum SettlementStatus
    {
        Pending = 1,
        Paid = 2,
        Confirmed = 3,
        Cancelled = 4
    }

    public class Settlement
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public string PayerUserId { get; set; } = string.Empty;
        public virtual ApplicationUser PayerUser { get; set; } = null!;

        public string ReceiverUserId { get; set; } = string.Empty;
        public virtual ApplicationUser ReceiverUser { get; set; } = null!;

        public decimal Amount { get; set; }
        public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

        public string? PaymentMethod { get; set; } // Cash, UPI, Bank Transfer, Card, Other
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
