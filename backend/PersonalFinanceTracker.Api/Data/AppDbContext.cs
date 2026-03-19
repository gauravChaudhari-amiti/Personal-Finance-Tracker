using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PersonalFinanceTracker.Api.Entities;

namespace PersonalFinanceTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserActionToken> UserActionTokens => Set<UserActionToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserNumber)
                .UseIdentityByDefaultColumn();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AuthProvider).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.UserNumber).IsUnique();
        });

        modelBuilder.Entity<UserActionToken>(entity =>
        {
            entity.ToTable("user_action_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Type });

            entity.HasOne(x => x.User)
                .WithMany(x => x.AuthTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CategoryId).HasMaxLength(64);
            entity.Property(x => x.OpeningBalance).HasColumnType("numeric(12,2)");
            entity.Property(x => x.CurrentBalance).HasColumnType("numeric(12,2)");
            entity.Property(x => x.CreditLimit).HasColumnType("numeric(12,2)");
            entity.Property(x => x.InstitutionName).HasMaxLength(120);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Accounts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(20);
            entity.Property(x => x.Icon).HasMaxLength(50);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CategoryId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("numeric(12,2)");

            entity.HasIndex(x => new { x.UserId, x.CategoryId, x.Month, x.Year }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Budgets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Budgets)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("goals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TargetAmount).HasColumnType("numeric(12,2)");
            entity.Property(x => x.CurrentAmount).HasColumnType("numeric(12,2)");
            entity.Property(x => x.CategoryId).HasMaxLength(64);
            entity.Property(x => x.LinkedAccountId).HasMaxLength(64);
            entity.Property(x => x.Icon).HasMaxLength(50);
            entity.Property(x => x.Color).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Goals)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.LinkedAccount)
                .WithMany(x => x.LinkedGoals)
                .HasForeignKey(x => x.LinkedAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.ToTable("recurring_transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            entity.Property(x => x.CategoryId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccountId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Frequency).HasMaxLength(20).IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.RecurringTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.RecurringTransactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Account)
                .WithMany(x => x.RecurringTransactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransactionRecord>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.TransactionNumber)
                .UseIdentityByDefaultColumn();
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccountId).HasMaxLength(64);
            entity.Property(x => x.GoalId).HasMaxLength(64);
            entity.Property(x => x.CategoryId).HasMaxLength(64);
            entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("numeric(12,2)");
            entity.Property(x => x.Date).HasColumnType("date");
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Merchant).HasMaxLength(200);
            entity.Property(x => x.PaymentMethod).HasMaxLength(50);
            entity.Property(x => x.TransferGroupId).HasMaxLength(64);
            entity.Property(x => x.Tags).HasColumnType("text[]");

            entity.HasIndex(x => x.TransactionNumber).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Goal)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.GoalId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.CategoryItem)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
