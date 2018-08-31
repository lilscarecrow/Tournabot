using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tournabot.Models
{
    public partial class DarwinDBContext : DbContext
    {
        private string connection;
        public DarwinDBContext(string conn)
        {
            connection = conn;
        }

        public DarwinDBContext(DbContextOptions<DarwinDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Users> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(connection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.DiscordTag)
                    .HasName("Primary");

                entity.ToTable("users");

                entity.Property(e => e.DiscordTag)
                    .HasMaxLength(50)
                    .ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.Region).HasMaxLength(2);
            });
        }
    }
}
