using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace TrainService.App;

public partial class App : Application
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<MainWindow>();
        }).Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        
        base.OnExit(e);
    }
}
