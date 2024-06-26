using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

var topDir = Registry.CurrentUser.OpenSubKey("Software\\ModPlus")?.GetValue("TopDir")?.ToString();

if (string.IsNullOrEmpty(topDir) || !Directory.Exists(topDir))
{
    Console.WriteLine("Installed ModPlus not found!");
    Console.ReadKey();
    return;
}

var targetLangDirectory = Path.Combine(topDir, "Languages");
Directory.CreateDirectory(targetLangDirectory);

Console.WriteLine($"Target languages directory: {targetLangDirectory}");

foreach (var file in Directory.GetFiles(targetLangDirectory, "*.xml", SearchOption.TopDirectoryOnly))
{
    try
    {
        File.Delete(file);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Failed delete file {file}.\nException: {exception.Message}.\nDelete it manualy and try again");
        Console.ReadKey();
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
        Console.WriteLine($"Process language: {langName}");

        var resultDoc = new XElement("ModPlus");
        resultDoc.SetAttributeValue("Name", langName);
        resultDoc.SetAttributeValue("Version", version);
        
        foreach (var fileName in fileNames)
        {
            Console.WriteLine($"    Process part: {fileName}");

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
        Console.WriteLine($"Language file for {langName} created");
    }
}
catch (Exception exception)
{
    Console.WriteLine(exception.Message);
}

Console.WriteLine();
Console.WriteLine("Press any key for exit");
Console.ReadKey();

static string GetSolutionDirectory()
{
    var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var parent = Directory.GetParent(executingDirectory);
    while (parent.Name != "LanguageFiles" && parent.Name != "Core_LanguageFiles")
    {
        parent = Directory.GetParent(parent.FullName);
    }

    return parent.FullName;
}