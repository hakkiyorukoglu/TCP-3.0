using Microsoft.EntityFrameworkCore;
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
            services.AddSingleton<TrainService.Cad.ICadProject, TrainService.Cad.CadProject>();

            var dbPath = "trainservice.db";
            if (System.IO.File.Exists("appsettings.json"))
            {
                try
                {
                    var json = System.IO.File.ReadAllText("appsettings.json");
                    var node = System.Text.Json.Nodes.JsonNode.Parse(json);
                    if (node != null && node["DatabaseConfig"] != null && node["DatabaseConfig"]["DbPath"] != null)
                    {
                        dbPath = node["DatabaseConfig"]["DbPath"].ToString();
                    }
                }
                catch { }
            }
            services.AddDbContext<TrainService.Data.TrainDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            
            services.AddScoped<TrainService.Core.Abstractions.ITcpRepository, TrainService.Data.Repositories.TcpRepository>();
            
            services.AddSingleton<TrainService.Core.Abstractions.IMqttHub, TrainService.Messaging.Hubs.MqttHub>();
            services.AddSingleton<TrainService.Core.Abstractions.IEmbeddedBrokerService, TrainService.Messaging.Hubs.EmbeddedBrokerService>();
            services.AddSingleton<TrainService.Core.Abstractions.IDeviceRegistry, TrainService.Messaging.Registry.DeviceRegistry>();
            services.AddSingleton<TrainService.Core.Abstractions.IPingService, TrainService.Messaging.Health.PingService>();
            services.AddSingleton<TrainService.Core.Abstractions.IDispatchService, TrainService.Messaging.Commands.DispatchService>();
            services.AddSingleton<TrainService.Messaging.Mocks.MockStationClient>();
            services.AddSingleton<TrainService.Core.Abstractions.ITrainManager, TrainService.App.Services.TrainManager>();
            
            // Cad Services
            services.AddSingleton<TrainService.Cad.CadDocument>();
            services.AddSingleton<TrainService.Cad.UndoRedo.CommandStack>();
            services.AddSingleton<TrainService.Cad.Selection.SelectionService>();
            
            // v3.0.19 eklentileri (GridSnapProvider'dan önce eklendi):
            services.AddSingleton<TrainService.Cad.Snapping.ISnapProvider, TrainService.Cad.Snapping.EndpointSnapProvider>();
            services.AddSingleton<TrainService.Cad.Snapping.ISnapProvider, TrainService.Cad.Snapping.OnSegmentSnapProvider>();
            
            // v3.0.17:
            services.AddSingleton<TrainService.Cad.Snapping.ISnapProvider, TrainService.Cad.Snapping.GridSnapProvider>();
            services.AddSingleton<TrainService.Cad.Snapping.SnapEngine>();
            services.AddScoped<TrainService.Cad.Persistence.ICadDocumentStore, TrainService.App.Services.CadDocumentStore>();

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

    public static IHost CreateHostForTest() => _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        
        using (var scope = _host.Services.CreateScope())
        {
            try {
                var dbContext = scope.ServiceProvider.GetRequiredService<TrainService.Data.TrainDbContext>();
                dbContext.Database.Migrate();
            } catch (System.Exception ex) {
                System.IO.File.WriteAllText("dberror.txt", ex.ToString());
            }
        }

        var trainManager = _host.Services.GetRequiredService<TrainService.Core.Abstractions.ITrainManager>();
        trainManager.Initialize();

        var embeddedBroker = _host.Services.GetRequiredService<TrainService.Core.Abstractions.IEmbeddedBrokerService>();
        _ = embeddedBroker.StartAsync(1883);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var embeddedBroker = _host.Services.GetRequiredService<TrainService.Core.Abstractions.IEmbeddedBrokerService>();
        await embeddedBroker.StopAsync();

        await _host.StopAsync();
        _host.Dispose();
        
        base.OnExit(e);
    }
}


