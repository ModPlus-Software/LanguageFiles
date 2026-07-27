namespace LangFilesEditor.UI.Windows.MainWindow;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

/// <summary>
/// Строка состояния редактора; пересчитывает доступную ширину сегментов при изменении размера и прогресса.
/// </summary>
public partial class StatusBar
{
    private const double ProgressPanelWidth = 360;
    private const double ProgressPanelMargin = 12;
    private const double HorizontalChrome = 24;
    
    private readonly DispatcherTimer _popupCloseTimer;
    private Popup _diagnosticPopup;
    private FrameworkElement _diagnosticPopupAnchor;
    private FrameworkElement _diagnosticPopupRoot;
    
    /// <summary>
    /// Инициализирует разметку и подписывается на изменения контекста и размера.
    /// </summary>
    public StatusBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
        _popupCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _popupCloseTimer.Tick += OnPopupCloseTick;
    }
    
    private void ExpandIcon_MouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        OperationsPopup.IsOpen = true;
    }
    
    private void ExpandIcon_MouseLeave(object sender, MouseEventArgs e) => _popupCloseTimer.Start();
    
    private void Popup_MouseEnter(object sender, MouseEventArgs e) => _popupCloseTimer.Stop();
    
    private void Popup_MouseLeave(object sender, MouseEventArgs e) => _popupCloseTimer.Start();
    
    private void DiagnosticExpandIcon_MouseEnter(object sender, MouseEventArgs e)
    {
        _popupCloseTimer.Stop();
        if (sender is not FrameworkElement expandIcon)
        {
            return;
        }
        
        var grid = FindParent<Grid>(expandIcon);
        if (grid == null)
        {
            return;
        }
        
        foreach (var child in LogicalTreeHelper.GetChildren(grid))
        {
            if (child is not Popup popup)
            {
                continue;
            }
            
            popup.PlacementTarget = expandIcon;
            popup.IsOpen = true;
            _diagnosticPopup = popup;
            _diagnosticPopupAnchor = expandIcon;
            _diagnosticPopupRoot = FindChild<Border>(popup.Child, "DiagnosticPopupRoot");
            break;
        }
    }
    
    private void DiagnosticExpandIcon_MouseLeave(object sender, MouseEventArgs e) => _popupCloseTimer.Start();
    
    private void DiagnosticPopup_MouseEnter(object sender, MouseEventArgs e) => _popupCloseTimer.Stop();
    
    private void DiagnosticPopup_MouseLeave(object sender, MouseEventArgs e) => _popupCloseTimer.Start();
    
    private void OnPopupCloseTick(object sender, EventArgs e)
    {
        _popupCloseTimer.Stop();
        if (!ExpandIcon.IsMouseOver && !PopupRoot.IsMouseOver)
        {
            OperationsPopup.IsOpen = false;
        }
        
        if (_diagnosticPopup?.IsOpen == true
            && _diagnosticPopupAnchor?.IsMouseOver != true
            && _diagnosticPopupRoot?.IsMouseOver != true)
        {
            _diagnosticPopup.IsOpen = false;
            _diagnosticPopup = null;
            _diagnosticPopupAnchor = null;
            _diagnosticPopupRoot = null;
        }
    }
    
    private static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = LogicalTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T match)
            {
                return match;
            }
            
            parent = LogicalTreeHelper.GetParent(parent);
        }
        
        return null;
    }
    
    private static T FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is T element && element.Name == name)
            {
                return element;
            }
            
            if (child is DependencyObject dependencyObject)
            {
                var nested = FindChild<T>(dependencyObject, name);
                if (nested != null)
                {
                    return nested;
                }
            }
        }
        
        return null;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e) => UpdateStatusLayout();
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateStatusLayout();
    
    private void OnRootSizeChanged(object sender, SizeChangedEventArgs e) => UpdateStatusLayout();
    
    private void UpdateStatusLayout()
    {
        if (DataContext is not StatusBarVM viewModel)
        {
            return;
        }
        
        var available = CalculateStatusAvailableWidth();
        viewModel.ApplyLayout(available);
    }
    
    private double CalculateStatusAvailableWidth()
    {
        var total = RootPanel.ActualWidth;
        if (total <= 0)
        {
            total = ActualWidth - HorizontalChrome;
        }
        
        if (DataContext is StatusBarVM { IsOperationInProgress: true })
        {
            total -= ProgressPanelWidth + ProgressPanelMargin;
        }
        
        return Math.Max(80, total);
    }
}