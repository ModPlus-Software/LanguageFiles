namespace LangFilesEditor.Models;

using ModPlusAPI.Mvvm;

/// <summary>
/// Диагностическое состояние записи перевода.
/// </summary>
public sealed class EntryDiagnosticState : ObservableObject
{
    private bool _hasIncorrectData;
    private bool _hasDuplicateName;
    private bool _hasDuplicateValue;
    private DiagnosticSeverity? _extensionDiagnostic;
    private string _diagnosticToolTip;

    /// <summary>
    /// Содержит ли запись некорректные данные (пустое имя, цифра в начале, пустые значения).
    /// </summary>
    public bool HasIncorrectData
    {
        get => _hasIncorrectData;
        set
        {
            if (_hasIncorrectData == value)
            {
                return;
            }

            _hasIncorrectData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleError));
        }
    }

    /// <summary>
    /// Есть ли в родительской коллекции другая запись с тем же именем.
    /// </summary>
    public bool HasDuplicateName
    {
        get => _hasDuplicateName;
        set
        {
            if (_hasDuplicateName == value)
            {
                return;
            }

            _hasDuplicateName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleError));
        }
    }

    /// <summary>
    /// Есть ли в родительской коллекции другая запись с тем же набором значений.
    /// </summary>
    public bool HasDuplicateValue
    {
        get => _hasDuplicateValue;
        set
        {
            if (_hasDuplicateValue == value)
            {
                return;
            }

            _hasDuplicateValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleWarning));
        }
    }

    /// <summary>
    /// Категория диагностики от расширения.
    /// </summary>
    public DiagnosticSeverity? ExtensionDiagnostic
    {
        get => _extensionDiagnostic;
        set
        {
            if (_extensionDiagnostic == value)
            {
                return;
            }

            _extensionDiagnostic = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Текст диагностики от расширения.
    /// </summary>
    public string DiagnosticToolTip
    {
        get => _diagnosticToolTip;
        set
        {
            if (_diagnosticToolTip == value)
            {
                return;
            }

            _diagnosticToolTip = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Показывать ли иконку/подсветку ошибки (для фильтра «*» и счётчиков).
    /// </summary>
    public bool IsVisibleError => HasDuplicateName || HasIncorrectData;

    /// <summary>
    /// Показывать ли иконку/подсветку предупреждения (для фильтра «*» и счётчиков).
    /// </summary>
    public bool IsVisibleWarning => HasDuplicateValue;

    /// <summary>
    /// Проходит ли запись фильтр категории диагностики (ошибка перекрывает предупреждение,
    /// предупреждение перекрывает обновление).
    /// </summary>
    /// <param name="severity">Категория фильтра.</param>
    /// <returns><see langword="true"/>, если состояние относится к указанной категории.</returns>
    public bool MatchesDiagnosticFilter(DiagnosticSeverity severity)
    {
        var isError = IsVisibleError || ExtensionDiagnostic == DiagnosticSeverity.Error;
        var isWarning = IsVisibleWarning || ExtensionDiagnostic == DiagnosticSeverity.Warning;

        return severity switch
        {
            DiagnosticSeverity.Error => isError,
            DiagnosticSeverity.Warning => !isError && isWarning,
            DiagnosticSeverity.Update => !isError && !isWarning && ExtensionDiagnostic == DiagnosticSeverity.Update,
            _ => false,
        };
    }
}