using Microsoft.EntityFrameworkCore;
using TrainService.Core.Entities;

namespace TrainService.Data;

public class TrainDbContext : DbContext
{
    public DbSet<TrackNode> TrackNodes { get; set; } = null!;
    public DbSet<TrackSegment> TrackSegments { get; set; } = null!;
    public DbSet<Route> Routes { get; set; } = null!;
    public DbSet<RailSwitch> Switches { get; set; } = null!;
    public DbSet<Station> Stations { get; set; } = null!;
    public DbSet<Train> Trains { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<NetworkSwitch> NetworkSwitches { get; set; } = null!;
    public DbSet<SwitchPort> SwitchPorts { get; set; } = null!;
    public DbSet<EventLog> EventLogs { get; set; } = null!;

    public TrainDbContext(DbContextOptions<TrainDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackNode>().ComplexProperty(e => e.Position);

        modelBuilder.Entity<Route>().OwnsMany(r => r.Steps, builder => 
        {
            builder.ToJson();
        });

        modelBuilder.Entity<TrackNode>()
            .Property(e => e.ConnectedSegments)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<System.Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new System.Collections.Generic.List<System.Guid>()
            )
            .Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<System.Collections.Generic.List<System.Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );

        base.OnModelCreating(modelBuilder);
    }
}
