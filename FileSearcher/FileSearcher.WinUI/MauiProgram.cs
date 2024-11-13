using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using FileSearcherLibrary;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using Syncfusion.Maui.Core.Hosting;

namespace FileSearcher.WinUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.UseSharedMauiApp();

            builder.UseMauiCommunityToolkit();
            builder.ConfigureSyncfusionCore();

            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);
            builder.Services.AddTransient<IFileSearcherService, FileSearcherService>();
            builder.Services.AddTransient<IFileSearcher, DocxFileSearcher>();
            builder.Services.AddTransient<MainPage>();

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                // Make sure to add "using Microsoft.Maui.LifecycleEvents;" in the top of the file 
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = false;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
                        switch (appWindow.Presenter)
                        {
                            case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                                overlappedPresenter.SetBorderAndTitleBar(true, true);
                                overlappedPresenter.Maximize();
                                break;
                        }
                    });
                });
            });
#endif

            var file = Path.Combine(FileSystem.AppDataDirectory, "log.txt");
            var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j} {NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .Enrich.FromGlobalLogContext()
#if DEBUG
             .MinimumLevel.Debug()
#else
             .MinimumLevel.Information()
#endif
             .Destructure.ToMaximumCollectionCount(10)
             .Destructure.ToMaximumDepth(5)
             .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
             .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
             .WriteTo.Console()
             .WriteTo.File(file, rollingInterval:RollingInterval.Day, outputTemplate: outputTemplate)
             .CreateLogger();

            
			builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();
                logging.AddSerilog(dispose: true);
            });

            return builder.Build();
        }
    }
}
