using Microsoft.EntityFrameworkCore;
using EncoreLedger.Models;

namespace EncoreLedger.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<BulkImport> BulkImports { get; set; }
        public DbSet<ImportMapping> ImportMappings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>().ToTable("Transaction").HasKey(t => t.IDTransaction);
            modelBuilder.Entity<Category>().ToTable("Category").HasKey(c => c.IDCategory);
            modelBuilder.Entity<Account>().ToTable("Account").HasKey(a => a.IDAccount);
            modelBuilder.Entity<BulkImport>().ToTable("BulkImport").HasKey(b => b.IDBulkImport);
            modelBuilder.Entity<ImportMapping>().ToTable("ImportMapping").HasKey(m => m.IDImportMapping);

            // One-to-many: Category -> Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Account -> Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountID)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: BulkImport -> Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.BulkImport)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BulkImportID)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Account -> ImportMapping
            modelBuilder.Entity<ImportMapping>()
                .HasOne(m => m.Account)
                .WithMany()
                .HasForeignKey(m => m.AccountID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}