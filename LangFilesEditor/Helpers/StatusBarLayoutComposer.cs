namespace LangFilesEditor.Helpers;

using UI.Windows.MainWindow;

/// <summary>
/// Раскладка левого блока status bar: иерархия со стрелками, равная обрезка, скрытие domain → module.
/// </summary>
internal static class StatusBarLayoutComposer
{
    // todo: разве это не стилистические штуки? мб вынести их куда-то? для настроек тем что-ли
    private const double ArrowChromeWidth = 20;
    private const double SeparatorChromeWidth = 22;
    
    // todo: мне немного странно, что список сегментов рассчитывается как-то в возвращаемом vm, а не во view, что было бы логичнее.
    /// <summary>
    /// Формирует список сегментов status bar с учётом доступной ширины и текущей иерархии навигации.
    /// </summary>
    /// <param name="isSearchMode">Активен ли режим поиска.</param>
    /// <param name="domainName">Имя домена.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="entryName">Имя записи.</param>
    /// <param name="openModulesCount">Количество открытых модулей.</param>
    /// <param name="availableWidth">Доступная ширина левого блока status bar в пикселях.</param>
    /// <returns>Упорядоченный список сегментов для отображения.</returns>
    public static IReadOnlyList<StatusBarSegmentVm> Compose(
        bool isSearchMode,
        string? domainName,
        string? moduleName,
        string? entryName,
        int openModulesCount,
        double availableWidth)
    {
        if (availableWidth < 80)
        {
            availableWidth = 80;
        }
        
        var showDomain = !string.IsNullOrEmpty(domainName);
        var showModule = !string.IsNullOrEmpty(moduleName);
        var showEntry = !string.IsNullOrEmpty(entryName);
        
        if (!showDomain && !showModule && !showEntry && !isSearchMode)
        {
            return [Label(EditorStrings.StatusBarReady, EditorStrings.StatusBarReady, null)];
        }
        
        while (true)
        {
            var hierarchy = BuildHierarchy(domainName, moduleName, entryName, showDomain, showModule, showEntry);
            var chromeWidth = MeasureChrome(isSearchMode, hierarchy.Count);
            var openText = EditorStrings.FormatOpenModulesCount(openModulesCount);
            chromeWidth += StatusBarTextMetrics.MeasureLabel(openText);
            
            if (hierarchy.Count == 0)
            {
                var onlyAux = BuildSegments(isSearchMode, domainName, moduleName, entryName, openModulesCount, showDomain, showModule, showEntry, null);
                if (MeasureSegments(onlyAux) <= availableWidth)
                {
                    return onlyAux;
                }
                
                return BuildSegments(isSearchMode, domainName, moduleName, entryName, openModulesCount, false, false, showEntry, StatusBarTextMetrics.MinHierarchyLabelWidth);
            }
            
            var naturalLabels = hierarchy.Sum(h => StatusBarTextMetrics.MeasureLabel(h.Text));
            if (chromeWidth + naturalLabels <= availableWidth)
            {
                return BuildSegments(isSearchMode, domainName, moduleName, entryName, openModulesCount, showDomain, showModule, showEntry, null);
            }
            
            var labelBudget = availableWidth - chromeWidth;
            var equalWidth = labelBudget / hierarchy.Count;
            if (equalWidth >= StatusBarTextMetrics.MinHierarchyLabelWidth)
            {
                var equalized = BuildSegments(isSearchMode, domainName, moduleName, entryName, openModulesCount, showDomain, showModule, showEntry, equalWidth);
                if (MeasureSegments(equalized) <= availableWidth + 1)
                {
                    return equalized;
                }
            }
            
            if (showDomain)
            {
                showDomain = false;
                continue;
            }
            
            if (showModule)
            {
                showModule = false;
                continue;
            }
            
            var fallbackWidth = Math.Max(StatusBarTextMetrics.MinHierarchyLabelWidth, labelBudget);
            return BuildSegments(isSearchMode, domainName, moduleName, entryName, openModulesCount, false, false, showEntry, fallbackWidth);
        }
    }
    
    /// <summary>
    /// Собирает полную подсказку status bar из всех элементов иерархии навигации.
    /// </summary>
    /// <param name="isSearchMode">Активен ли режим поиска.</param>
    /// <param name="domainName">Имя домена.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="entryName">Имя записи.</param>
    /// <param name="openModulesCount">Количество открытых модулей.</param>
    /// <returns>Текст всплывающей подсказки; «Готово», если иерархия пуста.</returns>
    public static string BuildToolTip(bool isSearchMode, string? domainName, string? moduleName, string? entryName, int openModulesCount)
    {
        var parts = new List<string>();
        if (isSearchMode)
        {
            parts.Add(EditorStrings.StatusBarSearchMode);
        }
        
        if (!string.IsNullOrEmpty(domainName))
        {
            parts.Add(domainName);
        }
        
        if (!string.IsNullOrEmpty(moduleName))
        {
            parts.Add(moduleName);
        }
        
        if (!string.IsNullOrEmpty(entryName))
        {
            parts.Add(entryName);
        }
        
        parts.Add(EditorStrings.FormatOpenModulesCount(openModulesCount));
        return parts.Count > 0 ? string.Join(" → ", parts) : EditorStrings.StatusBarReady;
    }
    
    private static List<(string Text, string ToolTip)> BuildHierarchy(
        string? domainName,
        string? moduleName,
        string? entryName,
        bool showDomain,
        bool showModule,
        bool showEntry)
    {
        var list = new List<(string Text, string ToolTip)>();
        if (showDomain && domainName != null)
        {
            list.Add((domainName, domainName));
        }
        
        if (showModule && moduleName != null)
        {
            list.Add((moduleName, moduleName));
        }
        
        if (showEntry && entryName != null)
        {
            list.Add((entryName, entryName));
        }
        
        return list;
    }
    
    private static double MeasureChrome(bool isSearchMode, int hierarchyCount)
    {
        var width = 0.0;
        if (isSearchMode)
        {
            width += StatusBarTextMetrics.MeasureLabel(EditorStrings.StatusBarSearchMode) + SeparatorChromeWidth;
        }
        
        if (hierarchyCount > 0)
        {
            width += (hierarchyCount - 1) * ArrowChromeWidth;
        }
        
        if (hierarchyCount > 0 || isSearchMode)
        {
            width += SeparatorChromeWidth;
        }
        
        return width;
    }
    
    private static double MeasureSegments(IReadOnlyList<StatusBarSegmentVm> segments)
    {
        var total = 0.0;
        foreach (var segment in segments)
        {
            total += segment.Kind switch
            {
                StatusBarSegmentKind.Label => segment.MaxWidth ?? StatusBarTextMetrics.MeasureLabel(segment.Text),
                StatusBarSegmentKind.Arrow => ArrowChromeWidth,
                StatusBarSegmentKind.Separator => SeparatorChromeWidth,
                _ => 0
            };
        }
        
        return total;
    }
    
    private static IReadOnlyList<StatusBarSegmentVm> BuildSegments(
        bool isSearchMode,
        string? domainName,
        string? moduleName,
        string? entryName,
        int openModulesCount,
        bool showDomain,
        bool showModule,
        bool showEntry,
        double? hierarchyEqualWidth)
    {
        var segments = new List<StatusBarSegmentVm>();
        if (isSearchMode)
        {
            segments.Add(Label(EditorStrings.StatusBarSearchMode, EditorStrings.StatusBarSearchMode, null));
            segments.Add(Separator());
        }
        
        var firstHierarchy = true;
        
        if (showDomain && domainName != null)
        {
            segments.Add(Label(domainName, domainName, hierarchyEqualWidth));
            firstHierarchy = false;
        }
        
        if (showModule && moduleName != null)
        {
            if (!firstHierarchy)
            {
                segments.Add(Arrow());
            }
            
            segments.Add(Label(moduleName, moduleName, hierarchyEqualWidth));
            firstHierarchy = false;
        }
        
        if (showEntry && entryName != null)
        {
            if (!firstHierarchy)
            {
                segments.Add(Arrow());
            }
            
            segments.Add(Label(entryName, entryName, hierarchyEqualWidth));
        }
        
        if (segments.Count > 0)
        {
            segments.Add(Separator());
        }
        
        var openText = EditorStrings.FormatOpenModulesCount(openModulesCount);
        segments.Add(Label(openText, openText, null));
        return segments;
    }
    
    private static StatusBarSegmentVm Label(string text, string toolTip, double? maxWidth) =>
        new()
        {
            Kind = StatusBarSegmentKind.Label,
            Text = text,
            ToolTip = toolTip,
            MaxWidth = maxWidth
        };
    
    private static StatusBarSegmentVm Arrow() =>
        new() { Kind = StatusBarSegmentKind.Arrow, Text = "→", ToolTip = string.Empty };
    
    private static StatusBarSegmentVm Separator() =>
        new() { Kind = StatusBarSegmentKind.Separator, Text = "|", ToolTip = string.Empty };
}