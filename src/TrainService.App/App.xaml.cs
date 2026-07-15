using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using TrainService.App.ViewModels;
using TrainService.App.Services;
using Wpf.Ui;

namespace TrainService.App;

public partial class App : Application
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            // WPF-UI Services
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            // Application Services
            services.AddSingleton<TrainService.Core.Abstractions.ILogBus, LogBus>();
            services.AddSingleton<TrainService.Core.Abstractions.ISettingsService, SettingsService>();

            // ViewModels & Windows
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            // Pages & ViewModels
            services.AddTransient<TerminalPanelViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.HomeView>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.EditorView>();
            services.AddTransient<EditorViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.ElectronicsView>();
            services.AddTransient<ElectronicsViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.KitchenView>();
            services.AddTransient<KitchenViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.InfoView>();
            services.AddTransient<InfoViewModel>();
            services.AddTransient<TrainService.App.Views.Pages.SettingsView>();
            services.AddTransient<SettingsViewModel>();
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
