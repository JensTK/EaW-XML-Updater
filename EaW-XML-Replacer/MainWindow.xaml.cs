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
    private const string? TacticalCameraSpaceModeTag = "<TacticalCamera Name=\"Space_Mode\">";
    private const string? TacticalCameraLandModeTag = "<TacticalCamera Name=\"Land_Mode\">";
    private const string DistanceMaxTag = "<Distance_Max>";
    private const string FovMaxTag = "<Fov_Max>";
    private const string GmcInitialPullbackDistanceTag = "<GMC_InitialPullbackDistance>";
    private const string SpaceFowRevealRangeTag = "<Space_FOW_Reveal_Range>";
    private const string LandFowRevealRangeTag = "<Land_FOW_Reveal_Range>";
    private const string FovDefaultTag = "<Fov_Default>";
    private const string FovMinTag = "<Fov_Min>";
    private DirectoryInfo? sourceDirectory = null;
    private DirectoryInfo? backupDirectory = null;
    private FileInfo[]? xmlFiles = null;
    private FileInfo? TacticalCamerasFile = null;
    private FileInfo? GameConstantsFile = null;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void ToggleEditButtons()
    {
        bool enable = sourceDirectory != null && backupDirectory != null;
        MultGroundViewButton.IsEnabled = enable;
        MultSpaceViewButton.IsEnabled = enable;
        SetGcCameraHeightButton.IsEnabled = GameConstantsFile != null && backupDirectory != null;
        SetGroundCameraFovButton.IsEnabled = TacticalCamerasFile != null && backupDirectory != null;
        SetGroundCameraHeightButton.IsEnabled = TacticalCamerasFile != null && backupDirectory != null;
        SetSpaceCameraFovButton.IsEnabled = TacticalCamerasFile != null && backupDirectory != null;
        SetSpaceCameraHeightButton.IsEnabled = TacticalCamerasFile != null && backupDirectory != null;
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
            TacticalCamerasFile = xmlFiles.FirstOrDefault(file => file.Name.StartsWith("TacticalCameras", StringComparison.InvariantCultureIgnoreCase));
            GameConstantsFile = xmlFiles.FirstOrDefault(file => file.Name.StartsWith("GameConstants", StringComparison.InvariantCultureIgnoreCase));
            FileListLabel.Content = "Detected XML files: " + xmlFiles.Length;
            if (GameConstantsFile is not null)
            {
                GcCameraHeightBox.Text = ReadValueInFile(GameConstantsFile, GmcInitialPullbackDistanceTag)?.ToString() ?? "";
            }
            if (TacticalCamerasFile is not null)
            {
                GroundCameraHeightBox.Text = ReadValueInFile(TacticalCamerasFile, DistanceMaxTag, TacticalCameraLandModeTag)?.ToString() ?? "";
                SpaceCameraHeightBox.Text = ReadValueInFile(TacticalCamerasFile, DistanceMaxTag, TacticalCameraSpaceModeTag)?.ToString() ?? "";
                GroundCameraFovBox.Text = ReadValueInFile(TacticalCamerasFile, FovMaxTag, TacticalCameraLandModeTag)?.ToString() ?? "";
                SpaceCameraFovBox.Text = ReadValueInFile(TacticalCamerasFile, FovMaxTag, TacticalCameraSpaceModeTag)?.ToString() ?? "";
            }
            
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
    
    private void BackupSelectButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            TargetTextBox.Content = dialog.FolderName;
            backupDirectory = new DirectoryInfo(dialog.FolderName);
        }
        ToggleEditButtons();
    }

    private int MultiplyXmlValues(string xmlStartTag, double multiplier)
    {
        if (xmlFiles is null || xmlFiles.Length == 0)
        {
            MessageBox.Show("No XML files found in selected folder.");
            return 0;
        }

        if (sourceDirectory is null || backupDirectory is null)
        {
            throw new Exception("Source and Target are required.");
        }
        
        int filesUpdated = 0;
        
        foreach (var file in xmlFiles)
        {
            if (SetValueInFile(file, xmlStartTag, d => d * multiplier))
            {
                filesUpdated++;
            }
        }
        return filesUpdated;
    }

    private double? ReadValueInFile(FileInfo file, string xmlStartTag, string? xmlSectionTag = null)
    {
        string[] lines = File.ReadAllLines(file.FullName);
        string? xmlSectionEndTag = xmlSectionTag?.Split(' ').First().Replace("<", "</");
        bool isInSection = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();
            if (xmlSectionTag is not null && trimmedLine.StartsWith(xmlSectionTag))
            {
                isInSection = true;
            }
            else if (xmlSectionEndTag is not null && trimmedLine.StartsWith(xmlSectionEndTag))
            {
                isInSection = false;
            }
            if (((xmlSectionTag is not null && isInSection) || xmlSectionTag is null) && trimmedLine.StartsWith(xmlStartTag))
            {
                string valueString = trimmedLine.Substring(xmlStartTag.Length, trimmedLine.Length - (xmlStartTag.Length * 2) - 1);
                return double.Parse(valueString);
            }
        }
        return null;
    }

    private bool SetValueInFile(FileInfo file, string xmlStartTag, Func<double, double> valueFunc, string? xmlSectionTag = null)
    {
        if (sourceDirectory is null || backupDirectory is null)
        {
            throw new Exception("Source and Target are required.");
        }
        if (file.DirectoryName is null)
        {
            throw new Exception("Directory not found.");
        }
        bool fileUpdated = false;
        string[] lines = File.ReadAllLines(file.FullName);
        string[] updatedLines = new string[lines.Length];
        string? xmlSectionEndTag = xmlSectionTag?.Split(' ').First().Replace("<", "</");
        bool isInSection = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();
            if (xmlSectionTag is not null && trimmedLine.StartsWith(xmlSectionTag))
            {
                isInSection = true;
            }
            else if (xmlSectionEndTag is not null && trimmedLine.StartsWith(xmlSectionEndTag))
            {
                isInSection = false;
            }
            if (((xmlSectionTag is not null && isInSection) || xmlSectionTag is null) && trimmedLine.StartsWith(xmlStartTag))
            {
                string valueString = trimmedLine.Substring(xmlStartTag.Length, trimmedLine.IndexOf("</") -xmlStartTag.Length); // ???
                // <Space_FOW_Reveal_Range>6000.0</Space_FOW_Reveal_Range>
                double value = double.Parse(valueString);
                if (value > 0)
                {
                    value = valueFunc(value);
                    updatedLines[i] = line.Replace(valueString, value.ToString("0.0"));
                    //linesUpdated++;
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
            //filesUpdated++;
            DirectoryInfo backupDir = Directory.CreateDirectory(file.DirectoryName.Replace(sourceDirectory.FullName, backupDirectory.FullName));
            string copyPath = backupDir.FullName + "/" + file.Name;
            if (!File.Exists(copyPath))
            {
                File.Copy(file.FullName, copyPath, false);
            }
            File.WriteAllLines(file.FullName, updatedLines);
        }
        return fileUpdated;
    }

    private void MultSpaceViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(MultSpaceViewMultiplierBox.Text, out double multValue))
        {
            int updatedFiles = MultiplyXmlValues(SpaceFowRevealRangeTag, multValue);
            MessageBox.Show($"Updated {updatedFiles} file(s)");
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
            int updatedFiles = MultiplyXmlValues(LandFowRevealRangeTag, multValue);
            MessageBox.Show($"Updated {updatedFiles} file(s)");
        }
        else
        {
            MessageBox.Show("Invalid mult value.");
        }
    }

    private void SetGcCameraHeightButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GameConstantsFile is null)
        {
            throw new Exception("GameConstants file not found.");
        }
        if (double.TryParse(GcCameraHeightBox.Text, out double setValue))
        {
            if (SetValueInFile(GameConstantsFile, GmcInitialPullbackDistanceTag, _ => setValue))
            {
                MessageBox.Show("Value set.");
            }
            else
            {
                MessageBox.Show("Value not set.");
            }
        }
        else
        {
            MessageBox.Show("Invalid value.");
        }
    }

    private void SetGroundCameraHeightButton_OnClick(object sender, RoutedEventArgs e)
    {
        // <Distance_Max> <TacticalCamera Name="Land_Mode">
        if (TacticalCamerasFile is null)
        {
            throw new Exception("GameConstants file not found.");
        }
        if (double.TryParse(GroundCameraHeightBox.Text, out double setValue))
        {
            if (SetValueInFile(TacticalCamerasFile, DistanceMaxTag, _ => setValue, TacticalCameraLandModeTag)) 
            {
                MessageBox.Show("Value set.");
            }
            else
            {
                MessageBox.Show("Value not set.");
            }
        }
        else
        {
            MessageBox.Show("Invalid value.");
        }
    }

    private void SetGroundCameraFovButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Disse er i samme fil men forskjellige sections...
        // <Fov_Default>65.0</Fov_Default>
        // <Fov_Max>65.0</Fov_Max>
        // <Fov_Min>65.0</Fov_Min>
        if (TacticalCamerasFile is null)
        {
            throw new Exception("GameConstants file not found.");
        }
        if (double.TryParse(GroundCameraFovBox.Text, out double setValue))
        {
            if (SetValueInFile(TacticalCamerasFile, FovDefaultTag, _ => setValue, TacticalCameraLandModeTag)
                && SetValueInFile(TacticalCamerasFile, FovMaxTag, _ => setValue, TacticalCameraLandModeTag)
                && SetValueInFile(TacticalCamerasFile, FovMinTag, _ => setValue, TacticalCameraLandModeTag))
            {
                MessageBox.Show("Value set.");
            }
            else
            {
                MessageBox.Show("Value not set.");
            }
        }
        else
        {
            MessageBox.Show("Invalid value.");
        }
    }

    private void SetSpaceCameraHeightButton_OnClick(object sender, RoutedEventArgs e)
    {
        // <TacticalCamera Name="Space_Mode">
        if (TacticalCamerasFile is null)
        {
            throw new Exception("GameConstants file not found.");
        }
        if (double.TryParse(SpaceCameraHeightBox.Text, out double setValue))
        {
            if (SetValueInFile(TacticalCamerasFile, DistanceMaxTag, _ => setValue, TacticalCameraSpaceModeTag))
            {
                MessageBox.Show("Value set.");
            }
            else
            {
                MessageBox.Show("Value not set.");
            }
        }
        else
        {
            MessageBox.Show("Invalid value.");
        }
    }

    private void SetSpaceCameraFovButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TacticalCamerasFile is null)
        {
            throw new Exception("GameConstants file not found.");
        }
        if (double.TryParse(SpaceCameraFovBox.Text, out double setValue))
        {
            if (SetValueInFile(TacticalCamerasFile, FovDefaultTag, _ => setValue, TacticalCameraSpaceModeTag)
                && SetValueInFile(TacticalCamerasFile, FovMaxTag, _ => setValue, TacticalCameraSpaceModeTag)
                && SetValueInFile(TacticalCamerasFile, FovMinTag, _ => setValue, TacticalCameraSpaceModeTag))
            {
                MessageBox.Show("Value set.");
            }
            else
            {
                MessageBox.Show("Value not set.");
            }
        }
        else
        {
            MessageBox.Show("Invalid value.");
        }
    }
}