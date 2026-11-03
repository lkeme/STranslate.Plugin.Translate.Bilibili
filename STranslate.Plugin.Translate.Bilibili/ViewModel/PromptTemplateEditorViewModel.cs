using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace STranslate.Plugin.Translate.Bilibili.ViewModel;

/// <summary>
/// Editing state for the role-free translation-template dialog. It works on
/// clones only; the caller commits the resulting models after the dialog saves.
/// </summary>
public partial class PromptTemplateEditorViewModel : ObservableObject
{
    private readonly Func<string, string> _translate;
    private readonly string _newTemplateName;

    public PromptTemplateEditorViewModel(
        IEnumerable<TranslationPromptTemplate> templates,
        Func<string, string> translate)
    {
        _translate = translate;
        _newTemplateName = T("STranslate_Plugin_Translate_Bilibili_TemplateNewName");
        Templates = new ObservableCollection<PromptTemplateEditorItem>(templates.Select(PromptTemplateEditorItem.FromModel));

        if (Templates.Count == 0)
            Templates.Add(new PromptTemplateEditorItem(_newTemplateName, isEnabled: true, isDefault: true));

        SelectedTemplate = Templates.FirstOrDefault(template => template.IsEnabled) ?? Templates[0];
    }

    public ObservableCollection<PromptTemplateEditorItem> Templates { get; }

    [ObservableProperty]
    private PromptTemplateEditorItem? _selectedTemplate;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [RelayCommand]
    private void NewTemplate()
    {
        var template = new PromptTemplateEditorItem(GetUniqueTemplateName(_newTemplateName));
        Templates.Add(template);
        SelectedTemplate = template;
    }

    [RelayCommand(CanExecute = nameof(CanCopyTemplate))]
    private void CopyTemplate()
    {
        if (SelectedTemplate is null)
            return;

        var copy = SelectedTemplate.Clone();
        copy.Name = GetUniqueTemplateName($"{SelectedTemplate.Name} {T("STranslate_Plugin_Translate_Bilibili_TemplateCopy")}");
        copy.IsDefault = false;
        copy.IsEnabled = false;
        Templates.Add(copy);
        SelectedTemplate = copy;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTemplate))]
    private void DeleteTemplate()
    {
        if (SelectedTemplate is null || SelectedTemplate.IsDefault || Templates.Count <= 1)
            return;

        var removed = SelectedTemplate;
        var nextIndex = Math.Min(Templates.IndexOf(removed), Templates.Count - 2);
        Templates.Remove(removed);
        SelectedTemplate = Templates[nextIndex];

        if (removed.IsEnabled)
            SetActiveTemplate();
        else
            UpdateTemplateCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanSetActiveTemplate))]
    private void SetActiveTemplate()
    {
        if (SelectedTemplate is null)
            return;

        foreach (var template in Templates)
            template.IsEnabled = ReferenceEquals(template, SelectedTemplate);

        UpdateTemplateCommandStates();
    }

    [RelayCommand(CanExecute = nameof(CanEditGlossary))]
    private void AddGlossaryTerm()
    {
        SelectedTemplate?.Glossary.Add(new GlossaryTermEditorItem());
    }

    [RelayCommand(CanExecute = nameof(CanRemoveGlossaryTerm))]
    private void RemoveGlossaryTerm(GlossaryTermEditorItem? term)
    {
        if (term is not null)
            SelectedTemplate?.Glossary.Remove(term);
    }

    /// <summary>
    /// Validates and materializes the form values without mutating persistent settings.
    /// </summary>
    public bool TryBuildTemplates(out IReadOnlyList<TranslationPromptTemplate> templates)
    {
        ValidationMessage = string.Empty;
        templates = [];

        if (Templates.Count == 0)
        {
            ValidationMessage = T("STranslate_Plugin_Translate_Bilibili_TemplateRequired");
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var models = new List<TranslationPromptTemplate>();

        foreach (var template in Templates)
        {
            var name = template.Name.Trim();
            if (name.Length == 0)
            {
                ValidationMessage = T("STranslate_Plugin_Translate_Bilibili_TemplateNameRequired");
                return false;
            }

            if (!names.Add(name))
            {
                ValidationMessage = T("STranslate_Plugin_Translate_Bilibili_TemplateNameDuplicate");
                return false;
            }

            var glossary = new List<GlossaryTerm>();
            foreach (var term in template.Glossary)
            {
                var source = term.Source.Trim();
                var target = term.Target.Trim();

                if (source.Length == 0 && target.Length == 0)
                    continue;

                if (source.Length == 0 || target.Length == 0)
                {
                    ValidationMessage = T("STranslate_Plugin_Translate_Bilibili_GlossaryTermIncomplete");
                    return false;
                }

                glossary.Add(new GlossaryTerm { Source = source, Target = target });
            }

            models.Add(new TranslationPromptTemplate
            {
                Name = name,
                Instruction = template.Instruction.Trim(),
                Glossary = glossary,
                IsDefault = template.IsDefault,
                InheritDefaultGlossary = template.InheritDefaultGlossary,
                IsEnabled = template.IsEnabled,
            });
        }

        var defaultTemplate = models.FirstOrDefault(template => template.IsDefault);
        if (defaultTemplate is null)
        {
            ValidationMessage = T("STranslate_Plugin_Translate_Bilibili_TemplateDefaultRequired");
            return false;
        }

        foreach (var template in models)
            template.IsDefault = ReferenceEquals(template, defaultTemplate);

        var activeTemplate = models.FirstOrDefault(template => template.IsEnabled) ?? models[0];
        foreach (var template in models)
            template.IsEnabled = ReferenceEquals(template, activeTemplate);

        templates = models;
        return true;
    }

    partial void OnSelectedTemplateChanged(PromptTemplateEditorItem? value) => UpdateTemplateCommandStates();

    private bool CanCopyTemplate() => SelectedTemplate is not null;

    private bool CanDeleteTemplate() => SelectedTemplate is not null && !SelectedTemplate.IsDefault && Templates.Count > 1;

    private bool CanSetActiveTemplate() => SelectedTemplate is not null && !SelectedTemplate.IsEnabled;

    private bool CanEditGlossary() => SelectedTemplate is not null;

    private bool CanRemoveGlossaryTerm(GlossaryTermEditorItem? term) => SelectedTemplate is not null && term is not null;

    private void UpdateTemplateCommandStates()
    {
        CopyTemplateCommand.NotifyCanExecuteChanged();
        DeleteTemplateCommand.NotifyCanExecuteChanged();
        SetActiveTemplateCommand.NotifyCanExecuteChanged();
        AddGlossaryTermCommand.NotifyCanExecuteChanged();
        RemoveGlossaryTermCommand.NotifyCanExecuteChanged();
    }

    private string GetUniqueTemplateName(string baseName)
    {
        var candidate = string.IsNullOrWhiteSpace(baseName) ? "模板" : baseName.Trim();
        if (!Templates.Any(template => string.Equals(template.Name.Trim(), candidate, StringComparison.OrdinalIgnoreCase)))
            return candidate;

        var index = 2;
        while (Templates.Any(template => string.Equals(template.Name.Trim(), $"{candidate} {index}", StringComparison.OrdinalIgnoreCase)))
            index++;

        return $"{candidate} {index}";
    }

    private string T(string key) => _translate(key);
}

public partial class PromptTemplateEditorItem : ObservableObject
{
    public PromptTemplateEditorItem(
        string name = "",
        string instruction = "",
        bool isEnabled = false,
        bool isDefault = false,
        bool inheritDefaultGlossary = true)
    {
        Name = name;
        Instruction = instruction;
        IsEnabled = isEnabled;
        IsDefault = isDefault;
        InheritDefaultGlossary = inheritDefaultGlossary;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _instruction;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private bool _inheritDefaultGlossary = true;

    public ObservableCollection<GlossaryTermEditorItem> Glossary { get; } = [];

    public static PromptTemplateEditorItem FromModel(TranslationPromptTemplate template)
    {
        var item = new PromptTemplateEditorItem(
            template.Name,
            template.Instruction,
            template.IsEnabled,
            template.IsDefault,
            template.InheritDefaultGlossary);
        foreach (var term in template.Glossary ?? [])
            item.Glossary.Add(new GlossaryTermEditorItem(term.Source, term.Target));

        return item;
    }

    public PromptTemplateEditorItem Clone()
    {
        var copy = new PromptTemplateEditorItem(Name, Instruction, IsEnabled, IsDefault, InheritDefaultGlossary);
        foreach (var term in Glossary)
            copy.Glossary.Add(new GlossaryTermEditorItem(term.Source, term.Target));

        return copy;
    }
}

public partial class GlossaryTermEditorItem : ObservableObject
{
    public GlossaryTermEditorItem(string source = "", string target = "")
    {
        Source = source;
        Target = target;
    }

    [ObservableProperty]
    private string _source;

    [ObservableProperty]
    private string _target;
}
