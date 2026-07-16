using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Steps",
                table: "Routes");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "EventLogs",
                newName: "Ts");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Trains",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TrackSegments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "TrackNodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "SwitchPorts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Switches",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Stations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Routes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "NetworkSwitches",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "EventLogs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "HardwareBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ElementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardwareBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Layers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Layers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GridSizeMm = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ramps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartZ = table.Column<double>(type: "REAL", nullable: false),
                    EndZ = table.Column<double>(type: "REAL", nullable: false),
                    LengthMm = table.Column<double>(type: "REAL", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ramps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SegmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    RouteId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteSteps_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SwitchStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SwitchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwitchStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateKey = table.Column<string>(type: "TEXT", nullable: false),
                    StateValue = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TrainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LayerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteSteps_RouteId",
                table: "RouteSteps",
                column: "RouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HardwareBindings");

            migrationBuilder.DropTable(
                name: "Layers");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Ramps");

            migrationBuilder.DropTable(
                name: "RouteSteps");

            migrationBuilder.DropTable(
                name: "Scenarios");

            migrationBuilder.DropTable(
                name: "ScenarioSteps");

            migrationBuilder.DropTable(
                name: "SwitchStates");

            migrationBuilder.DropTable(
                name: "SystemState");

            migrationBuilder.DropTable(
                name: "TrainStates");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Trains");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TrackSegments");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TrackNodes");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "SwitchPorts");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Switches");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "NetworkSwitches");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Devices");

            migrationBuilder.RenameColumn(
                name: "Ts",
                table: "EventLogs",
                newName: "Timestamp");

            migrationBuilder.AddColumn<string>(
                name: "Steps",
                table: "Routes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "EventLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
