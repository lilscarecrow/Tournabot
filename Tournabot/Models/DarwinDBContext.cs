using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tournabot.Models
{
    public partial class DarwinDBContext : DbContext
    {
        string sqlString;
        public DarwinDBContext(string sql)
        {
            sqlString = sql;
        }

        public DarwinDBContext(DbContextOptions<DarwinDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Directors> Directors { get; set; }
        public virtual DbSet<Finals> Finals { get; set; }
        public virtual DbSet<MatchA> MatchA { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(sqlString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Directors>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.DirectorName).HasMaxLength(50);

                entity.Property(e => e.MatchId).HasColumnName("MatchID");
            });

            modelBuilder.Entity<Finals>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<MatchA>(entity =>
            {
                entity.ToTable("Match A");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.DiscordTag)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.Region).HasMaxLength(2);
            });
        }
    }
}
