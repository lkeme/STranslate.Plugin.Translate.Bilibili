using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using STranslate.Plugin.Translate.Bilibili.View;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;

namespace STranslate.Plugin.Translate.Bilibili.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;
    private bool _isUpdating;

    public Main Main { get; }

    public SettingsViewModel(IPluginContext context, Settings settings, Main main)
    {
        _context = context;
        _settings = settings;
        Main = main;

        Model = _settings.Model;
        Models = new ObservableCollection<string>(_settings.Models);
        Temperature = _settings.Temperature;

        PropertyChanged += OnPropertyChanged;
        Models.CollectionChanged += OnModelsCollectionChanged;
    }

    private void OnModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or
                       NotifyCollectionChangedAction.Remove or
                       NotifyCollectionChangedAction.Replace)
        {
            _settings.Models = [.. Models];
            _context.SaveSettingStorage<Settings>();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Model):
                _settings.Model = Model ?? string.Empty;
                break;
            case nameof(Temperature):
                _settings.Temperature = Math.Round(Temperature, 1);
                break;
            default:
                return;
        }

        _context.SaveSettingStorage<Settings>();
    }

    [ObservableProperty] public partial string ValidateResult { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsValidateSuccess { get; set; } = true;
    [ObservableProperty] public partial string? Model { get; set; }
    [ObservableProperty] public partial ObservableCollection<string> Models { get; set; }
    [ObservableProperty] public partial double Temperature { get; set; }

    [RelayCommand]
    private void AddModel(string model)
    {
        if (_isUpdating || string.IsNullOrWhiteSpace(model) || Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        var trimmed = model.Trim();
        Models.Add(trimmed);
        Model = trimmed;
    }

    [RelayCommand]
    private void DeleteModel(string model)
    {
        if (_isUpdating || !Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        if (Model == model)
            Model = Models.Count > 1 ? Models.First(m => m != model) : Main.DefaultModel;

        Models.Remove(model);
    }

    [RelayCommand]
    private void ManageTemplates()
    {
        var editorViewModel = new PromptTemplateEditorViewModel(
            Main.GetPromptTemplates(),
            key => _context.GetTranslation(key));
        var dialog = new PromptTemplateEditorWindow(editorViewModel);
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        if (owner is not null)
            dialog.Owner = owner;

        _context.ApplyTheme(dialog);

        if (dialog.ShowDialog() == true && editorViewModel.TryBuildTemplates(out var templates))
        {
            Main.ReplacePromptTemplates(templates);
        }
    }

    [RelayCommand]
    public async Task ValidateAsync()
    {
        ValidateResult = string.Empty;
        try
        {
            await Main.ValidateApiAsync();
            IsValidateSuccess = true;
            ValidateResult = _context.GetTranslation("ValidationSuccess");
        }
        catch (Exception ex)
        {
            IsValidateSuccess = false;
            ValidateResult = _context.GetTranslation("ValidationFailure");
            _context.Logger.LogError(ex, _context.GetTranslation("ValidationFailure"));
        }
    }

    public void Dispose()
    {
        PropertyChanged -= OnPropertyChanged;
        Models.CollectionChanged -= OnModelsCollectionChanged;
    }

    private readonly struct UpdateGuard : IDisposable
    {
        private readonly SettingsViewModel _viewModel;

        public UpdateGuard(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel._isUpdating = true;
        }

        public void Dispose() => _viewModel._isUpdating = false;
    }
}
