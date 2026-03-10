using WorkspaceManager.UI.ViewModels;

namespace WorkspaceManager.App;

public partial class ModeEditorWindow : System.Windows.Window
{
    public ModeEditorWindow(
        string title,
        IEnumerable<ModeLayoutOptionViewModel> layoutOptions,
        DesktopModeViewModel? initialMode = null)
    {
        InitializeComponent();
        Title = title;

        LayoutComboBox.ItemsSource = layoutOptions.ToList();
        LayoutComboBox.SelectedValue = initialMode?.LayoutId ?? string.Empty;
        ModeNameTextBox.Text = initialMode?.Name ?? string.Empty;
        DescriptionTextBox.Text = initialMode?.Description ?? string.Empty;
        DesktopIconsVisibleCheckBox.IsChecked = initialMode?.DesktopIconsVisible ?? true;
        TaskbarVisibleCheckBox.IsChecked = initialMode?.TaskbarVisible ?? true;
        ValidationBorder.Visibility = System.Windows.Visibility.Collapsed;

        Loaded += (_, _) => ModeNameTextBox.Focus();
    }

    public string ModeName => ModeNameTextBox.Text.Trim();

    public string Description => DescriptionTextBox.Text.Trim();

    public bool DesktopIconsVisible => DesktopIconsVisibleCheckBox.IsChecked == true;

    public bool TaskbarVisible => TaskbarVisibleCheckBox.IsChecked == true;

    public string LayoutId => LayoutComboBox.SelectedValue as string ?? string.Empty;

    private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ModeName))
        {
            ShowValidation("请输入模式名称。");
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ShowValidation(string message)
    {
        ValidationTextBlock.Text = message;
        ValidationBorder.Visibility = System.Windows.Visibility.Visible;
    }
}
