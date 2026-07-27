using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Core.Abstractions;
using Helpers;
using Models;
using ModPlusAPI;
using ModPlusAPI.Mvvm;
using Services;

/// <summary>
/// ViewModel строки состояния с иерархией выбора и групповым индикатором прогресса.
/// </summary>
public class StatusBarVM : ObservableObject
{
    private readonly IEditorWorkspace _workspace;
    private readonly EditorOperationTracker _operations;
    private readonly IEditorDiagnostics _diagnostics;
    private string _operationsHeader = string.Empty;
    private string _progressToolTip = string.Empty;
    private double _overallProgress;
    private bool _isOverallIndeterminate;
    private bool _hasMultipleOperations;
    private string _statusToolTip = "Готово";
    private bool _isOperationInProgress;
    private string _lastTransientMessage = string.Empty;
    private double _lastAvailableWidth = 480;
    
    /// <summary>
    /// Создаёт ViewModel строки состояния, подписанную на рабочую область и трекер операций.
    /// </summary>
    /// <param name="workspace">Рабочая область с выбором, открытыми модулями и режимами отображения.</param>
    /// <param name="operations">Трекер длительных операций — источник данных прогресса.</param>
    /// <param name="diagnostics">Сводка диагностики (ошибки/предупреждения/обновления).</param>
    public StatusBarVM(
        IEditorWorkspace workspace,
        EditorOperationTracker operations,
        IEditorDiagnostics diagnostics)
    {
        _workspace = workspace;
        _operations = operations;
        _diagnostics = diagnostics;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
        _workspace.OpenModules.CollectionChanged += OnOpenModulesChanged;
        _operations.Changed += OnOperationsChanged;
        _diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
        UpdateProgress();
        ApplyLayoutInternal();
    }
    
    /// <summary>
    /// Сводка диагностики для индикаторов строки состояния.
    /// </summary>
    public IEditorDiagnostics Diagnostics => _diagnostics;
    
    /// <summary>
    /// Переключает фильтр рабочей области по категории диагностики (повторный клик — выключить).
    /// </summary>
    public ICommand ToggleDiagnosticFilterCommand => new RelayCommand<DiagnosticCategory>(
        category => SafeExecute.Execute(() =>
        {
            if (category == null)
            {
                return;
            }
            
            _ = ToggleDiagnosticFilterAsync(category);
        }),
        category => category is DiagnosticCategory diagnosticCategory && diagnosticCategory.HasItems);
    
    /// <summary>
    /// Сегменты строки состояния для отображения в UI.
    /// </summary>
    public ObservableCollection<StatusBarSegmentVm> StatusSegments { get; } = [];
    
    /// <summary>
    /// Подсказка со сводной информацией о текущем контексте.
    /// </summary>
    public string StatusToolTip
    {
        get => _statusToolTip;
        private set
        {
            if (_statusToolTip == value)
            {
                return;
            }
            
            _statusToolTip = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Активные операции для развёрнутого списка прогресса.
    /// </summary>
    public ReadOnlyObservableCollection<IEditorOperation> ActiveOperations => _operations.Operations;
    
    /// <summary>
    /// Сводный заголовок индикатора: текст единственной операции либо число операций.
    /// </summary>
    public string OperationsHeader
    {
        get => _operationsHeader;
        private set
        {
            if (_operationsHeader == value)
            {
                return;
            }
            
            _operationsHeader = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Общая доля выполнения всех операций от 0 до 1 для шкалы прогресса.
    /// </summary>
    public double OverallProgress
    {
        get => _overallProgress;
        private set
        {
            if (Math.Abs(_overallProgress - value) < 0.0001)
            {
                return;
            }
            
            _overallProgress = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Неопределён ли общий прогресс (показывать «бегущую» шкалу).
    /// </summary>
    public bool IsOverallIndeterminate
    {
        get => _isOverallIndeterminate;
        private set
        {
            if (_isOverallIndeterminate == value)
            {
                return;
            }
            
            _isOverallIndeterminate = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Выполняется ли больше одной операции (есть смысл разворачивать список).
    /// </summary>
    public bool HasMultipleOperations
    {
        get => _hasMultipleOperations;
        private set
        {
            if (_hasMultipleOperations == value)
            {
                return;
            }
            
            _hasMultipleOperations = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Полный текст прогресса для подсказки.
    /// </summary>
    public string ProgressToolTip
    {
        get => _progressToolTip;
        private set
        {
            if (_progressToolTip == value)
            {
                return;
            }
            
            _progressToolTip = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Идёт ли в данный момент фоновая операция.
    /// </summary>
    public bool IsOperationInProgress
    {
        get => _isOperationInProgress;
        private set
        {
            if (_isOperationInProgress == value)
            {
                return;
            }
            
            _isOperationInProgress = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Пересчитывает сегменты строки состояния под доступную ширину.
    /// </summary>
    /// <param name="availableWidth">Доступная ширина области строки состояния в пикселях.</param>
    public void ApplyLayout(double availableWidth)
    {
        if (availableWidth > 0)
        {
            _lastAvailableWidth = availableWidth;
        }
        
        ApplyLayoutInternal();
    }
    
    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IEditorWorkspace.ActiveDiagnosticFilter))
        {
            SyncFilterActiveStates(_workspace.ActiveDiagnosticFilter);
        }
        
        // Прогресс приходит напрямую из трекера (OnOperationsChanged); workspace сообщает
        // только смену выбора/режимов, влияющую на раскладку сегментов.
        ApplyLayoutInternal();
    }
    
    private void OnOperationsChanged()
    {
        UpdateProgress();
        if (_lastTransientMessage != _operations.TransientMessage)
        {
            _lastTransientMessage = _operations.TransientMessage;
            ApplyLayoutInternal();
        }
    }
    
    private void OnOpenModulesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyLayoutInternal();
    }
    
    private void OnDiagnosticsPropertyChanged(object sender, PropertyChangedEventArgs e) =>
        OnPropertyChanged(nameof(Diagnostics));
    
    private async Task ToggleDiagnosticFilterAsync(DiagnosticCategory category)
    {
        if (_workspace.ActiveDiagnosticFilter == category.Severity)
        {
            _workspace.SetDiagnosticResultsView(false, []);
            SyncFilterActiveStates(null);
            return;
        }
        
        var candidates = category.Modules.Select(entry => entry.Module).ToList();
        if (candidates.Count == 0)
        {
            return;
        }
        
        await _workspace.ShowDiagnosticFilterAsync(category.Severity, candidates);
        SyncFilterActiveStates(category.Severity);
    }
    
    private void SyncFilterActiveStates(DiagnosticSeverity? active)
    {
        foreach (var cat in _diagnostics.Categories)
        {
            cat.IsFilterActive = cat.Severity == active;
        }
    }
    
    private void RefreshHierarchyState()
    {
    }
    
    private void ApplyLayoutInternal()
    {
        var isSearchMode = _workspace.IsSearchResultsView || _workspace.IsDiagnosticResultsView;
        var domainName = _workspace.SelectedDomain?.Name;
        var moduleName = _workspace.SelectedModule?.Name;
        var entryName = _workspace.SelectedTranslationEntry?.Name;
        var openModulesCount = _workspace.OpenModules.Count;

        var segments = StatusBarLayoutComposer.Compose(
            isSearchMode,
            domainName,
            moduleName,
            entryName,
            openModulesCount,
            _lastAvailableWidth);

        StatusSegments.Clear();
        foreach (var segment in segments)
        {
            StatusSegments.Add(segment);
        }
        
        StatusToolTip = StatusBarLayoutComposer.BuildToolTip(
            isSearchMode,
            domainName,
            moduleName,
            entryName,
            openModulesCount);

        if (!string.IsNullOrEmpty(_operations.TransientMessage))
        {
            StatusToolTip = $"{_operations.TransientMessage}\n{StatusToolTip}";
        }
    }
    
    private void UpdateProgress()
    {
        var wasInProgress = IsOperationInProgress;
        IsOperationInProgress = _operations.IsActive;
        
        var count = _operations.ActiveCount;
        HasMultipleOperations = count > 1;
        IsOverallIndeterminate = _operations.IsOverallIndeterminate;
        OverallProgress = _operations.OverallProgress;
        OperationsHeader = count switch
        {
            0 => string.Empty,
            1 => _operations.Operations[0].DisplayText,
            _ => $"Операций: {count}",
        };
        ProgressToolTip = count == 0
            ? string.Empty
            : string.Join("\n", _operations.Operations.Select(o => o.DisplayText));
        
        if (wasInProgress != IsOperationInProgress)
        {
            ApplyLayoutInternal();
        }
    }
}