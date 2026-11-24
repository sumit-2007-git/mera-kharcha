using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeraKharcha.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeraKharcha.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure Database is created
            await context.Database.EnsureCreatedAsync();

            // Ensure new schema columns exist in SQLite database
            await EnsureSchemaColumnsAsync(context);

            // 1. Seed Roles
            string[] roles = { "SystemAdmin", "GroupAdmin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            var adminUser = await userManager.FindByEmailAsync("admin@merakharcha.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@merakharcha.com",
                    Email = "admin@merakharcha.com",
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    UpiId = "admin@merakharcha",
                    AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=200&q=80",
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRolesAsync(adminUser, new[] { "SystemAdmin", "User" });
                }
            }

            var rahul = await userManager.FindByEmailAsync("rahul@merakharcha.com");
            if (rahul == null)
            {
                rahul = new ApplicationUser
                {
                    UserName = "rahul@merakharcha.com",
                    Email = "rahul@merakharcha.com",
                    EmailConfirmed = true,
                    FullName = "Rahul Sharma",
                    UpiId = "rahul@okhdfcbank",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=200&q=80",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };
                await userManager.CreateAsync(rahul, "User@123");
                await userManager.AddToRoleAsync(rahul, "User");
            }

            var priya = await userManager.FindByEmailAsync("priya@merakharcha.com");
            if (priya == null)
            {
                priya = new ApplicationUser
                {
                    UserName = "priya@merakharcha.com",
                    Email = "priya@merakharcha.com",
                    EmailConfirmed = true,
                    FullName = "Priya Patel",
                    UpiId = "priya@okaxis",
                    AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=200&q=80",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };
                await userManager.CreateAsync(priya, "User@123");
                await userManager.AddToRoleAsync(priya, "User");
            }

            var amit = await userManager.FindByEmailAsync("amit@merakharcha.com");
            if (amit == null)
            {
                amit = new ApplicationUser
                {
                    UserName = "amit@merakharcha.com",
                    Email = "amit@merakharcha.com",
                    EmailConfirmed = true,
                    FullName = "Amit Verma",
                    UpiId = "amit@ybl",
                    AvatarUrl = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?auto=format&fit=crop&w=200&q=80",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                };
                await userManager.CreateAsync(amit, "User@123");
                await userManager.AddToRoleAsync(amit, "User");
            }

            var sneha = await userManager.FindByEmailAsync("sneha@merakharcha.com");
            if (sneha == null)
            {
                sneha = new ApplicationUser
                {
                    UserName = "sneha@merakharcha.com",
                    Email = "sneha@merakharcha.com",
                    EmailConfirmed = true,
                    FullName = "Sneha Reddy",
                    UpiId = "sneha@paytm",
                    AvatarUrl = "https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=200&q=80",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                };
                await userManager.CreateAsync(sneha, "User@123");
                await userManager.AddToRoleAsync(sneha, "User");
            }

            // 3. Seed Default Categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Food & Dining", Icon = "bi-cup-hot", ColorHex = "#EF4444", IsDefault = true },
                    new Category { Name = "Travel & Transport", Icon = "bi-airplane", ColorHex = "#3B82F6", IsDefault = true },
                    new Category { Name = "Rent & Stay", Icon = "bi-house-door", ColorHex = "#8B5CF6", IsDefault = true },
                    new Category { Name = "Utilities & Bills", Icon = "bi-lightning-charge", ColorHex = "#F59E0B", IsDefault = true },
                    new Category { Name = "Groceries & Supplies", Icon = "bi-cart", ColorHex = "#10B981", IsDefault = true },
                    new Category { Name = "Entertainment & Movies", Icon = "bi-film", ColorHex = "#EC4899", IsDefault = true },
                    new Category { Name = "Shopping", Icon = "bi-bag", ColorHex = "#06B6D4", IsDefault = true },
                    new Category { Name = "General / Other", Icon = "bi-tag", ColorHex = "#6B7280", IsDefault = true }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            var catFood = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Food & Dining");
            var catTravel = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Travel & Transport");
            var catRent = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Rent & Stay");
            var catUtil = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Utilities & Bills");
            var catGroceries = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Groceries & Supplies");
            var catEnt = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Entertainment & Movies");

            // 4. Seed Demo Groups
            if (!context.Groups.Any())
            {
                var goaGroup = new Group
                {
                    Name = "Goa Trip 2026 🌴",
                    Description = "Annual beach vacation with friends: Villa, food, scooters & water sports.",
                    ImageUrl = "bi-compass",
                    InviteCode = "GOA2026",
                    CreatedById = rahul.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    IsArchived = false
                };

                goaGroup.Members.Add(new GroupMember { UserId = rahul.Id, Role = GroupRole.Owner, JoinedAt = DateTime.UtcNow.AddDays(-15) });
                goaGroup.Members.Add(new GroupMember { UserId = priya.Id, Role = GroupRole.Admin, JoinedAt = DateTime.UtcNow.AddDays(-15) });
                goaGroup.Members.Add(new GroupMember { UserId = amit.Id, Role = GroupRole.Member, JoinedAt = DateTime.UtcNow.AddDays(-14) });
                goaGroup.Members.Add(new GroupMember { UserId = sneha.Id, Role = GroupRole.Member, JoinedAt = DateTime.UtcNow.AddDays(-14) });

                var flatGroup = new Group
                {
                    Name = "Apartment 402 Flatmates 🏠",
                    Description = "Monthly shared rent, groceries, electricity, and house maid expenses.",
                    ImageUrl = "bi-buildings",
                    InviteCode = "FLAT402",
                    CreatedById = priya.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1),
                    IsArchived = false
                };

                flatGroup.Members.Add(new GroupMember { UserId = priya.Id, Role = GroupRole.Owner, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
                flatGroup.Members.Add(new GroupMember { UserId = rahul.Id, Role = GroupRole.Member, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
                flatGroup.Members.Add(new GroupMember { UserId = amit.Id, Role = GroupRole.Member, JoinedAt = DateTime.UtcNow.AddMonths(-1) });

                context.Groups.AddRange(goaGroup, flatGroup);
                await context.SaveChangesAsync();

                // 5. Seed Expenses for Goa Group
                // Expense 1: Villa Booking (Equal split)
                var expVilla = new Expense
                {
                    GroupId = goaGroup.Id,
                    Title = "Luxury Villa Booking (Calangute)",
                    BaseAmount = 14000m,
                    TaxAmount = 2000m,
                    DiscountAmount = 1000m,
                    Amount = 15000m, // 14000 + 2000 - 1000
                    PaidByUserId = rahul.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-10),
                    CategoryId = catRent?.Id,
                    SplitType = SplitType.Equal,
                    Notes = "Paid 3 nights advance for Calangute Beachfront Villa",
                    Tags = "villa,stay,calangute",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                };
                expVilla.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 3750m });
                expVilla.Participants.Add(new ExpenseParticipant { UserId = priya.Id, ShareAmount = 3750m });
                expVilla.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 3750m });
                expVilla.Participants.Add(new ExpenseParticipant { UserId = sneha.Id, ShareAmount = 3750m });

                // Expense 2: Seafood Shack Dinner (Equal split)
                var expDinner = new Expense
                {
                    GroupId = goaGroup.Id,
                    Title = "Fisherman's Wharf Seafood Feast",
                    BaseAmount = 4500m,
                    ServiceCharge = 450m,
                    TipAmount = 250m,
                    Amount = 5200m,
                    PaidByUserId = priya.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-8),
                    CategoryId = catFood?.Id,
                    SplitType = SplitType.Equal,
                    Notes = "Includes fresh tiger prawns, curry, drinks and dessert.",
                    Tags = "dinner,food,seafood",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                };
                expDinner.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 1300m });
                expDinner.Participants.Add(new ExpenseParticipant { UserId = priya.Id, ShareAmount = 1300m });
                expDinner.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 1300m });
                expDinner.Participants.Add(new ExpenseParticipant { UserId = sneha.Id, ShareAmount = 1300m });

                // Expense 3: Scooters Rental (Equal split)
                var expScooters = new Expense
                {
                    GroupId = goaGroup.Id,
                    Title = "Activa Scooters Rental (4 bikes x 3 days)",
                    BaseAmount = 4800m,
                    Amount = 4800m,
                    PaidByUserId = amit.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-6),
                    CategoryId = catTravel?.Id,
                    SplitType = SplitType.Equal,
                    Notes = "4 scooters hired near Mapusa bus terminal",
                    Tags = "scooter,travel",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                };
                expScooters.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 1200m });
                expScooters.Participants.Add(new ExpenseParticipant { UserId = priya.Id, ShareAmount = 1200m });
                expScooters.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 1200m });
                expScooters.Participants.Add(new ExpenseParticipant { UserId = sneha.Id, ShareAmount = 1200m });

                // Expense 4: Scuba Diving & Parasailing (Exact split)
                var expWatersports = new Expense
                {
                    GroupId = goaGroup.Id,
                    Title = "Grand Island Scuba & Watersports",
                    BaseAmount = 9500m,
                    Amount = 9500m,
                    PaidByUserId = rahul.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-4),
                    CategoryId = catEnt?.Id,
                    SplitType = SplitType.ExactAmount,
                    Notes = "Sneha & Amit took Scuba combo, Rahul took JetSki and Parasail",
                    Tags = "adventure,scuba",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                };
                expWatersports.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 3500m, ExactAmount = 3500m });
                expWatersports.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 3000m, ExactAmount = 3000m });
                expWatersports.Participants.Add(new ExpenseParticipant { UserId = sneha.Id, ShareAmount = 3000m, ExactAmount = 3000m });

                context.Expenses.AddRange(expVilla, expDinner, expScooters, expWatersports);

                // Expenses for Flatmates
                var expWifi = new Expense
                {
                    GroupId = flatGroup.Id,
                    Title = "Airtel Xstream Fiber 200Mbps",
                    BaseAmount = 1199m,
                    TaxAmount = 215.82m,
                    Amount = 1414.82m,
                    PaidByUserId = priya.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-12),
                    CategoryId = catUtil?.Id,
                    SplitType = SplitType.Equal,
                    Notes = "Monthly broadband renewal",
                    Tags = "wifi,bills",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                };
                expWifi.Participants.Add(new ExpenseParticipant { UserId = priya.Id, ShareAmount = 471.60m });
                expWifi.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 471.61m });
                expWifi.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 471.61m });

                var expGroceries = new Expense
                {
                    GroupId = flatGroup.Id,
                    Title = "Nature's Basket Organic Groceries",
                    BaseAmount = 3200m,
                    DiscountAmount = 200m,
                    Amount = 3000m,
                    PaidByUserId = rahul.Id,
                    ExpenseDate = DateTime.UtcNow.AddDays(-3),
                    CategoryId = catGroceries?.Id,
                    SplitType = SplitType.Equal,
                    Notes = "Oil, rice, spices, detergent, milk cartons",
                    Tags = "groceries,kitchen",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                };
                expGroceries.Participants.Add(new ExpenseParticipant { UserId = rahul.Id, ShareAmount = 1000m });
                expGroceries.Participants.Add(new ExpenseParticipant { UserId = priya.Id, ShareAmount = 1000m });
                expGroceries.Participants.Add(new ExpenseParticipant { UserId = amit.Id, ShareAmount = 1000m });

                context.Expenses.AddRange(expWifi, expGroceries);

                // 6. Seed Recurring Expenses
                var recMaintenance = new RecurringExpense
                {
                    GroupId = flatGroup.Id,
                    Title = "Apartment Society Maintenance",
                    Amount = 3500m,
                    CategoryId = catUtil?.Id,
                    PaidByUserId = priya.Id,
                    SplitType = SplitType.Equal,
                    Frequency = "Monthly",
                    NextDueDate = DateTime.UtcNow.AddDays(8),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                };
                context.RecurringExpenses.Add(recMaintenance);

                // 7. Seed Activity Logs
                context.ActivityLogs.AddRange(
                    new ActivityLog
                    {
                        GroupId = goaGroup.Id,
                        UserId = rahul.Id,
                        Action = "GroupCreated",
                        Description = "Rahul Sharma created the group 'Goa Trip 2026 🌴'",
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    },
                    new ActivityLog
                    {
                        GroupId = goaGroup.Id,
                        UserId = rahul.Id,
                        Action = "ExpenseAdded",
                        Description = "Rahul Sharma added 'Luxury Villa Booking' (₹15,000.00)",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new ActivityLog
                    {
                        GroupId = goaGroup.Id,
                        UserId = priya.Id,
                        Action = "ExpenseAdded",
                        Description = "Priya Patel added 'Fisherman's Wharf Seafood Feast' (₹5,200.00)",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new ActivityLog
                    {
                        GroupId = flatGroup.Id,
                        UserId = priya.Id,
                        Action = "GroupCreated",
                        Description = "Priya Patel created the group 'Apartment 402 Flatmates 🏠'",
                        CreatedAt = DateTime.UtcNow.AddMonths(-1)
                    }
                );

                // 8. Seed Sample Notification
                context.InAppNotifications.AddRange(
                    new InAppNotification
                    {
                        UserId = rahul.Id,
                        Title = "Welcome to Mera Kharcha!",
                        Message = "Your smart expense tracking workspace is ready. Check your Goa trip balances.",
                        Type = "success",
                        LinkUrl = $"/Groups/Details/{goaGroup.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new InAppNotification
                    {
                        UserId = priya.Id,
                        Title = "Settlement Optimization Ready",
                        Message = "Settlement algorithm reduced transactions from 6 pairwise transfers to 2 direct payments!",
                        Type = "info",
                        LinkUrl = $"/Groups/Details/{goaGroup.Id}?tab=settlements",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureSchemaColumnsAsync(ApplicationDbContext context)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // 1. Check columns in Groups
                var groupCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Groups');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        groupCols.Add(reader.GetString(1));
                    }
                }

                if (!groupCols.Contains("BudgetAmount"))
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "ALTER TABLE Groups ADD COLUMN BudgetAmount TEXT NULL DEFAULT '0';";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!groupCols.Contains("BudgetPeriod"))
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "ALTER TABLE Groups ADD COLUMN BudgetPeriod TEXT NULL DEFAULT 'Total';";
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Check columns in Expenses
                var expenseCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Expenses');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        expenseCols.Add(reader.GetString(1));
                    }
                }

                if (!expenseCols.Contains("Currency"))
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "ALTER TABLE Expenses ADD COLUMN Currency TEXT NULL DEFAULT 'INR';";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!expenseCols.Contains("OriginalAmount"))
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "ALTER TABLE Expenses ADD COLUMN OriginalAmount TEXT NULL DEFAULT '0';";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!expenseCols.Contains("ExchangeRate"))
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "ALTER TABLE Expenses ADD COLUMN ExchangeRate TEXT NULL DEFAULT '1.0';";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Ignore fallback
            }

            // Set default budgets for demo groups if not already set
            try
            {
                await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET BudgetAmount = '40000', BudgetPeriod = 'Trip' WHERE Name LIKE '%Goa%' AND (BudgetAmount IS NULL OR BudgetAmount = '0');");
                await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET BudgetAmount = '15000', BudgetPeriod = 'Monthly' WHERE Name LIKE '%Apartment%' AND (BudgetAmount IS NULL OR BudgetAmount = '0');");
                await context.Database.ExecuteSqlRawAsync("UPDATE Expenses SET Currency = 'INR', ExchangeRate = '1.0' WHERE Currency IS NULL;");
            }
            catch
            {
                // Ignore if error
            }
        }
    }
}
