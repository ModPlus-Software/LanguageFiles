namespace LangFilesEditor;

using System.ComponentModel;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += OnClosing;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is MainContext mainContext &&
            mainContext.Nodes.Any(n => n.HasIncorrectData) && 
            !mainContext.CloseWithoutSave)
        {
            MessageBox.Show("There is Nodes with errors! Fix them or close without saving");
            e.Cancel = true;
        }
    }
}