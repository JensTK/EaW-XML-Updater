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
    private FileInfo[]? xmlFiles;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void FolderSelectButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            FolderTextBox.Content = dialog.FolderName;
            DirectoryInfo dir = new DirectoryInfo(dialog.FolderName);
            //xmlFiles = dir.GetFiles("*.xml", SearchOption.AllDirectories);
            xmlFiles = dir.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            FileListLabel.Content = "Detected XML files: " + xmlFiles.Length;
            foreach (var file in xmlFiles)
            {
                //Console.WriteLine(file.FullName);
                Label fileNameLabel = new Label();
                fileNameLabel.Height = 25;
                fileNameLabel.Content = file.Name;
                FileListPanel.Children.Add(fileNameLabel);
            }
        }
    }

    private void MultiplyXmlValues(string xmlStartTag, double multiplier)
    {
        if (xmlFiles is null || xmlFiles.Length == 0)
        {
            MessageBox.Show("No XML files found in selected folder.");
            return;
        }
        int linesUpdated = 0;
        int filesUpdated = 0;
        DirectoryInfo? writeDir = null;
        
        foreach (var file in xmlFiles)
        {
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
                    value *= multiplier;
                    updatedLines[i] = line.Replace(valueString, value.ToString("0.0"));
                    linesUpdated++;
                    fileUpdated = true;
                }
                else
                {
                    updatedLines[i] = line;
                }
            }
            if (fileUpdated)
            {
                filesUpdated++;
                if (writeDir is null)
                {
                    writeDir = Directory.CreateDirectory(file.DirectoryName + "/updated");
                }
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
            MultiplyXmlValues("<Ground_FOW_Reveal_Range>", multValue);
        }
        else
        {
            MessageBox.Show("Invalid mult value.");
        }
    }
}