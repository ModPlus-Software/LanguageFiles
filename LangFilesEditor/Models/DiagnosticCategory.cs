namespace LangFilesEditor.Models;

using System.Collections.ObjectModel;
using ModPlusAPI.Mvvm;

/// <summary>
/// Сводка по одной категории диагностики (ошибки/предупреждения/обновления):
/// суммарный счётчик и разбивка по модулям. Цвет индикатора в модели не хранится —
/// это вопрос отображения, см. <see cref="Converters.DiagnosticSeverityToBrushConverter"/>.
/// </summary>
public sealed class DiagnosticCategory : ObservableObject
{
    private int _count;
    private bool _isFilterActive;

    /// <summary>
    /// Создаёт категорию с фиксированным заголовком и глифом.
    /// </summary>
    /// <param name="severity">Категория диагностики.</param>
    /// <param name="title">Заголовок (Ошибки/Предупреждения/Обновления).</param>
    /// <param name="glyph">Символ-индикатор для строки состояния.</param>
    public DiagnosticCategory(DiagnosticSeverity severity, string title, string glyph)
    {
        Severity = severity;
        Title = title;
        Glyph = glyph;
    }

    /// <summary>
    /// Категория диагностики.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Заголовок для подсказки и шапки раскрывающегося списка.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Символ-индикатор (кружок) рядом со счётчиком.
    /// </summary>
    public string Glyph { get; }

    /// <summary>
    /// Разбивка по модулям: имя модуля и число проблем.
    /// </summary>
    public ObservableCollection<DiagnosticModuleEntry> Modules { get; } = [];

    /// <summary>
    /// Суммарное число проблем этой категории.
    /// </summary>
    public int Count
    {
        get => _count;
        set
        {
            if (_count == value)
            {
                return;
            }

            _count = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasItems));
        }
    }

    /// <summary>
    /// Есть ли хотя бы одна проблема (показывать ли индикатор).
    /// </summary>
    public bool HasItems => _count > 0;

    /// <summary>
    /// Включён ли фильтр рабочей области по этой категории.
    /// </summary>
    public bool IsFilterActive
    {
        get => _isFilterActive;
        set
        {
            if (_isFilterActive == value)
            {
                return;
            }

            _isFilterActive = value;
            OnPropertyChanged();
        }
    }
}