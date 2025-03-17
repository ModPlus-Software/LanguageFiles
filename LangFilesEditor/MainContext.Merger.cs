namespace LangFilesEditor;

using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Xml;
using Structure;

internal partial class MainContext
{
    private string _localVersion;

    /// <summary>
    /// Local version
    /// </summary>
    public string LocalVersion
    {
        get => _localVersion;
        set
        {
            if (_localVersion == value)
                return;
            _localVersion = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Set local version to file
    /// </summary>
    public ICommand SetLocalVersionCommand => new RelayCommand(() => Utils.SafeExecuteAsync(async () =>
    {
        if (!Version.TryParse(LocalVersion, out var version))
        {
            MessageBox.Show("Filed parse version!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var remoteVersion = await GetRemoteVersion();

        if (version <= remoteVersion)
        {
            MessageBox.Show("The local version is less than or equal to the remote version!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SetLocalVersion(version);

        WriteToMergeLog($"Set {version} as local version");
    }));

    /// <summary>
    /// Merge language files and copy to local ModPlus directory
    /// </summary>
    public ICommand MergeCommand => new RelayCommand(() => Utils.SafeExecuteAsync(async () =>
    {
        Save();

        mainWindow.TbMergeLog.Text = string.Empty;

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
            var version = await GetVersion(sourceLanguagesDirectory);
            WriteToMergeLog($"Target language version: {version}");

            var fileNames = new[] { "Common", "AutoCAD", "Revit", "Renga" };
            foreach (var directory in Directory.GetDirectories(sourceLanguagesDirectory))
            {
                var langName = new DirectoryInfo(directory).Name;
                WriteToMergeLog($"Process language: {langName}");
                
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
        mainWindow.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(mainWindow.TbMergeLog.Text))
                mainWindow.TbMergeLog.Text += message;
            else
                mainWindow.TbMergeLog.Text += $"{Environment.NewLine}{message}";
        }, DispatcherPriority.Render);
    }

    private static async Task<Version> GetVersion(string sourceLanguagesDirectory)
    {
        var localVersion = Version.Parse(await File.ReadAllTextAsync(Path.Combine(sourceLanguagesDirectory, "Version.txt")));
        var remoteVersion = await GetRemoteVersion();
        return localVersion > remoteVersion ? localVersion : remoteVersion;
    }

    private static Version GetLocalVersion()
    {
        try
        {
            var solutionDirectory = GetSolutionDirectory();
            var sourceLanguagesDirectory = Path.Combine(solutionDirectory, "LanguageFiles");
            return Version.Parse(File.ReadAllText(Path.Combine(sourceLanguagesDirectory, "Version.txt")));
        }
        catch
        {
            MessageBox.Show("Failed get local version!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private static void SetLocalVersion(Version version)
    {
        var solutionDirectory = GetSolutionDirectory();
        var sourceLanguagesDirectory = Path.Combine(solutionDirectory, "LanguageFiles");
        File.WriteAllText(Path.Combine(sourceLanguagesDirectory, "Version.txt"), version.ToString());
    }

    private static async Task<Version> GetRemoteVersion()
    {
        try
        {
            const string url = "https://storage.modplus.org/Languages/Langs.xml";
            var str = await new HttpClient().GetStringAsync(url);

            return !string.IsNullOrEmpty(str) 
                ? Version.Parse(XElement.Parse(str).Elements("lang").First().Attribute("Version")!.Value) 
                : null;
        }
        catch
        {
            return null;
        }
    }
}