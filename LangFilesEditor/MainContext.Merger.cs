namespace LangFilesEditor;

using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Xml;
using Structure;

internal partial class MainContext
{
    /// <summary>
    /// Merge language files and copy to local ModPlus directory
    /// </summary>
    public ICommand MergeCommand => new RelayCommand(() => Utils.SafeExecute(() =>
    {
        _mainWindow.TbMergeLog.Text = string.Empty;

        var topDir = Registry.CurrentUser.OpenSubKey("Software\\ModPlus")?.GetValue("TopDir")?.ToString();

        if (string.IsNullOrEmpty(topDir) || !Directory.Exists(topDir))
        {
            WriteToMergeLog("Installed ModPlus not found!");
            return;
        }

        var targetLangDirectory = Path.Combine(topDir, "Languages");
        Directory.CreateDirectory(targetLangDirectory);

        WriteToMergeLog($"Target languages directory: {targetLangDirectory}");

        foreach (var file in Directory.GetFiles(targetLangDirectory, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception)
            {
                WriteToMergeLog($"Failed delete file {file}.\nException: {exception.Message}.\nDelete it manualy and try again");
                return;
            }
        }

        var solutionDirectory = GetSolutionDirectory();
        var sourceLanguagesDirectory = Path.Combine(solutionDirectory, "LanguageFiles");

        try
        {
            var version = Version.Parse(File.ReadAllText(Path.Combine(sourceLanguagesDirectory, "Version.txt")));
            var fileNames = new[] { "Common", "AutoCAD", "Revit", "Renga" };
            foreach (var directory in Directory.GetDirectories(sourceLanguagesDirectory))
            {
                var langName = new DirectoryInfo(directory).Name;
                WriteToMergeLog($"Process language: {langName}");

                if (langName == "es-ES")
                {
                    WriteToMergeLog("Skip");
                    continue;
                }

                var resultDoc = new XElement("ModPlus");
                resultDoc.SetAttributeValue("Name", langName);
                resultDoc.SetAttributeValue("Version", version);

                foreach (var fileName in fileNames)
                {
                    WriteToMergeLog($"    Process part: {fileName}");

                    resultDoc.Add(new XComment(fileName));

                    var elements = new List<XElement>();

                    foreach (var file in Directory.GetFiles(directory, $"{fileName}*.xml", SearchOption.TopDirectoryOnly))
                    {
                        elements.AddRange(XElement.Load(file).Elements());
                    }

                    foreach (var xElement in elements.OrderBy(e => e.Name.LocalName))
                    {
                        xElement.DescendantNodes().Where(x => x.NodeType == XmlNodeType.Comment).Remove();
                        resultDoc.Add(xElement);
                    }
                }

                resultDoc.Save(Path.Combine(targetLangDirectory, $"{langName}.xml"));
                WriteToMergeLog($"Language file for {langName} created");
            }

            WriteToMergeLog("Done");
        }
        catch (Exception exception)
        {
            WriteToMergeLog(exception.Message);
        }
    }));

    private void WriteToMergeLog(string message)
    {
        _mainWindow.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(_mainWindow.TbMergeLog.Text))
                _mainWindow.TbMergeLog.Text += message;
            else
                _mainWindow.TbMergeLog.Text += $"{Environment.NewLine}{message}";
        }, DispatcherPriority.Render);
    }
}