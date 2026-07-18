using IoTAssetTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTAssetTrack.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<Firmware> Firmware => Set<Firmware>();
    public DbSet<DeviceGroup> DeviceGroups => Set<DeviceGroup>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>(entity =>
        {
            entity.ToTable("DeviceType");
            entity.HasKey(e => e.DeviceTypeId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Firmware>(entity =>
        {
            entity.ToTable("Firmware");
            entity.HasKey(e => e.FirmwareId);
            entity.Property(e => e.Version).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => new { e.DeviceTypeId, e.Version }).IsUnique();
            entity.HasOne(e => e.DeviceType)
                .WithMany(dt => dt.FirmwareVersions)
                .HasForeignKey(e => e.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeviceGroup>(entity =>
        {
            entity.ToTable("DeviceGroup");
            entity.HasKey(e => e.GroupId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasOne(e => e.ParentGroup)
                .WithMany(g => g.ChildGroups)
                .HasForeignKey(e => e.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Device");
            entity.HasKey(e => e.DeviceId);
            entity.Property(e => e.SerialNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.LastModified).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.HasOne(e => e.Firmware)
                .WithMany(f => f.Devices)
                .HasForeignKey(e => e.FirmwareId);
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Devices)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
