namespace LangFilesEditor.Services.RepositoryServices;

using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Core.Abstractions;
using Helpers;
using Models;
using Loggers;
using Microsoft.Win32;

/// <summary>
/// Чтение и запись XML-файлов локализации на диске.
/// </summary>
public class LanguageRepositoryService : ILanguageRepository
{
    private static readonly string[] MergeDomainPrefixes = ["Common", "AutoCAD", "Revit", "Renga"];

    /// <inheritdoc />
    public IReadOnlyList<string> LoadLanguages(string languageDirectory)
    {
        if (string.IsNullOrEmpty(languageDirectory) || !Directory.Exists(languageDirectory))
        {
            return [];
        }

        return Directory.EnumerateDirectories(languageDirectory)
            .Where(dir => Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly).Any())
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    /// <inheritdoc />
    public ObservableCollection<Domain> LoadDomains(string languageDirectory, IReadOnlyList<string> languages)
    {
        var domains = new ObservableCollection<Domain>();
        var registeredNames = new HashSet<string>(StringComparer.Ordinal);

        // Домены собираются по всем языковым каталогам: файл нового плагина может появиться
        // сначала только в одном языке, и домен всё равно должен быть виден в дереве.
        foreach (var languageName in EnumerateLanguageNames(languageDirectory, languages))
        {
            var directory = LanguageFilePaths.GetLanguageDirectory(languageDirectory, languageName);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var languageFile in Directory.EnumerateFiles(directory, "*.xml"))
            {
                AddDomainFromFileName(languageFile, domains, registeredNames);
            }
        }

        return domains;
    }

    /// <inheritdoc />
    public Task<ObservableCollection<Module>> LoadModulesAsync(Domain domain)
    {
        if (domain == null)
        {
            throw new ArgumentNullException(nameof(domain));
        }

        return Task.Run(() => LoadModules(domain));
    }

    /// <inheritdoc />
    public Task<ModuleTranslationData> ReadTranslationEntriesAsync(
        Module module,
        IReadOnlyList<string> languages,
        CancellationToken cancellationToken = default)
    {
        if (module == null)
        {
            throw new ArgumentNullException(nameof(module));
        }

        if (languages == null || languages.Count == 0 || string.IsNullOrEmpty(module.SourceFileName))
        {
            return Task.FromResult(new ModuleTranslationData([], []));
        }

        return Task.Run(
            () => ReadTranslationEntries(module, languages, module.SourceFileName, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public void Save(
        ICollection<Domain> domains,
        IReadOnlyList<string> languages,
        IReadOnlyDictionary<string, List<string>> itemsToRemove)
    {
        if (domains == null || domains.Count == 0 || languages == null || languages.Count == 0)
        {
            return;
        }

        itemsToRemove ??= new Dictionary<string, List<string>>();

        // Модули группируются по ключу источника один раз на всё сохранение, а не заново для каждого языка.
        var modulesBySourceKey = domains
            .SelectMany(d => d.Modules)
            .Where(m => !string.IsNullOrEmpty(m.SourceFileName))
            .Where(m => HasLoadedEntries(m) || itemsToRemove.ContainsKey(m.Name))
            .GroupBy(m => m.SourceFileName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Module>)group.ToList(), StringComparer.Ordinal);

        if (modulesBySourceKey.Count == 0)
        {
            return;
        }

        foreach (var languageName in languages)
        {
            SaveLanguage(languageName, modulesBySourceKey, itemsToRemove);
        }
    }

    /// <inheritdoc />
    public void MergeWithWorkingDirectory(NotificationService notifications)
    {
        var targetLangDirectory = GetMergeTargetDirectory(notifications);
        if (targetLangDirectory == null)
        {
            return;
        }

        if (!ClearMergeTargetDirectory(targetLangDirectory, notifications))
        {
            return;
        }

        var sourceLanguagesDirectory = Constants.LanguageFilesDirectory;
        try
        {
            var version = new LocalizationVersionService().GetLocalVersion();
            if (version == null)
            {
                return;
            }

            notifications.Notify(EditorStrings.FormatTargetLanguageVersion(version));

            foreach (var directory in Directory.GetDirectories(sourceLanguagesDirectory))
            {
                var langName = new DirectoryInfo(directory).Name;
                notifications.Notify(EditorStrings.FormatProcessLanguage(langName));
                var mergedDocument = BuildMergedLanguageDocument(directory, langName, version.ToString(), notifications);
                mergedDocument.Save(Path.Combine(targetLangDirectory, $"{langName}.xml"));
                notifications.Notify(EditorStrings.FormatLanguageFileCreated(langName));
            }

            notifications.Notify(EditorStrings.Done);
        }
        catch (Exception exception)
        {
            notifications.Notify(exception.Message);
        }
    }

    /// <summary>
    /// Возвращает имена языковых подпапок: переданный список, либо — если он пуст — фактическое
    /// содержимое каталога локализации.
    /// </summary>
    private static IEnumerable<string> EnumerateLanguageNames(
        string languageFilesRoot,
        IReadOnlyList<string> languages)
    {
        if (languages is { Count: > 0 })
        {
            return languages;
        }

        if (!Directory.Exists(languageFilesRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(languageFilesRoot).Select(Path.GetFileName);
    }

    private static void AddDomainFromFileName(
        string languageFilePath,
        ObservableCollection<Domain> domains,
        HashSet<string> registeredNames)
    {
        var fileName = Path.GetFileNameWithoutExtension(languageFilePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var separatorIndex = fileName.IndexOf(Constants.DomainNamesSeparator, StringComparison.Ordinal);
        var domainName = separatorIndex > 0 ? fileName[..separatorIndex] : fileName;

        if (!registeredNames.Add(domainName))
        {
            return;
        }

        domains.Add(new Domain
        {
            Name = domainName,
            IsCommon = string.Equals(domainName, Constants.CommonDomainName, StringComparison.OrdinalIgnoreCase),
        });
    }

    private static ObservableCollection<Module> LoadModules(Domain domain)
    {
        var domainPrefix = domain.Name + Constants.DomainNamesSeparator;
        var sourceKeys = CollectDomainSourceKeys(domain.Name, domainPrefix)
            .OrderBy(key => key == domain.Name ? 0 : 1)
            .ThenBy(key => key, StringComparer.OrdinalIgnoreCase);

        var loaded = new List<Module>();
        var loadedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sourceKey in sourceKeys)
        {
            LoadModulesFromFile(domain, loaded, loadedNames, sourceKey);
        }

        // В навигационном дереве модули каждой группы показываются по алфавиту.
        loaded.Sort(static (first, second) => string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase));

        return new ObservableCollection<Module>(loaded);
    }

    /// <summary>
    /// Собирает языконезависимые ключи источников домена по всем языковым каталогам: общий файл
    /// домена (<c>Revit.xml</c>, <c>Revit_Architecture.xml</c>) и вынесенные файлы отдельных
    /// плагинов с суффиксом языка (<c>Revit_mprAlignViews_ru.xml</c>).
    /// </summary>
    private static HashSet<string> CollectDomainSourceKeys(string domainName, string domainPrefix)
    {
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        if (!Directory.Exists(Constants.LanguageFilesDirectory))
        {
            return sourceKeys;
        }

        foreach (var languageDirectory in Directory.EnumerateDirectories(Constants.LanguageFilesDirectory))
        {
            var languageName = Path.GetFileName(languageDirectory);
            foreach (var filePath in Directory.EnumerateFiles(languageDirectory, "*.xml"))
            {
                var sourceKey = LanguageFilePaths.GetSourceKey(
                    Path.GetFileNameWithoutExtension(filePath),
                    languageName);

                if (sourceKey == domainName || sourceKey.StartsWith(domainPrefix, StringComparison.Ordinal))
                {
                    sourceKeys.Add(sourceKey);
                }
            }
        }

        return sourceKeys;
    }

    private static ModuleTranslationData ReadTranslationEntries(
        Module module,
        IReadOnlyList<string> languages,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        var loadedAttributes = new List<TranslationEntry>();
        var loadedItems = new List<TranslationEntry>();
        var attributeIndex = new Dictionary<string, TranslationEntry>(StringComparer.Ordinal);
        var itemIndex = new Dictionary<string, TranslationEntry>(StringComparer.Ordinal);

        foreach (var languageName in languages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = LanguageFilePaths.GetSourceFilePath(
                Constants.LanguageFilesDirectory,
                languageName,
                sourceKey);

            if (!File.Exists(filePath))
            {
                continue;
            }

            var moduleNode = XElement.Load(filePath).Element(module.Name);
            if (moduleNode == null)
            {
                continue;
            }

            ModuleXmlSerializer.ReadMetadata(moduleNode, loadedAttributes, attributeIndex, languageName);
            ModuleXmlSerializer.ReadItems(moduleNode, loadedItems, itemIndex, languageName);
        }

        return new ModuleTranslationData(loadedAttributes, loadedItems);
    }

    private static void SaveLanguage(
        string languageName,
        IReadOnlyDictionary<string, IReadOnlyList<Module>> modulesBySourceKey,
        IReadOnlyDictionary<string, List<string>> itemsToRemove)
    {
        var languageDirectory = LanguageFilePaths.GetLanguageDirectory(Constants.LanguageFilesDirectory, languageName);
        if (!Directory.Exists(languageDirectory))
        {
            return;
        }

        foreach (var (sourceKey, modulesInFile) in modulesBySourceKey)
        {
            var filePath = LanguageFilePaths.GetSourceFilePath(
                Constants.LanguageFilesDirectory,
                languageName,
                sourceKey);

            // Файла для этого языка ещё нет: перевод на данный язык не заведён, пропускаем.
            if (!File.Exists(filePath))
            {
                continue;
            }

            SaveModulesInFile(filePath, modulesInFile, languageName, itemsToRemove);
        }
    }

    private static bool HasLoadedEntries(Module module) => module.Items.Count > 0 || module.Metadata.Count > 0;

    private static void SaveModulesInFile(
        string filePath,
        IReadOnlyList<Module> modules,
        string languageName,
        IReadOnlyDictionary<string, List<string>> itemsToRemove)
    {
        var document = XElement.Load(filePath);
        var modified = false;
        foreach (var module in modules)
        {
            var moduleNode = document.Element(module.Name);
            if (moduleNode == null)
            {
                continue;
            }

            if (itemsToRemove.TryGetValue(module.Name, out var removedItems))
            {
                modified |= ModuleXmlSerializer.RemoveItems(moduleNode, removedItems);
            }

            modified |= ModuleXmlSerializer.WriteMetadata(moduleNode, module.Metadata, languageName);
            modified |= ModuleXmlSerializer.WriteItems(moduleNode, module.Items, languageName);
        }

        if (modified)
        {
            ModuleXmlSerializer.WriteDocument(filePath, document);
        }
    }

    private static string GetMergeTargetDirectory(NotificationService notifications)
    {
        var topDir = Registry.CurrentUser.OpenSubKey("Software\\ModPlus")?.GetValue("TopDir")?.ToString();
        if (string.IsNullOrEmpty(topDir) || !Directory.Exists(topDir))
        {
            notifications.Notify(EditorStrings.InstalledModPlusNotFound);
            return null;
        }

        var targetLangDirectory = Path.Combine(topDir, "Languages");
        Directory.CreateDirectory(targetLangDirectory);
        notifications.Notify(EditorStrings.FormatTargetLanguagesDirectory(targetLangDirectory));
        return targetLangDirectory;
    }

    private static bool ClearMergeTargetDirectory(string targetLangDirectory, NotificationService notifications)
    {
        foreach (var file in Directory.GetFiles(targetLangDirectory, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception)
            {
                notifications.Notify(EditorStrings.FormatDeleteFileFailed(file, exception.Message));
                return false;
            }
        }

        return true;
    }

    private static XElement BuildMergedLanguageDocument(
        string languageDirectory,
        string languageName,
        string version,
        NotificationService notifications)
    {
        var resultDoc = new XElement("ModPlus");
        resultDoc.SetAttributeValue("Name", languageName);
        resultDoc.SetAttributeValue("Version", version);
        foreach (var domainPrefix in MergeDomainPrefixes)
        {
            notifications.Notify(EditorStrings.FormatProcessMergePart(domainPrefix));
            resultDoc.Add(new XComment(domainPrefix));
            var moduleNodes = CollectModuleNodesForMerge(languageDirectory, domainPrefix);
            foreach (var moduleNode in moduleNodes.OrderBy(node => node.Name.LocalName, StringComparer.OrdinalIgnoreCase))
            {
                moduleNode.DescendantNodes().Where(node => node.NodeType == XmlNodeType.Comment).Remove();
                resultDoc.Add(moduleNode);
            }
        }

        return resultDoc;
    }

    private static IEnumerable<XElement> CollectModuleNodesForMerge(string languageDirectory, string domainPrefix)
    {
        var moduleNodes = new List<XElement>();
        foreach (var file in Directory.EnumerateFiles(languageDirectory, $"{domainPrefix}*.xml", SearchOption.TopDirectoryOnly))
        {
            moduleNodes.AddRange(XElement.Load(file).Elements());
        }

        return moduleNodes;
    }

    /// <summary>
    /// Читает список модулей одного источника: берётся самый полный языковой файл этого источника
    /// (в разных языках файл может отставать по составу узлов).
    /// </summary>
    private static void LoadModulesFromFile(
        Domain domain,
        List<Module> modules,
        HashSet<string> loadedNames,
        string sourceKey)
    {
        var filepath = SelectPathWithMaxScore(
            LanguageFilePaths.EnumerateExistingSourceFilePaths(Constants.LanguageFilesDirectory, sourceKey),
            file => XElement.Load(file).Elements().Count());

        if (filepath == null)
        {
            return;
        }

        AddModulesFromDocument(domain, modules, loadedNames, sourceKey, XElement.Load(filepath));
    }

    private static void AddModulesFromDocument(
        Domain domain,
        List<Module> modules,
        HashSet<string> loadedNames,
        string sourceKey,
        XElement document)
    {
        foreach (var moduleNode in document.Elements())
        {
            var moduleName = moduleNode.Name.LocalName;
            if (!loadedNames.Add(moduleName))
            {
                continue;
            }

            var module = new Module(moduleName, domain, sourceKey);
            module.SetCatalogEntryCount(moduleNode.Elements().Count());
            modules.Add(module);
        }
    }

    /// <summary>
    /// Выбирает путь с наибольшим числом файлов/элементов среди кандидатов — используется, когда есть несколько
    /// каталогов/файлов-кандидатов (например, языковые копии одного домена) и нужно выбрать самый полный.
    /// </summary>
    /// <param name="paths">Пути-кандидаты.</param>
    /// <param name="scoreFactory">Функция подсчёта «полноты» пути (например, число XML-файлов или XML-элементов).</param>
    /// <returns>Путь с максимальным значением <paramref name="scoreFactory"/> или <see langword="null"/>, если кандидатов нет.</returns>
    private static string SelectPathWithMaxScore(IEnumerable<string> paths, Func<string, int> scoreFactory)
    {
        string best = null;
        var bestScore = int.MinValue;
        foreach (var path in paths)
        {
            var score = scoreFactory(path);
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            best = path;
        }

        return best;
    }
}
