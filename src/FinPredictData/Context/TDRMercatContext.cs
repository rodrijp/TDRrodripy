using System;
using System.Collections.Generic;
using FinPredictData.Models;
using Microsoft.EntityFrameworkCore;

namespace FinPredictData.Context;

public partial class TDRMercatContext : DbContext
{
    public TDRMercatContext()
    {
    }

    public TDRMercatContext(DbContextOptions<TDRMercatContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Datum> Data { get; set; }

    public virtual DbSet<HistoricalDatum> HistoricalData { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tdrmercatdb;Username=tdrmercat;Password=dnKZFBb5t2Orko.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Datum>(entity =>
        {
            entity.HasKey(e => e.DataId);

            entity.Property(e => e.DataId).ValueGeneratedNever();
            entity.Property(e => e.DataName).HasMaxLength(256);
            entity.Property(e => e.SourceAccess).HasColumnType("xml");

            entity.HasOne(d => d.Source).WithMany(p => p.Data)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SourceIdToSource");
        });

        modelBuilder.Entity<HistoricalDatum>(entity =>
        {
            entity.HasKey(e => e.HistoricalDataId);

            entity.HasOne(d => d.Data).WithMany(p => p.HistoricalData)
                .HasForeignKey(d => d.DataId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataIdToData");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.ToTable("Source");

            entity.Property(e => e.SourceId).ValueGeneratedNever();
            entity.Property(e => e.SourceName).HasMaxLength(256);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
