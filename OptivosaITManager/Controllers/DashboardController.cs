using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OptivosaITManager.Data;
using OptivosaITManager.Models;
using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var devices = _context.Devices.AsNoTracking();

        var vm = new DashboardViewModel
        {
            TotalDevices = await devices.CountAsync(),
            AssignedDevices = await devices.CountAsync(d => d.Status == DeviceStatus.Asignado),
            AvailableDevices = await devices.CountAsync(d => d.Status == DeviceStatus.Disponible),
            MaintenanceDevices = await devices.CountAsync(d => d.Status == DeviceStatus.Mantenimiento),
            RetiredDevices = await devices.CountAsync(d => d.Status == DeviceStatus.Baja),
            TotalEmployees = await _context.Employees.CountAsync(e => e.Status == EmployeeStatus.Activo),
            TotalEmailAccounts = await _context.EmailAccounts.CountAsync()
        };

        vm.DevicesByStatus = await devices
            .GroupBy(d => d.Status)
            .Select(g => new { Label = g.Key.ToString(), Count = g.Count() })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Label, x.Count)).ToList());

        vm.DevicesByType = await devices
            .GroupBy(d => d.DeviceType)
            .Select(g => new { Label = g.Key.ToString(), Count = g.Count() })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Label, x.Count)).ToList());

        vm.DevicesByDepartment = await devices
            .Where(d => d.Department != null)
            .GroupBy(d => d.Department!.Name)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Label, x.Count)).ToList());

        vm.RecentActivity = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(12)
            .Select(a => new RecentActivityItem
            {
                Timestamp = a.Timestamp,
                Action = a.Action,
                Details = a.Details,
                UserName = a.User != null ? a.User.DisplayName : null
            })
            .ToListAsync();

        return View(vm);
    }
}
