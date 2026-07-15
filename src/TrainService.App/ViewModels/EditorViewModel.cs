using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TrainService.Core.Entities;

namespace TrainService.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    public event Action<List<TrackNode>, List<TrackSegment>>? OnMapLoaded;

    public EditorViewModel()
    {
    }

    public async Task LoadSampleMapAsync()
    {
        // TODO: Mimari kural gereği, harita verileri .json üzerinden değil SQLite veritabanından çekilecektir.
        // FAZ C/D aşamasında burası Repository pattern üzerinden doldurulacaktır.
        await Task.CompletedTask;
        OnMapLoaded?.Invoke(new List<TrackNode>(), new List<TrackSegment>());
    }
}
