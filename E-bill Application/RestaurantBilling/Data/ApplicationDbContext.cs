using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Models;

namespace RestaurantBilling.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<GlobalSetting> GlobalSettings { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.IsActive);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.IsActive);
                
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Products)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Bill configuration
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.CreatedAt);
                entity.HasIndex(b => b.PaymentMethod);
                entity.HasIndex(b => new { b.UserId, b.BillNumber }).IsUnique();
                
                entity.HasOne(b => b.User)
                    .WithMany(u => u.Bills)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BillItem configuration
            modelBuilder.Entity<BillItem>(entity =>
            {
                entity.HasOne(bi => bi.Bill)
                    .WithMany(b => b.BillItems)
                    .HasForeignKey(bi => bi.BillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bi => bi.Product)
                    .WithMany(p => p.BillItems)
                    .HasForeignKey(bi => bi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(bi => bi.BillId);
                entity.HasIndex(bi => bi.ProductId);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rt => rt.Token).IsUnique();
            });

            // ExpenseCategory configuration
            modelBuilder.Entity<ExpenseCategory>(entity =>
            {
                entity.HasIndex(ec => ec.UserId);
                entity.HasIndex(ec => ec.IsActive);

                entity.HasOne(ec => ec.User)
                    .WithMany()
                    .HasForeignKey(ec => ec.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Expense configuration
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.PaymentMethod);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they have expenses (monitoring) or Cascade if preferred
                
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict); // Don't delete expense if category deleted (soft delete category instead)
            });

            // ProductCategory configuration
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasIndex(pc => pc.UserId);
                entity.HasIndex(pc => pc.IsActive);

                entity.HasOne(pc => pc.User)
                    .WithMany()
                    .HasForeignKey(pc => pc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
