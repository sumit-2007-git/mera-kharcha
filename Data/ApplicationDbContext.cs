using System;
using MeraKharcha.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<ExpenseParticipant> ExpenseParticipants { get; set; } = null!;
        public DbSet<Settlement> Settlements { get; set; } = null!;
        public DbSet<RecurringExpense> RecurringExpenses { get; set; } = null!;
        public DbSet<Receipt> Receipts { get; set; } = null!;
        public DbSet<InAppNotification> InAppNotifications { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Group configuration
            builder.Entity<Group>(b =>
            {
                b.HasKey(g => g.Id);
                b.HasIndex(g => g.InviteCode).IsUnique();
                b.HasOne(g => g.CreatedBy)
                    .WithMany()
                    .HasForeignKey(g => g.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // GroupMember configuration
            builder.Entity<GroupMember>(b =>
            {
                b.HasKey(gm => gm.Id);
                b.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();

                b.HasOne(gm => gm.Group)
                    .WithMany(g => g.Members)
                    .HasForeignKey(gm => gm.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(gm => gm.User)
                    .WithMany(u => u.GroupMemberships)
                    .HasForeignKey(gm => gm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Expense configuration
            builder.Entity<Expense>(b =>
            {
                b.HasKey(e => e.Id);

                b.Property(e => e.Amount).HasPrecision(18, 2);
                b.Property(e => e.BaseAmount).HasPrecision(18, 2);
                b.Property(e => e.TaxAmount).HasPrecision(18, 2);
                b.Property(e => e.TipAmount).HasPrecision(18, 2);
                b.Property(e => e.ServiceCharge).HasPrecision(18, 2);
                b.Property(e => e.DiscountAmount).HasPrecision(18, 2);

                b.HasOne(e => e.Group)
                    .WithMany(g => g.Expenses)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(e => e.PaidByUser)
                    .WithMany(u => u.ExpensesPaid)
                    .HasForeignKey(e => e.PaidByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(e => e.Category)
                    .WithMany(c => c.Expenses)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ExpenseParticipant configuration
            builder.Entity<ExpenseParticipant>(b =>
            {
                b.HasKey(ep => ep.Id);

                b.Property(ep => ep.ShareAmount).HasPrecision(18, 2);
                b.Property(ep => ep.Percentage).HasPrecision(18, 2);
                b.Property(ep => ep.SharesCount).HasPrecision(18, 2);
                b.Property(ep => ep.ExactAmount).HasPrecision(18, 2);

                b.HasOne(ep => ep.Expense)
                    .WithMany(e => e.Participants)
                    .HasForeignKey(ep => ep.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(ep => ep.User)
                    .WithMany(u => u.ExpenseShares)
                    .HasForeignKey(ep => ep.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Settlement configuration
            builder.Entity<Settlement>(b =>
            {
                b.HasKey(s => s.Id);
                b.Property(s => s.Amount).HasPrecision(18, 2);

                b.HasOne(s => s.Group)
                    .WithMany(g => g.Settlements)
                    .HasForeignKey(s => s.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(s => s.PayerUser)
                    .WithMany()
                    .HasForeignKey(s => s.PayerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(s => s.ReceiverUser)
                    .WithMany()
                    .HasForeignKey(s => s.ReceiverUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RecurringExpense configuration
            builder.Entity<RecurringExpense>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Amount).HasPrecision(18, 2);

                b.HasOne(r => r.Group)
                    .WithMany(g => g.RecurringExpenses)
                    .HasForeignKey(r => r.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(r => r.PaidByUser)
                    .WithMany()
                    .HasForeignKey(r => r.PaidByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(r => r.Category)
                    .WithMany()
                    .HasForeignKey(r => r.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Receipt configuration
            builder.Entity<Receipt>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasOne(r => r.Expense)
                    .WithMany(e => e.Receipts)
                    .HasForeignKey(r => r.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // InAppNotification configuration
            builder.Entity<InAppNotification>(b =>
            {
                b.HasKey(n => n.Id);
                b.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ActivityLog configuration
            builder.Entity<ActivityLog>(b =>
            {
                b.HasKey(a => a.Id);
                b.HasOne(a => a.Group)
                    .WithMany(g => g.ActivityLogs)
                    .HasForeignKey(a => a.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
