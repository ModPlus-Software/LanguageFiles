namespace ModPlus.Languages;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.Win32;

/// <summary>
/// Статическое хранилище документов локализации
/// </summary>
[PublicAPI]
public static class LanguageStorage
{
    /// <summary>
    /// Словарь хранения полных документов локализации с ключом по имени языка
    /// </summary>
    public static readonly Dictionary<string, XmlDocument> FullDocuments = [];

    /// <summary>
    /// Словарь хранения разделенных документов локализации (документ содержит только один узел) с ключом по имени языка
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, XmlDocument>> SeparatedDocuments = [];

    /// <summary>
    /// Возвращает документ <see cref="XmlDocument"/>, состоящий из одного узла,
    /// соответствующего заданному xPath. Если такой узел не найден в документе локализации,
    /// то вернет документ полностью
    /// </summary>
    /// <param name="xPath">Путь к узлу в документе вида ModPlus/Node</param>
    public static XmlDocument GetLanguageDocument(string xPath)
    {
        var langDir = Path.Combine(GetCurrentPluginDirectory(), "Languages");
        var currentLanguageName = GetCurrentLanguage(langDir);
        
        var nodeName = xPath.Replace("ModPlus/", string.Empty);

        if (SeparatedDocuments.TryGetValue(currentLanguageName, out var separatedDocuments))
        {
            if (separatedDocuments.TryGetValue(nodeName, out var separatedDocument))
                return separatedDocument;

            var fullDocument = GetFullDocument();
            separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                separatedDocuments[nodeName] = separatedDocument;
                return separatedDocument;
            }

            return fullDocument;
        }

        {
            var fullDocument = GetFullDocument();
            var separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                SeparatedDocuments[currentLanguageName] =
                    new Dictionary<string, XmlDocument> { { nodeName, separatedDocument } };

                return separatedDocument;
            }

            return fullDocument;
        }
    }

    /// <summary>
    /// Возвращает значение по ключу из текущего документа локализации
    /// </summary>
    /// <param name="nodeName">Имя узла</param>
    /// <param name="key">Ключ искомого значения</param>
    public static string GetItem(string nodeName, string key)
    {
        var langDir = Path.Combine(GetCurrentPluginDirectory(), "Languages");
        var currentLanguageName = GetCurrentLanguage(langDir);

        if (SeparatedDocuments.TryGetValue(currentLanguageName, out var separatedDocuments))
        {
            if (separatedDocuments.TryGetValue(nodeName, out var separatedDocument))
                return GetItem(separatedDocument, nodeName, key);

            var fullDocument = GetFullDocument();
            separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                separatedDocuments[nodeName] = separatedDocument;
                return GetItem(separatedDocument, nodeName, key);
            }

            return GetItem(fullDocument, nodeName, key);
        }

        {
            var fullDocument = GetFullDocument();
            var separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                SeparatedDocuments[currentLanguageName] =
                    new Dictionary<string, XmlDocument> { { nodeName, separatedDocument } };

                return GetItem(separatedDocument, nodeName, key);
            }

            return GetItem(fullDocument, nodeName, key);
        }
    }

    /// <summary>
    /// Возвращает значение атрибута для узла плагина из текущего документа локализации
    /// </summary>
    /// <param name="nodeName">Имя узла</param>
    /// <param name="attributeName">Имя атрибута</param>
    public static string GetAttributeValue(string nodeName, string attributeName)
    {
        var langDir = Path.Combine(GetCurrentPluginDirectory(), "Languages");
        var currentLanguageName = GetCurrentLanguage(langDir);

        if (SeparatedDocuments.TryGetValue(currentLanguageName, out var separatedDocuments))
        {
            if (separatedDocuments.TryGetValue(nodeName, out var separatedDocument))
                return GetAttributeValue(separatedDocument, nodeName, attributeName);

            var fullDocument = GetFullDocument();
            separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                separatedDocuments[nodeName] = separatedDocument;
                return GetAttributeValue(separatedDocument, nodeName, attributeName);
            }

            return GetAttributeValue(fullDocument, nodeName, attributeName);
        }

        {
            var fullDocument = GetFullDocument();
            var separatedDocument = CreateSeparated(fullDocument, nodeName);
            if (separatedDocument != null)
            {
                SeparatedDocuments[currentLanguageName] =
                    new Dictionary<string, XmlDocument> { { nodeName, separatedDocument } };

                return GetAttributeValue(separatedDocument, nodeName, attributeName);
            }

            return GetAttributeValue(fullDocument, nodeName, attributeName);
        }
    }

    private static string GetItem(XmlDocument document, string nodeName, string key)
    {
        var node = document.SelectSingleNode($"/ModPlus/{nodeName}/{key}");
        var value = "Localization error";
        if (node != null)
            value = node.InnerText.ReplaceSymbols();
        return value;
    }

    private static string GetAttributeValue(XmlDocument document, string nodeName, string attributeName)
    {
        var node = document.SelectSingleNode($"/ModPlus/{nodeName}");
        var value = "Localization error";
        if (node is { Attributes: { } attributes } && attributes[attributeName] is { } attribute)
            value = attribute.Value.ReplaceSymbols();
        return value;
    }

    private static XmlDocument GetFullDocument()
    {
        var langDir = Path.Combine(GetCurrentPluginDirectory(), "Languages");
        var currentLanguageName = GetCurrentLanguage(langDir);

        if (!FullDocuments.TryGetValue(currentLanguageName, out var fullDocument))
        {
            fullDocument = new XmlDocument();
            fullDocument.Load(Path.Combine(langDir, currentLanguageName + ".xml"));
            FullDocuments[currentLanguageName] = fullDocument;
        }

        return fullDocument;
    }

    private static XmlDocument CreateSeparated(XmlDocument fullDocument, string nodeName)
    {
        var nodes = fullDocument.DocumentElement?.GetElementsByTagName(nodeName);
        if (nodes is { Count: > 0 })
        {
            var separatedDocument = new XmlDocument();
            var root = separatedDocument.CreateElement(string.Empty, "ModPlus", string.Empty);
            separatedDocument.AppendChild(root);
            var importedNode = separatedDocument.ImportNode(nodes[0], true);
            root.AppendChild(importedNode);
            return separatedDocument;
        }

        return null;
    }

    private static string GetCurrentLanguage(string langDir)
    {
        var lang = GetRegistryValue("CurrentLanguage");

        // Если в настройках пусто, значит пробуем поставить системный язык
        if (string.IsNullOrEmpty(lang))
        {
            var systemLang = CultureInfo.InstalledUICulture.Name;
            var langFile = Path.Combine(langDir, systemLang + ".xml");

            // Иначе ставим Русский
            lang = File.Exists(langFile) ? systemLang : "ru-RU";
        }
        else
        {
            var langFile = Path.Combine(langDir, lang + ".xml");
            return File.Exists(langFile) ? lang : "ru-RU";
        }

        return lang;
    }

    private static string GetCurrentPluginDirectory()
    {
        // Эта dll будет находиться в папке ModPlus/Extensions!
        return Directory.GetParent(Path.GetDirectoryName(typeof(LanguageStorage).Assembly.Location)!)!.FullName;
    }

    /// <summary>
    /// Замена специальных символов
    /// </summary>
    /// <param name="item">Исходная строка</param>
    /// <returns>Новая строка с замененными символами</returns>
    private static string ReplaceSymbols(this string item)
    {
        // &qout; - двойные кавычки
        // &#x0a; - символ перехода на новую строку
        // &lt; - открывающая угловая скобка <
        // &gt; -закрывающая угловая скобка >
        return item
            .Replace("\\n", Environment.NewLine)
            .Replace("&qout;", "\"")
            .Replace("&#x0a;", Environment.NewLine)
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
    }

    /// <summary>
    /// Возвращает значение, связанное с заданным именем, преобразованное в строку (string).
    /// Если имя не найдено, возвращает string.Empty</summary>
    /// <param name="name">Имя параметра</param>
    /// <returns>Значение указанного параметра в виде строки</returns>
    private static string GetRegistryValue(string name)
    {
        var key = Registry.CurrentUser.OpenSubKey("Software\\ModPlus") ?? Registry.CurrentUser.OpenSubKey("ModPlus");
        if (key == null)
            return string.Empty;
        using (key)
            return key.GetValue(name, string.Empty).ToString();
    }
}