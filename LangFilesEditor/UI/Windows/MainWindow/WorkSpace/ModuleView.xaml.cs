using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow.WorkSpace;

using System.Windows;
using Models;

/// <summary>
/// Представление модуля с гридом записей перевода; связывает выбор строк и прокрутку к заголовку модуля.
/// </summary>
public partial class ModuleView
{
    private ModuleViewVM _viewModel;
    /// <summary>
    /// Инициализирует грид и подписывается на смену контекста, выбор строк и запросы прокрутки от VM.
    /// </summary>
    public ModuleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
        EntriesGrid.SelectedRowChanged += (_, selectedRow) =>
        {
            if (DataContext is ModuleViewVM viewModel)
            {
                viewModel.OnGridSelectionChanged(selectedRow);
            }
        };
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeViewModel();
        if (e.NewValue is ModuleViewVM viewModel)
        {
            SubscribeViewModel(viewModel);
        }
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ModuleViewVM viewModel)
        {
            SubscribeViewModel(viewModel);
        }
    }
    
    private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeViewModel();
    
    private void SubscribeViewModel(ModuleViewVM viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }
        
        UnsubscribeViewModel();
        _viewModel = viewModel;
        _viewModel.ScrollToModuleRequested += ScrollToModule;
    }
    
    private void UnsubscribeViewModel()
    {
        if (_viewModel == null)
        {
            return;
        }
        
        _viewModel.ScrollToModuleRequested -= ScrollToModule;
        _viewModel = null;
    }
    
    private void ScrollToModule(Module module)
    {
        if (module == null || _viewModel == null)
        {
            return;
        }
        
        if (TryScrollToModuleHeader(module))
        {
            return;
        }
        
        Dispatcher.BeginInvoke(() => TryScrollToModuleHeader(module), System.Windows.Threading.DispatcherPriority.Loaded);
    }
    
    private bool TryScrollToModuleHeader(Module module)
    {
        var header = _viewModel.FindModuleHeaderRow(module);
        if (header == null)
        {
            return false;
        }
        
        EntriesGrid.ScrollRowToTop(header);
        return true;
    }
}