using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Models;

namespace OptivosaITManager.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceAssignment> DeviceAssignments => Set<DeviceAssignment>();
    public DbSet<AccessCredential> Accesses => Set<AccessCredential>();
    public DbSet<Maintenance> MaintenanceRecords => Set<Maintenance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TelecomService> TelecomServices => Set<TelecomService>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasOne(e => e.Department).WithMany(d => d.Employees).HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Location).WithMany(l => l.Employees).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Device>(entity =>
        {
            entity.HasIndex(d => d.InventoryNumber).IsUnique();
            entity.HasIndex(d => d.SerialNumber);
            entity.HasIndex(d => d.ComputerName);
            entity.HasIndex(d => d.QrToken).IsUnique();
            entity.HasOne(d => d.Department).WithMany(dep => dep.Devices).HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.Location).WithMany(l => l.Devices).HasForeignKey(d => d.LocationId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DeviceAssignment>(entity =>
        {
            entity.HasOne(a => a.Device).WithMany(d => d.Assignments).HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Employee).WithMany(e => e.DeviceAssignments).HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => new { a.DeviceId, a.ReturnedAt });
        });

        builder.Entity<AccessCredential>(entity =>
        {
            entity.HasOne(a => a.Device).WithMany(d => d.Accesses).HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Employee).WithMany(e => e.Accesses).HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Maintenance>(entity =>
        {
            entity.HasOne(m => m.Device).WithMany(d => d.MaintenanceRecords).HasForeignKey(m => m.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Technician).WithMany().HasForeignKey(m => m.TechnicianId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });

        builder.Entity<TelecomService>(entity =>
        {
            entity.HasOne(s => s.Location).WithMany(l => l.TelecomServices).HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.SetNull);
            // Índices no únicos: el mismo número de servicio/teléfono/cuenta puede repetirse
            // legítimamente en distintos contextos (rule 15), pero se consultan con frecuencia
            // desde la búsqueda global del módulo y conviene que estén indexados.
            entity.HasIndex(s => s.ServiceNumber);
            entity.HasIndex(s => s.PhoneNumber);
            entity.HasIndex(s => s.AccountNumber);
            entity.HasIndex(s => s.Provider);
        });

        builder.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        builder.Entity<Location>().HasIndex(l => l.Name).IsUnique();
    }
}
