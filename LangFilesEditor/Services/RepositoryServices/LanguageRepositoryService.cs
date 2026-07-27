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
        
        return Directory.GetDirectories(languageDirectory)
            .Where(dir => Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly).Length > 0)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }
    
    /// <inheritdoc />
    public ObservableCollection<Domain> LoadDomains(
        string languageDirectory,
        IReadOnlyList<string> languages,
        bool isFullLoad = false)
    {
        var domains = new ObservableCollection<Domain>();
        var registeredNames = new HashSet<string>(StringComparer.Ordinal);
        var referenceLanguageDirectory = GetReferenceLanguageDirectory(languageDirectory, languages);
        
        foreach (var languageFile in Directory.GetFiles(referenceLanguageDirectory))
        {
            AddDomainFromFileName(languageFile, domains, registeredNames);
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
    public string GetLanguageDirectory()
    {
        var languageDirectory = SelectPathWithMaxScore(
            Directory.GetDirectories(Constants.LanguageFilesDirectory),
            dir => Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly).Length);
        
        if (languageDirectory == null)
        {
            throw new InvalidOperationException(EditorStrings.LanguageDirectoryNotFound);
        }
        
        return languageDirectory;
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
    
    // todo: следует перерасположить методы. в порядке для удобочтения
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
        var allModules = domains.SelectMany(d => d.Modules).ToList();
        var sourceFiles = CollectSourceFilesToSave(allModules, itemsToRemove);
        if (sourceFiles.Count == 0)
        {
            return;
        }
        
        foreach (var languageName in languages)
        {
            SaveLanguage(allModules, languageName, sourceFiles, itemsToRemove);
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
    
    // todo: GetLanguageDirectory как замена?
    private static string GetReferenceLanguageDirectory(
        string languageDirectory,
        IReadOnlyList<string> languages) =>
        languages is { Count: > 0 }
            ? Path.Combine(languageDirectory, languages[0])
            : Directory.GetDirectories(languageDirectory).First();
    
    private void AddDomainFromFileName(
        string languageFilePath,
        ObservableCollection<Domain> domains,
        HashSet<string> registeredNames)
    {
        var fileName = Path.GetFileNameWithoutExtension(languageFilePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }
        
        var domainName = fileName.Contains(Constants.DomainNamesSeparator)
            ? fileName.Split(Constants.DomainNamesSeparator)[0]
            : fileName;
        
        if (!registeredNames.Add(domainName))
        {
            return;
        }
        
        var domain = new Domain
        {
            Name = domainName,
            IsCommon = string.Equals(domainName, Constants.CommonDomainName, StringComparison.OrdinalIgnoreCase),
        };
        
        domains.Add(domain);
    }
    
    private ObservableCollection<Module> LoadModules(Domain domain)
    {
        var domainPrefix = domain.Name + Constants.DomainNamesSeparator;
        var languageDirectory = GetLanguageDirectory();
        var modules = new ObservableCollection<Module>();

        if (string.IsNullOrEmpty(languageDirectory))
        {
            return modules;
            // todo: здесь по идеи ошибку должно пробрасывать
        }
        
        var sourceFiles = Directory.GetFiles(languageDirectory, "*.xml")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(filename => filename == domain.Name || filename.StartsWith(domainPrefix, StringComparison.Ordinal))
            .OrderBy(filename => filename == domain.Name ? 0 : 1)
            .ThenBy(filename => filename, StringComparer.OrdinalIgnoreCase);
        
        foreach (var filename in sourceFiles)
        {
            LoadModulesFromFile(domain, modules, filename);
        }
        
        return modules;
    }
    
    // todo: строго говоря, если мы читаем какие-то translationentries у нас где-то отдельно должны быть чтение атрибутов, а не здесь внутри чтения не translationentires чтение атрибутов Это разные сущности, хотя и очень близкие. в этом и нюанс.
    private ModuleTranslationData ReadTranslationEntries(
        Module module,
        IReadOnlyList<string> languages,
        string sourceFile,
        CancellationToken cancellationToken)
    {
        var loadedAttributes = new List<TranslationEntry>();
        var loadedItems = new List<TranslationEntry>();
        foreach (var languageName in languages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = LanguageFilePaths.GetSourceFilePath(
                Constants.LanguageFilesDirectory,
                languageName,
                sourceFile);
            
            if (!File.Exists(filePath))
            {
                continue;
            }
            
            var moduleNode = XElement.Load(filePath).Element(module.Name);
            if (moduleNode == null)
            {
                continue;
            }
            
            // todo: странно что для чтения метаданных (атрибутов) передаются загруженные атрибуты...
            ModuleXmlSerializer.ReadMetadata(moduleNode, loadedAttributes, languageName);
            ModuleXmlSerializer.ReadItems(moduleNode, loadedItems, languageName);
        }
        
        return new ModuleTranslationData(loadedAttributes, loadedItems);
    }
    
    // todo: конкретный язык нет необходимости сохранять, а в вопросах оптимизации это словно сомнительное решение.
    private static void SaveLanguage(
        IReadOnlyList<Module> allModules,
        string languageName,
        HashSet<string> sourceFiles,
        IReadOnlyDictionary<string, List<string>> itemsToRemove)
    {
        var languageDirectory = LanguageFilePaths.GetLanguageDirectory(Constants.LanguageFilesDirectory, languageName);
        if (!Directory.Exists(languageDirectory))
        {
            return;
        }
        
        foreach (var sourceFileName in sourceFiles)
        {
            var filePath = LanguageFilePaths.GetSourceFilePath(
                Constants.LanguageFilesDirectory,
                languageName,
                sourceFileName);
            
            if (!File.Exists(filePath))
            {
                // todo: а что делать в случае если это новый плагин? Файл мб и создасться должен. я так думаю. По крайней мере где-то такая функция должна быть. Если уж не так, то хотя бы отдельный метод на создание файла нужен что ли...
                continue;
            }
            
            var modulesInFile = allModules
                .Where(m => string.Equals(m.SourceFileName, sourceFileName, StringComparison.Ordinal))
                .Where(m => HasLoadedEntries(m) || itemsToRemove.ContainsKey(m.Name))
                .ToList();
            
            if (modulesInFile.Count == 0)
            {
                continue;
            }
            
            SaveModulesInFile(filePath, modulesInFile, languageName, itemsToRemove);
        }
    }
    
    // todo: я думаю, что здесь не нужен список "itemsToRemove". А список файлов можно было бы получать совсем другим образом. Я считаю, что это лишний метод. 
    private static HashSet<string> CollectSourceFilesToSave(
        IEnumerable<Module> modules,
        IReadOnlyDictionary<string, List<string>> itemsToRemove)
    {
        var sourceFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            if (string.IsNullOrEmpty(module.SourceFileName))
            {
                continue;
            }
            
            if (HasLoadedEntries(module) || itemsToRemove.ContainsKey(module.Name))
            {
                sourceFiles.Add(module.SourceFileName);
            }
        }
        
        return sourceFiles;
    }
    
    // todo: Это не верно, потому что Metadata это не entry. Должно работать только по Items.Count, однако в этом методе тогда нет никакого смысла.
    private static bool HasLoadedEntries(Module module) => module.Items.Count > 0 || module.Metadata.Count > 0;
    
    // todo: немножко странный метод, потому что в одном файле может быть 1 модуль.
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
    
    // todo: а что-то подобное точно нужно?
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
    
    // todo: Под вопросом вообще необходим ли он. Очень спорная вещь.
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
            foreach (var moduleNode in moduleNodes.OrderBy(node => node.Name.LocalName))
            {
                moduleNode.DescendantNodes().Where(node => node.NodeType == XmlNodeType.Comment).Remove();
                resultDoc.Add(moduleNode);
            }
        }
        
        return resultDoc;
    }
    
    // todo: Под вопросом вообще необходим ли он. Очень спорная вещь.
    private static IEnumerable<XElement> CollectModuleNodesForMerge(string languageDirectory, string domainPrefix)
    {
        var moduleNodes = new List<XElement>();
        foreach (var file in Directory.GetFiles(languageDirectory, $"{domainPrefix}*.xml", SearchOption.TopDirectoryOnly))
        {
            moduleNodes.AddRange(XElement.Load(file).Elements());
        }
        
        return moduleNodes;
    }
    
    // todo: модуль 1 может грузится с 1 файла. А не модули с файла. Тоже под вопросом немного метод.
    private void LoadModulesFromFile(Domain domain, ObservableCollection<Module> modules, string sourceFileName)
    {
        var domainFiles = LanguageFilePaths
            .EnumerateExistingSourceFilePaths(Constants.LanguageFilesDirectory, sourceFileName)
            .ToList();
        if (domainFiles.Count == 0)
        {
            return;
        }
        
        var filepath = SelectPathWithMaxScore(domainFiles, file => XElement.Load(file).Elements().Count());
        if (filepath == null)
        {
            return;
        }
        
        AddModulesFromDocument(domain, modules, sourceFileName, XElement.Load(filepath));
    }
    
    private static void AddModulesFromDocument(
        Domain domain,
        ObservableCollection<Module> modules,
        string sourceFileName,
        XElement document)
    {
        foreach (var moduleNode in document.Elements())
        {
            var moduleName = moduleNode.Name.LocalName;
            if (modules.Any(m => string.Equals(m.Name, moduleName, StringComparison.Ordinal)))
            {
                continue;
            }
            
            var module = new Module(moduleName, domain, sourceFileName);
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
    private static string SelectPathWithMaxScore(IEnumerable<string> paths, Func<string, int> scoreFactory) =>
        paths
            .Select(path => new { Path = path, Score = scoreFactory(path) })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault()
            ?.Path;
}