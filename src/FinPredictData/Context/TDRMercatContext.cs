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

    public virtual DbSet<DataRelation> DataRelations { get; set; }

    public virtual DbSet<DataStadistic> DataStadistics { get; set; }

    public virtual DbSet<Datum> Data { get; set; }

    public virtual DbSet<HistoricalDatum> HistoricalData { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tdrmercatdb;Username=tdrmercat;Password=dnKZFBb5t2Orko.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataRelation>(entity =>
        {
            entity.HasKey(e => e.DataRelationId).HasName("DataRelationId_PK");

            entity.ToTable("DataRelation");
        });

        modelBuilder.Entity<DataStadistic>(entity =>
        {
            entity.HasKey(e => e.DataId).HasName("DataStadistics_pkey");

            entity.Property(e => e.DataId).ValueGeneratedNever();
            entity.Property(e => e.Cagr).HasColumnName("CAGR");
            entity.Property(e => e.Volatilidadcruda).HasColumnName("VOLATILIDADCruda");
            entity.Property(e => e.Volatilidaddetendenciada).HasColumnName("VOLATILIDADDetendenciada");
            entity.Property(e => e.Sharpe).HasColumnName("Sharpe");

            entity.HasOne(d => d.Data).WithOne(p => p.DataStadistic)
                .HasForeignKey<DataStadistic>(d => d.DataId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DATASDISTICS_DATA");
        });

        modelBuilder.Entity<Datum>(entity =>
        {
            entity.HasKey(e => e.DataId);

            entity.Property(e => e.DataId).ValueGeneratedNever();
            entity.Property(e => e.DataName).HasMaxLength(256);
            entity.Property(e => e.IsValue).HasDefaultValue(true);
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
