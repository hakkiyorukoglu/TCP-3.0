using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRailSwitchRampMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "Switches",
                newName: "MainExitNodeId");

            migrationBuilder.RenameColumn(
                name: "MainSegmentId",
                table: "Switches",
                newName: "EntryNodeId");

            migrationBuilder.RenameColumn(
                name: "DivergingSegmentId",
                table: "Switches",
                newName: "DivergingExitNodeId");

            migrationBuilder.AddColumn<string>(
                name: "BoundServoDeviceId",
                table: "Switches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Position_X",
                table: "Switches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Position_Y",
                table: "Switches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RotationDeg",
                table: "Switches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SwitchState",
                table: "RouteSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryNodeId",
                table: "Ramps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExitNodeId",
                table: "Ramps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Position_X",
                table: "Ramps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Position_Y",
                table: "Ramps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RotationDeg",
                table: "Ramps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_TrackSegments_ProjectId",
                table: "TrackSegments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackNodes_ProjectId",
                table: "TrackNodes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Ramps_ProjectId",
                table: "Ramps",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Layers_ProjectId",
                table: "Layers",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Layers_Projects_ProjectId",
                table: "Layers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ramps_Projects_ProjectId",
                table: "Ramps",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackNodes_Projects_ProjectId",
                table: "TrackNodes",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackSegments_Projects_ProjectId",
                table: "TrackSegments",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Layers_Projects_ProjectId",
                table: "Layers");

            migrationBuilder.DropForeignKey(
                name: "FK_Ramps_Projects_ProjectId",
                table: "Ramps");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackNodes_Projects_ProjectId",
                table: "TrackNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackSegments_Projects_ProjectId",
                table: "TrackSegments");

            migrationBuilder.DropIndex(
                name: "IX_TrackSegments_ProjectId",
                table: "TrackSegments");

            migrationBuilder.DropIndex(
                name: "IX_TrackNodes_ProjectId",
                table: "TrackNodes");

            migrationBuilder.DropIndex(
                name: "IX_Ramps_ProjectId",
                table: "Ramps");

            migrationBuilder.DropIndex(
                name: "IX_Layers_ProjectId",
                table: "Layers");

            migrationBuilder.DropColumn(
                name: "BoundServoDeviceId",
                table: "Switches");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "Switches");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "Switches");

            migrationBuilder.DropColumn(
                name: "RotationDeg",
                table: "Switches");

            migrationBuilder.DropColumn(
                name: "SwitchState",
                table: "RouteSteps");

            migrationBuilder.DropColumn(
                name: "EntryNodeId",
                table: "Ramps");

            migrationBuilder.DropColumn(
                name: "ExitNodeId",
                table: "Ramps");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "Ramps");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "Ramps");

            migrationBuilder.DropColumn(
                name: "RotationDeg",
                table: "Ramps");

            migrationBuilder.RenameColumn(
                name: "MainExitNodeId",
                table: "Switches",
                newName: "NodeId");

            migrationBuilder.RenameColumn(
                name: "EntryNodeId",
                table: "Switches",
                newName: "MainSegmentId");

            migrationBuilder.RenameColumn(
                name: "DivergingExitNodeId",
                table: "Switches",
                newName: "DivergingSegmentId");
        }
    }
}
