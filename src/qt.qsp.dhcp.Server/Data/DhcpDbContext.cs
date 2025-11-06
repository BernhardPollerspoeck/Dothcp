using Microsoft.EntityFrameworkCore;
using qt.qsp.dhcp.Server.Models;

namespace qt.qsp.dhcp.Server.Data;

public class DhcpDbContext : DbContext
{
    public DhcpDbContext(DbContextOptions<DhcpDbContext> options) : base(options)
    {
    }

    public DbSet<DhcpLease> Leases { get; set; } = null!;
    public DbSet<DhcpReservation> Reservations { get; set; } = null!;
    public DbSet<IpAddressStatus> IpAddressStatuses { get; set; } = null!;
    public DbSet<AppSetting> Settings { get; set; } = null!;
    public DbSet<ClientInfo> ClientInfos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DhcpLease configuration
        modelBuilder.Entity<DhcpLease>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MacAddress);  // Frequent lookup by MAC
            entity.HasIndex(e => e.IpAddressString);  // Frequent lookup by IP
            entity.HasIndex(e => e.Status);  // Dashboard statistics and filtering
            entity.HasIndex(e => e.LeaseStart);  // Expired lease queries
            entity.Property(e => e.MacAddress).IsRequired();
            entity.Property(e => e.IpAddressString).IsRequired();
            entity.Ignore(e => e.IpAddress);
            entity.Ignore(e => e.Subnet);
            entity.Ignore(e => e.Router);
            entity.Ignore(e => e.DhcpServer);
            entity.Ignore(e => e.DnsServers);
            entity.Ignore(e => e.LeaseExpiration);
        });

        // DhcpReservation configuration
        modelBuilder.Entity<DhcpReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MacAddress).IsUnique();  // One reservation per MAC
            entity.HasIndex(e => e.IpAddressString).IsUnique();  // One reservation per IP
            entity.HasIndex(e => e.IsActive);  // Filter active reservations
            entity.Property(e => e.MacAddress).IsRequired();
            entity.Property(e => e.IpAddressString).IsRequired();
            entity.Ignore(e => e.IpAddress);
            entity.Ignore(e => e.Subnet);
            entity.Ignore(e => e.Router);
            entity.Ignore(e => e.DhcpServer);
            entity.Ignore(e => e.DnsServers);
        });

        // IpAddressStatus configuration
        modelBuilder.Entity<IpAddressStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IpAddressString).IsUnique();
            entity.Property(e => e.IpAddressString).IsRequired();
        });

        // AppSetting configuration
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });

        // ClientInfo configuration
        modelBuilder.Entity<ClientInfo>(entity =>
        {
            entity.HasKey(e => e.ClientId);
            entity.Property(e => e.ClientId).IsRequired();
        });
    }
}
