using Xunit;
using TrainService.App.Controls.CadCanvas;
using TrainService.Core.Geometry;
using System.Windows;

namespace TrainService.App.Tests;

public class T3xx_ViewportTests
{
    [Fact]
    public void T302_ZoomToCursor_ShouldKeepWorldPointInvariant()
    {
        // Arrange
        var viewport = new ViewportTransform();
        viewport.Scale = 1.0;
        viewport.PanOffset = new Vector2D(0, 0);

        // Ekrandaki imlecimiz 100, 100 noktasında olsun
        var screenCursor = new Point(100, 100);
        
        // Yakınlaştırma öncesi imlecin altındaki dünya noktası
        var worldBeforeZoom = viewport.ScreenToWorld(screenCursor);

        // Act
        // Yüzde 50 yakınlaştır (x1.5)
        viewport.ZoomAt(screenCursor, 1.5);

        // Assert
        // Yakınlaştırma sonrası aynı piksel (100, 100) altındaki dünya noktası değişmemeli
        var worldAfterZoom = viewport.ScreenToWorld(screenCursor);

        Assert.Equal(worldBeforeZoom.X, worldAfterZoom.X, 3);
        Assert.Equal(worldBeforeZoom.Y, worldAfterZoom.Y, 3);
    }
}
