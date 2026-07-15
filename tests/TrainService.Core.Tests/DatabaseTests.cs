using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainService.Core.Entities;
using TrainService.Core.Enums;
using TrainService.Data;
using Xunit;

namespace TrainService.Core.Tests;

public class DatabaseTests
{
    private TrainDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TrainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var context = new TrainDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Add_And_Read_NetworkSwitch_ShouldSucceed()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var netSwitch = new NetworkSwitch
        {
            Name = "Main Router",
            PortCount = 8
        };

        // Act
        context.NetworkSwitches.Add(netSwitch);
        await context.SaveChangesAsync();

        var fetchedSwitch = await context.NetworkSwitches.FirstOrDefaultAsync(s => s.Name == "Main Router");

        // Assert
        Assert.NotNull(fetchedSwitch);
        Assert.Equal(8, fetchedSwitch.PortCount);
        Assert.NotEqual(Guid.Empty, fetchedSwitch.Id);
    }

    [Fact]
    public async Task Add_And_Delete_Device_ShouldSucceed()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var device = new Device
        {
            Name = "Masa 5 İstasyonu",
            Kind = DeviceKind.StationEsp32,
            Ip = "192.168.1.50"
        };
        
        context.Devices.Add(device);
        await context.SaveChangesAsync();
        
        // Act (Delete)
        context.Devices.Remove(device);
        await context.SaveChangesAsync();
        
        var count = await context.Devices.CountAsync();

        // Assert
        Assert.Equal(0, count);
    }
}
