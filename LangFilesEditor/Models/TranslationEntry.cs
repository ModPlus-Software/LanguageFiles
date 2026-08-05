namespace LangFilesEditor.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;
using ModPlusAPI.Mvvm;

/// <summary>
/// Запись перевода: ключ, значения по языкам, комментарий и диагностическое состояние.
/// </summary>
public class TranslationEntry : ObservableObject
{
    private string _name;
    private readonly Dictionary<string, ItemValue> _values;
    private string _comment;
    private string _removesOnVersion;

    /// <summary>
    /// Создаёт пустую запись с инициализированной коллекцией значений.
    /// </summary>
    public TranslationEntry()
    {
        _values = [];
        Values = new ReadOnlyDictionary<string, ItemValue>(_values);
        DiagnosticState = new EntryDiagnosticState();
        DiagnosticState.PropertyChanged += OnDiagnosticStatePropertyChanged;
        Validate();
    }

    /// <summary>
    /// Запрашивает повторную валидацию у родительского модуля при изменении значений.
    /// </summary>
    public event EventHandler ValidateInParent;

    /// <summary>
    /// Имя ключа перевода.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
            Validate();
            OnPropertyChanged(nameof(RowToolTip));
        }
    }

    /// <summary>
    /// Комментарий к этому элементу.
    /// </summary>
    public string Comment
    {
        get => _comment;
        set
        {
            if (value == _comment)
            {
                return;
            }

            _comment = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RowVisualState));
            OnPropertyChanged(nameof(RowToolTip));
        }
    }

    /// <summary>
    /// Диагностическое состояние записи: флаги валидации и диагностика от расширений.
    /// Владеет данными, ранее хранившимися напрямую в <see cref="TranslationEntry"/>;
    /// изменения форвардятся через <see cref="ObservableObject.PropertyChanged"/> этой записи
    /// под теми же именами свойств, поэтому существующие подписчики (например, обёртки строк грида)
    /// продолжают работать без изменений.
    /// </summary>
    public EntryDiagnosticState DiagnosticState { get; }

    /// <summary>
    /// Визуальное состояние строки в гриде: по приоритету — ошибка, предупреждение, обновление,
    /// пометка к удалению (комментарий), иначе обычная строка. Отображение (цвет/кисть)
    /// определяется в UI-слое (см. <c>Converters.RowVisualStateToBrushConverter</c>).
    /// </summary>
    public RowVisualState RowVisualState
    {
        get
        {
            if (DiagnosticState.IsVisibleError || DiagnosticState.ExtensionDiagnostic == DiagnosticSeverity.Error)
            {
                return RowVisualState.Error;
            }

            if (DiagnosticState.IsVisibleWarning || DiagnosticState.ExtensionDiagnostic == DiagnosticSeverity.Warning)
            {
                return RowVisualState.Warning;
            }

            if (DiagnosticState.ExtensionDiagnostic == DiagnosticSeverity.Update)
            {
                return RowVisualState.Update;
            }

            return !string.IsNullOrEmpty(Comment) ? RowVisualState.Marked : RowVisualState.None;
        }
    }

    /// <summary>
    /// Подсказка для строки грида: причины ошибок/предупреждений валидации,
    /// диагностика расширения и пометка к удалению. Пустая строка — подсказку показывать не нужно.
    /// </summary>
    public string RowToolTip => BuildRowToolTip();

    /// <summary>
    /// Значения перевода по именам языков (только для чтения).
    /// </summary>
    public IReadOnlyDictionary<string, ItemValue> Values { get; }

    /// <summary>
    /// Версия локализации, начиная с которой ключ помечен к удалению.
    /// </summary>
    public string RemovesOnVersion
    {
        get => _removesOnVersion;
        set
        {
            if (_removesOnVersion == value)
            {
                return;
            }

            _removesOnVersion = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RowToolTip));
        }
    }

    /// <summary>
    /// Добавляет или заменяет значение для указанного языка.
    /// </summary>
    /// <param name="languageName">Имя языка.</param>
    /// <param name="itemValue">Значение перевода.</param>
    public void Add(string languageName, ItemValue itemValue)
    {
        if (string.IsNullOrEmpty(languageName) || itemValue == null)
        {
            return;
        }

        // Значение языка может замещаться (повторное чтение модуля, импорт): подписка на старом
        // объекте иначе продолжала бы дёргать валидацию уже выброшенным значением.
        if (_values.TryGetValue(languageName, out var previous) && !ReferenceEquals(previous, itemValue))
        {
            previous.PropertyChanged -= ItemValueOnPropertyChanged;
        }

        itemValue.PropertyChanged += ItemValueOnPropertyChanged;
        _values[languageName] = itemValue;
        Validate();
    }

    private void ItemValueOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Validate();
        InvokeValidateInParent();
    }

    /// <summary>
    /// Пересчитывает флаг <see cref="EntryDiagnosticState.HasIncorrectData"/> по текущим данным записи.
    /// </summary>
    public void Validate()
    {
        DiagnosticState.HasIncorrectData = string.IsNullOrEmpty(Name) ||
                           (!string.IsNullOrEmpty(Name) && char.IsDigit(Name[0])) ||
                           _values.Values.Any(v => v == null || string.IsNullOrEmpty(v.Value));
        OnPropertyChanged(nameof(RowToolTip));
    }

    private void InvokeValidateInParent()
    {
        ValidateInParent?.Invoke(this, EventArgs.Empty);
    }

    private void OnDiagnosticStatePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);

        if (AffectsRowVisualState(e.PropertyName))
        {
            OnPropertyChanged(nameof(RowVisualState));
        }

        if (AffectsRowToolTip(e.PropertyName))
        {
            OnPropertyChanged(nameof(RowToolTip));
        }
    }

    private string BuildRowToolTip()
    {
        var parts = new List<string>();

        if (string.IsNullOrEmpty(Name))
        {
            parts.Add(Helpers.EditorStrings.EntryTooltipEmptyName);
        }
        else if (char.IsDigit(Name[0]))
        {
            parts.Add(Helpers.EditorStrings.EntryTooltipNameStartsWithDigit);
        }

        if (_values.Values.Any(v => v == null || string.IsNullOrEmpty(v.Value)))
        {
            parts.Add(Helpers.EditorStrings.EntryTooltipEmptyValues);
        }

        if (DiagnosticState.HasDuplicateName)
        {
            parts.Add(Helpers.EditorStrings.EntryTooltipDuplicateName);
        }

        if (DiagnosticState.HasDuplicateValue)
        {
            parts.Add(Helpers.EditorStrings.EntryTooltipDuplicateValues);
        }

        if (!string.IsNullOrWhiteSpace(DiagnosticState.DiagnosticToolTip))
        {
            parts.Add(DiagnosticState.DiagnosticToolTip.Trim());
        }

        var deletionVersion = ResolveDeletionVersion();
        if (deletionVersion != null || IsMarkedForDeletionByComment())
        {
            parts.Add(Helpers.EditorStrings.FormatEntryMarkedForDeletion(deletionVersion));
        }
        else if (!string.IsNullOrWhiteSpace(Comment))
        {
            parts.Add(Comment.Trim());
        }

        return parts.Count == 0 ? string.Empty : string.Join(Environment.NewLine, parts);
    }

    private string ResolveDeletionVersion()
    {
        if (!string.IsNullOrWhiteSpace(RemovesOnVersion))
        {
            return RemovesOnVersion.Trim();
        }

        return IsMarkedForDeletionByComment()
            ? Comment[Constants.RemoveAfterCommentPrefix.Length..].Trim()
            : null;
    }

    private bool IsMarkedForDeletionByComment() =>
        !string.IsNullOrEmpty(Comment)
        && Comment.StartsWith(Constants.RemoveAfterCommentPrefix, StringComparison.Ordinal);

    private static bool AffectsRowVisualState(string propertyName) => propertyName switch
    {
        nameof(EntryDiagnosticState.HasIncorrectData) => true,
        nameof(EntryDiagnosticState.HasDuplicateName) => true,
        nameof(EntryDiagnosticState.HasDuplicateValue) => true,
        nameof(EntryDiagnosticState.ExtensionDiagnostic) => true,
        _ => false,
    };

    private static bool AffectsRowToolTip(string propertyName) => propertyName switch
    {
        nameof(EntryDiagnosticState.HasIncorrectData) => true,
        nameof(EntryDiagnosticState.HasDuplicateName) => true,
        nameof(EntryDiagnosticState.HasDuplicateValue) => true,
        nameof(EntryDiagnosticState.DiagnosticToolTip) => true,
        _ => false,
    };
}