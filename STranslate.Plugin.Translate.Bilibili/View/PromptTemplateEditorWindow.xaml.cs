using STranslate.Plugin.Translate.Bilibili.ViewModel;
using System.Windows;

namespace STranslate.Plugin.Translate.Bilibili.View;

public partial class PromptTemplateEditorWindow
{
    public PromptTemplateEditorWindow(PromptTemplateEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    public PromptTemplateEditorViewModel ViewModel { get; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TryBuildTemplates(out _))
            DialogResult = true;
    }
}
