using System.IO;
using System.Windows;
using System.Windows.Forms;
using static GajyunWPF.Settings;

namespace GajyunWPF
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private AppSettings _settings;
        private const string ConfigFilePath = @"..\..\..\..\GajyunETL\bin\Debug\net8.0\appsettings.json";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ConfigurationWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(ConfigFilePath))
            {
                _settings = AppSettings.Load(ConfigFilePath);
                pdfFolderPathTextBox.Text = _settings.FilePaths.PdfFolderPath;
                CsvPathTextBox.Text = _settings.FilePaths.CsvPath;
            }
            else
            {
                System.Windows.MessageBox.Show("設定ファイルが見つかりませんでした。"); //The configuration file could not be found.
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.FilePaths.PdfFolderPath = pdfFolderPathTextBox.Text;
            //_settings.FilePaths.OutputFolderPath = outputFolderPathTextBox.Text;
            _settings.FilePaths.CsvPath = CsvPathTextBox.Text;

            _settings.Save(ConfigFilePath);
            System.Windows.MessageBox.Show("設定が正常に保存されました。"); //Settings saved successfully.
            this.Close();
        }

        private void BrowsePdfFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "パスポートフォルダーのパスを選択してください。"; //Select Passaport Folder Path
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    pdfFolderPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseCsvFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "CSVフォルダーのパスを選択してください。"; //Select CSV Folder Path
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    CsvPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }
    }

}
