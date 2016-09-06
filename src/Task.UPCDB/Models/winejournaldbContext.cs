using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Task.UPCDB.Models
{
    public partial class WinejournaldbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=tcp:zu8cg4sdy2.database.windows.net,1433;Initial Catalog=winejournaldb;Persist Security Info=False;User ID=bexposedcommerce@zu8cg4sdy2;Password=MercedesCLK430!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MigrationHistory>(entity =>
            {
                entity.HasKey(e => e.MigrationId)
                    .HasName("PK_dbo.__MigrationHistory");

                entity.ToTable("__MigrationHistory");

                entity.Property(e => e.MigrationId).HasMaxLength(255);

                entity.Property(e => e.Model).IsRequired();

                entity.Property(e => e.ProductVersion)
                    .IsRequired()
                    .HasMaxLength(32);
            });

            modelBuilder.Entity<WineCategories>(entity =>
            {
                entity.HasKey(e => e.WineCategoryId)
                    .HasName("PK_dbo.WineCategories");

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<WineItems>(entity =>
            {
                entity.HasKey(e => e.WineItemId)
                    .HasName("PK_dbo.WineItems");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<WineList>(entity =>
            {
                entity.Property(e => e.AlchoholLevel).HasColumnType("decimal");

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.Rating).HasColumnType("varchar(10)");

                entity.Property(e => e.Region)
                    .IsRequired()
                    .HasColumnType("varchar(150)");

                entity.Property(e => e.UpcCode)
                    .IsRequired()
                    .HasColumnType("varchar(14)");

                entity.Property(e => e.Varietal)
                    .IsRequired()
                    .HasColumnType("varchar(150)");

                entity.Property(e => e.WineName)
                    .IsRequired()
                    .HasColumnType("varchar(250)");

                entity.Property(e => e.Winery)
                    .IsRequired()
                    .HasColumnType("varchar(250)");
            });

            modelBuilder.Entity<WineTastingGuides>(entity =>
            {
                entity.HasKey(e => e.WineTastingGuideId)
                    .HasName("PK_dbo.WineTastingGuides");

                entity.Property(e => e.Description).IsRequired();

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<WineTerms>(entity =>
            {
                entity.HasKey(e => e.WineTermId)
                    .HasName("PK_dbo.WineTerms");

                entity.Property(e => e.Description).IsRequired();

                entity.Property(e => e.Group).IsRequired();

                entity.Property(e => e.Name).IsRequired();
            });
        }

        public virtual DbSet<MigrationHistory> MigrationHistory { get; set; }
        public virtual DbSet<WineCategories> WineCategories { get; set; }
        public virtual DbSet<WineItems> WineItems { get; set; }
        public virtual DbSet<WineList> WineList { get; set; }
        public virtual DbSet<WineTastingGuides> WineTastingGuides { get; set; }
        public virtual DbSet<WineTerms> WineTerms { get; set; }
    }
}