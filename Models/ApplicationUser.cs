using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace MeraKharcha.Models
{
    public enum GroupRole
    {
        Member = 1,
        Admin = 2,
        Owner = 3
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? UpiId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public virtual ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
        public virtual ICollection<ExpenseParticipant> ExpenseShares { get; set; } = new List<ExpenseParticipant>();
        public virtual ICollection<InAppNotification> Notifications { get; set; } = new List<InAppNotification>();
    }
}
