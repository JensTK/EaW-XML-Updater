using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace EaW_XML_Replacer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DirectoryInfo? sourceDirectory = null;
    private DirectoryInfo? targetDirectory = null;
    private FileInfo[]? xmlFiles = null;
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void ToggleEditButtons()
    {
        bool enable = sourceDirectory != null && targetDirectory != null;
        MultGroundViewButton.IsEnabled = enable;
        MultSpaceViewButton.IsEnabled = enable;
    }

    private void ToggleEditButtons(bool enabled)
    {
        MultGroundViewButton.IsEnabled = enabled;
        MultSpaceViewButton.IsEnabled = enabled;
    }

    private void SourceSelectButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SourceTextBox.Content = dialog.FolderName;
            sourceDirectory = new DirectoryInfo(dialog.FolderName);
            //xmlFiles = dir.GetFiles("*.xml", SearchOption.AllDirectories);
            xmlFiles = sourceDirectory.GetFiles("*.xml", SearchOption.AllDirectories);
            FileListLabel.Content = "Detected XML files: " + xmlFiles.Length;
            /*foreach (var file in xmlFiles)
            {
                //Console.WriteLine(file.FullName);
                Label fileNameLabel = new Label();
                fileNameLabel.Height = 25;
                fileNameLabel.Content = file.Name;
                FileListPanel.Children.Add(fileNameLabel);
            }*/
        }
        ToggleEditButtons();
    }
    
    private void TargetSelectButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            TargetTextBox.Content = dialog.FolderName;
            targetDirectory = new DirectoryInfo(dialog.FolderName);
        }
        ToggleEditButtons();
    }

    private void MultiplyXmlValues(string xmlStartTag, double multiplier)
    {
        // replikere mappestrukturen i target
        if (xmlFiles is null || xmlFiles.Length == 0)
        {
            MessageBox.Show("No XML files found in selected folder.");
            return;
        }

        if (sourceDirectory is null || targetDirectory is null)
        {
            throw new Exception("Source and Target are required.");
        }
        
        int linesUpdated = 0;
        int filesUpdated = 0;
        
        foreach (var file in xmlFiles)
        {
            if (file.DirectoryName is null)
            {
                throw new Exception("Directory not found.");
            }
            bool fileUpdated = false;
            string[] lines = File.ReadAllLines(file.FullName);
            string[] updatedLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith(xmlStartTag))
                {
                    string valueString = trimmedLine.Substring(xmlStartTag.Length, trimmedLine.Length - (xmlStartTag.Length * 2) - 1); // ???
                    // <Space_FOW_Reveal_Range>6000.0</Space_FOW_Reveal_Range>
                    double value = double.Parse(valueString);
                    if (value > 0)
                    {
                        value *= multiplier;
                        updatedLines[i] = line.Replace(valueString, value.ToString("0.0"));
                        linesUpdated++;
                        fileUpdated = true;
                    }
                }
                else
                {
                    updatedLines[i] = line;
                }
            }
            if (fileUpdated)
            {
                filesUpdated++;
                DirectoryInfo writeDir = Directory.CreateDirectory(file.DirectoryName.Replace(sourceDirectory.FullName, targetDirectory.FullName));
                File.WriteAllLines(writeDir.FullName + "/" + file.Name, updatedLines);
            }
        }
        MessageBox.Show($"Updated {linesUpdated} vales in {filesUpdated} files.");
    }

    private void MultSpaceViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MultSpaceViewMultiplierBox.Text, out double multValue))
        {
            MultiplyXmlValues("<Space_FOW_Reveal_Range>", multValue);
        }
        else
        {
            MessageBox.Show("Invalid mult value.");
        }
    }

    private void MultGroundViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MultGroundViewMultiplierBox.Text, out double multValue))
        {
            MultiplyXmlValues("<Land_FOW_Reveal_Range>", multValue);
        }
        else
        {
            MessageBox.Show("Invalid mult value.");
        }
    }
}