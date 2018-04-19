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

            modelBuilder.Entity<Host>().ToTable("Host");
            modelBuilder.Entity<RegisteredDevice>().ToTable("RegisteredDevice");
            modelBuilder.Entity<ActivePairingCode>().ToTable("ActivePairingCode");
            modelBuilder.Entity<HostPeripheral>().ToTable("HostPeripheral");

            modelBuilder.Entity<Host>().HasMany(h => h.PairedPeripherals).WithOne(hp => hp.Host)
                .HasForeignKey(hp => hp.HostId);
            modelBuilder.Entity<Peripheral>().HasMany(h => h.PairedHosts).WithOne(hp => hp.Peripheral)
                .HasForeignKey(hp => hp.PeripheralId);
            modelBuilder.Entity<HostPeripheral>().HasKey(h => new { h.HostId, h.PeripheralId });
            modelBuilder.Entity<HostPeripheral>().HasOne(hp => hp.Host).WithMany(h => h.PairedPeripherals)
                .HasForeignKey(hp => hp.HostId);
            modelBuilder.Entity<HostPeripheral>().HasOne(hp => hp.Peripheral).WithMany(h => h.PairedHosts)
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
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string Token { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }

    public class Host : RegisteredDevice
    {
        public Host()
        {
            PairedPeripherals = new List<HostPeripheral>();
        }

        public List<HostPeripheral> PairedPeripherals { get; set; }
    }

    public class Peripheral : RegisteredDevice
    {
        public Peripheral()
        {
            PairedHosts = new List<HostPeripheral>();
        }

        public List<HostPeripheral> PairedHosts { get; set; }
    }

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
    }
}
