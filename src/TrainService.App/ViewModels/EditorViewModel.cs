using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TrainService.Cad.Abstractions;
using TrainService.Core.Entities;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ICadParser _cadParser;

    public event Action<List<TrackNode>, List<TrackSegment>>? OnMapLoaded;

    public EditorViewModel(ICadParser cadParser)
    {
        _cadParser = cadParser;
    }

    public async Task LoadSampleMapAsync()
    {
        try
        {
            var result = await _cadParser.ParseAsync("sample_map.json");
            OnMapLoaded?.Invoke(result.Nodes, result.Segments);
        }
        catch { }
    }
}
