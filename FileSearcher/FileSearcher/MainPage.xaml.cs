using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using FileSearcherLibrary;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FileSearcher;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly IFolderPicker _folderPicker;
	private readonly IFileSearcherService _fileSearcherService;
    private readonly ObservableCollection<FileModel> files = new ObservableCollection<FileModel>();

    private string? _path = null;

    public MainPage(ILogger<MainPage> logger, IFolderPicker folderPicker, IFileSearcherService fileSearcherService)
	{
        _logger = logger;
		_folderPicker = folderPicker;
        _fileSearcherService = fileSearcherService;

        _logger.LogInformation("Application started");

        InitializeComponent();

        dataGrid.ItemsSource = files;
    }

    private async void OnLoaded(object sender, EventArgs e)
	{
        var defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        await SetFolderSearch(defaultFolder);

        _logger.LogInformation("Application Loaded");
    }

    private async void OnFolderSearchClicked(object sender, EventArgs e)
	{
        try
		{
			_logger.LogInformation("Change folder asked");
			var result = await _folderPicker.PickAsync(CancellationToken.None);
			if(result.IsSuccessful)
				await SetFolderSearch(result.Folder.Path);
        }
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while switching folder {@Exception}", ex);
		}
		finally
		{
            _logger.LogInformation("Changed folder done");
        }
    }
	
	private async Task SetFolderSearch(string? path)
	{
		LogContext.PushProperty(nameof(path), path);
        _path = path;

		if (string.IsNullOrWhiteSpace(_path))
		{
            folder.Text = "Veuillez choisir un dossier de recherche";
            return;
		}

        folder.Text = path;
        fileCount.Text = (await _fileSearcherService.Count(_path)).ToString();
    }

	private async void OnCounterClicked(object sender, EventArgs e)
    {
        await SearchAsync();
    }

	private async void OnEntryCompleted(object sender, EventArgs e)
    {
        await SearchAsync();
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
		if (string.IsNullOrWhiteSpace(_path))
		{
			return;
		}

        var swWhole = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Search beggining [ {Keywords} ]", entry.Text);

            if (string.IsNullOrWhiteSpace(entry.Text))
				return;

			var keywords = entry.Text
								.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.Distinct()
								.Select(Regex.Escape)
								.ToList();

			_logger.LogInformation("Cleaned keywords {@Keywords}", keywords);

			if (keywords.Count == 0)
				return;
			var sw = Stopwatch.StartNew();
			var result = (await _fileSearcherService.Search(_path, keywords)).ToList();
			sw.Stop();

            _logger.LogDebug("Searched finished in {@Time} with {@Result}", sw.Elapsed, result);
			files.Clear();
			foreach (var item in result)
			{
				files.Add(item);
			}
			dataGrid.SortColumnDescriptions.Clear();
			dataGrid.SortColumnDescriptions.Add(new Syncfusion.Maui.DataGrid.SortColumnDescription()
			{
				ColumnName = nameof(FileModel.HitNumber),
				SortDirection = System.ComponentModel.ListSortDirection.Descending
			});
		}
		catch(Exception ex)
		{
            _logger.LogError(ex, "Error while searching folder {@Exception}", ex);
        }
        finally
        {
			swWhole.Stop();
            _logger.LogInformation("Search ended {@Time}", swWhole.Elapsed);
        }
    }

    private async void OnFileOpenClicked(object sender, EventArgs e)
    {
		try 
        { 
		    var button = sender as Button;
		    var fileModel = button!.BindingContext as FileModel;
            await Launcher.OpenAsync(new OpenFileRequest("Open", new ReadOnlyFile(fileModel!.Path)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while opening file {@Exception}", ex);
        }
    }
}