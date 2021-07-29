﻿using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Rat.Data.Entities;

using static Rat.Data.DatabaseSchema;

namespace Rat.Data
{
    public class RatDbContext : DbContext
    {
        public DbSet<ProjectEntity> Projects { get; set; }

        public DbSet<UserEntity> Users { get; set; }

        public DbSet<ProjectTypeEntity> ProjectTypes { get; set; }

        public RatDbContext(DbContextOptions<RatDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectEntity>(entity =>
                {
                    entity.ToTable("Projects");
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();

                    entity.Property(e => e.Name).HasMaxLength(ProjectSchema.Max_Name_Length).IsRequired();

                    entity.HasOne(e => e.Type);
                    entity.HasMany(e => e.Users).WithMany(u => u.Projects);
                });

            modelBuilder.Entity<UserEntity>(builer =>
            {
                builer.ToTable("Users");

                builer.HasKey(x => x.Id);
                builer.Property(x => x.Id).ValueGeneratedOnAdd();

                builer.Property(x => x.UserId).HasMaxLength(UserSchema.Max_UserId_Length).IsRequired();
            });

            modelBuilder.Entity<ProjectTypeEntity>(builder =>
            {
                builder.ToTable("ProjectTypes");

                builder.HasKey(x => x.Id);
                builder.Property(x => x.Id).ValueGeneratedOnAdd();

                builder.Property(x => x.Name).HasMaxLength(ProjectTypeSchema.Max_Name_Length).IsRequired();

                builder.HasIndex(x => x.Name).IsUnique();

                builder.HasData(new List<ProjectTypeEntity>
                {
                    new ProjectTypeEntity {Id = 1, Name = "other"},
                    new ProjectTypeEntity {Id = 2, Name = "js"},
                    new ProjectTypeEntity {Id = 3, Name = "csharp"}
                });
            });
        }
    }

    internal class RatDesignTimeDbContextFactory : IDesignTimeDbContextFactory<RatDbContext>
    {
        public RatDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RatDbContext>();
            optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=RatDb;User ID=sa;Password=Password1!;Connect Timeout=30");

            return new RatDbContext(optionsBuilder.Options);
        }
    }
}
