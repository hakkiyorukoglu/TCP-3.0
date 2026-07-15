using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TrainService.Core.Geometry;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    // Ekranda (durum çubuğu için) gösterilecek fare dünya koordinatı
    [ObservableProperty]
    private string _cursorWorldPosition = "0.0, 0.0 mm";

    public event Action<List<(Vector2D, Vector2D)>>? OnTestLinesLoaded;

    public EditorViewModel()
    {
    }

    public async Task LoadSampleMapAsync()
    {
        await Task.CompletedTask;
        
        var lines = new List<(Vector2D, Vector2D)>();
        var rand = new Random(42);
        
        // Rastgele 10.000 çizgi oluştur (5000x5000 mm alanında)
        for (int i = 0; i < 10000; i++)
        {
            var x = rand.NextDouble() * 5000;
            var y = rand.NextDouble() * 5000;
            var dx = (rand.NextDouble() - 0.5) * 100; // max 100 mm uzunluğunda
            var dy = (rand.NextDouble() - 0.5) * 100;
            
            lines.Add((new Vector2D(x, y), new Vector2D(x + dx, y + dy)));
        }
        
        OnTestLinesLoaded?.Invoke(lines);
    }
}
