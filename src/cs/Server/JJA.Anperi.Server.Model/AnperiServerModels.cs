using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace JJA.Anperi.Server.Model
{
    public class AnperiDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RegisteredDevice>().HasIndex(nameof(RegisteredDevice.Token)).IsUnique();

            modelBuilder.Entity<ActivePairingCode>().HasIndex(nameof(ActivePairingCode.Code)).IsUnique();
            modelBuilder.Entity<ActivePairingCode>().HasIndex(nameof(ActivePairingCode.PeripheralId)).IsUnique();
            modelBuilder.Entity<ActivePairingCode>().Property(pc => pc.Created).HasDefaultValueSql("NOW()");

            modelBuilder.Entity<Host>().ToTable("Host");
            modelBuilder.Entity<RegisteredDevice>().ToTable("RegisteredDevice");
            modelBuilder.Entity<ActivePairingCode>().ToTable("ActivePairingCode");
            modelBuilder.Entity<HostPeripheral>().ToTable("HostPeripheral");

            modelBuilder.Entity<Host>().HasMany(h => h.PairedDevices).WithOne(hp => hp.Host)
                .HasForeignKey(hp => hp.HostId);
            modelBuilder.Entity<Peripheral>().HasMany(h => h.PairedDevices).WithOne(hp => hp.Peripheral)
                .HasForeignKey(hp => hp.PeripheralId);
            modelBuilder.Entity<HostPeripheral>().HasKey(h => new { h.HostId, h.PeripheralId });
            modelBuilder.Entity<HostPeripheral>().HasOne(hp => hp.Host).WithMany(h => h.PairedDevices)
                .HasForeignKey(hp => hp.HostId);
            modelBuilder.Entity<HostPeripheral>().HasOne(hp => hp.Peripheral).WithMany(h => h.PairedDevices)
                .HasForeignKey(hp => hp.PeripheralId);
            modelBuilder.Entity<HostPeripheral>().HasIndex(hp => hp.HostId);

            base.OnModelCreating(modelBuilder);
        }

        public AnperiDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Host> Hosts { get; set; }
        public DbSet<Peripheral> Peripherals { get; set; }
        public DbSet<RegisteredDevice> RegisteredDevices { get; set; }
        public DbSet<ActivePairingCode> ActivePairingCodes { get; set; }
        public DbSet<HostPeripheral> HostPeripherals { get; set; }
    }

    public class RegisteredDevice
    {
        public RegisteredDevice()
        {
            PairedDevices = new List<HostPeripheral>();
        }

        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string Token { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public List<HostPeripheral> PairedDevices { get; set; }
    }

    public class Host : RegisteredDevice
    { }

    public class Peripheral : RegisteredDevice
    { }

    public class HostPeripheral
    {
        public int HostId { get; set; }
        public Host Host { get; set; }
        
        public int PeripheralId { get; set; }
        public Peripheral Peripheral { get; set; }
    }

    public class ActivePairingCode
    {
        [Key]
        [StringLength(10)]
        public string Code { get; set; }
        [Required]
        public int PeripheralId { get; set; }
        public DateTime Created { get; set; }
    }
}
