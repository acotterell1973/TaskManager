using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Task.UPCDB.Models
{
    public partial class WineHunterContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=TASK-SERVER;Database=WineHunter;User Id=sa;Password=MercedesCLK430!;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WineJournals>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("WineJournals_pk");

                entity.Property(e => e.UserId).HasColumnType("varchar(250)");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.DeviceId).HasColumnType("varchar(100)");

                entity.Property(e => e.PlaceId)
                    .IsRequired()
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.WineListWineListId).HasColumnName("WineList_WineListId");

                entity.HasOne(d => d.WineListWineList)
                    .WithMany(p => p.WineJournals)
                    .HasForeignKey(d => d.WineListWineListId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("WineJournals_WineList");
            });

            modelBuilder.Entity<WineList>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.Producer)
                    .IsRequired()
                    .HasColumnType("varchar(250)");

                entity.Property(e => e.Region).HasColumnType("varchar(250)");

                entity.Property(e => e.Upc)
                    .IsRequired()
                    .HasColumnName("UPC")
                    .HasColumnType("varchar(20)");

                entity.Property(e => e.WineVarietiesVarietyId).HasColumnName("WineVarieties_VarietyId");

                entity.HasOne(d => d.WineVarietiesVariety)
                    .WithMany(p => p.WineList)
                    .HasForeignKey(d => d.WineVarietiesVarietyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("WineList_WineVarieties");
            });

            modelBuilder.Entity<WineRatings>(entity =>
            {
                entity.HasKey(e => e.WineRatingId)
                    .HasName("WineRatings_pk");

                entity.Property(e => e.Prefix)
                    .IsRequired()
                    .HasColumnType("varchar(5)");

                entity.Property(e => e.Rate).HasColumnType("decimal");

                entity.Property(e => e.WineListWineListId).HasColumnName("WineList_WineListId");

                entity.HasOne(d => d.WineListWineList)
                    .WithMany(p => p.WineRatings)
                    .HasForeignKey(d => d.WineListWineListId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("WineRatings_WineList");
            });

            modelBuilder.Entity<WineTypes>(entity =>
            {
                entity.HasKey(e => e.WineTypeId)
                    .HasName("WineTypes_pk");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("varchar(30)");
            });

            modelBuilder.Entity<WineVarieties>(entity =>
            {
                entity.HasKey(e => e.VarietyId)
                    .HasName("WineVarieties_pk");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("varchar(100)");
            });

            modelBuilder.Entity<WineVarietyTyes>(entity =>
            {
                entity.HasKey(e => new { e.WineVarietiesVarietyId, e.WineTypesWineTypeId })
                    .HasName("WineVarietyTyes_pk");

                entity.Property(e => e.WineVarietiesVarietyId).HasColumnName("WineVarieties_VarietyId");

                entity.Property(e => e.WineTypesWineTypeId).HasColumnName("WineTypes_WineTypeId");

                entity.HasOne(d => d.WineTypesWineType)
                    .WithMany(p => p.WineVarietyTyes)
                    .HasForeignKey(d => d.WineTypesWineTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("WineVarietyTyes_WineTypes");

                entity.HasOne(d => d.WineVarietiesVariety)
                    .WithMany(p => p.WineVarietyTyes)
                    .HasForeignKey(d => d.WineVarietiesVarietyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("WineVarietyTyes_WineVarieties");
            });
        }

        public virtual DbSet<WineJournals> WineJournals { get; set; }
        public virtual DbSet<WineList> WineList { get; set; }
        public virtual DbSet<WineRatings> WineRatings { get; set; }
        public virtual DbSet<WineTypes> WineTypes { get; set; }
        public virtual DbSet<WineVarieties> WineVarieties { get; set; }
        public virtual DbSet<WineVarietyTyes> WineVarietyTyes { get; set; }
    }
}