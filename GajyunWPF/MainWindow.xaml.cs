using GajyunETL;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using static GajyunWPF.Settings;


namespace GajyunWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings _settings;

        private CsvProcessor _csvProcessor;
        private PdfProcessor _pdfProcessor;
        private string _connectionString;
        private LogService _logService;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            try
            {
                InitializeComponent();
                LoadSettings(); // Carrega as configurações ao iniciar
                InitializeServices();
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                LogError(ex); // Log any initialization errors.
                MessageBox.Show("起動時にエラーが発生しました。エラーログを確認してください。"); //An error occurred on startup. Please check the error log.
            }
        }

        private void InitializeServices()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _connectionString = "Server=localhost;Database=GajyunDb;Trusted_Connection=True;TrustServerCertificate=True;";

                // Executar limpeza de dados antigos
                ExecuteSqlCommand("DELETE FROM [dbo].[Booking_Data] WHERE [チェックアウト日] < DATEADD(YEAR, -3, GETDATE());");

                // Executar backup do banco de dados
                BackupDatabase();

                _logService = new LogService(_connectionString);
                _csvProcessor = new CsvProcessor(_connectionString, _logService);
                _pdfProcessor = new PdfProcessor(_connectionString, _settings.FilePaths.PdfFolderPath, _logService);
            }
            catch (Exception ex)
            {
                LogError(ex); // Log any initialization errors.
                MessageBox.Show("サービスの初期化中にエラーが発生しました。エラーログを確認してください。"); // An error occurred during service initialization. Please check the error log.
            }
        }

        private void ExecuteSqlCommand(string commandText)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(commandText, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void BackupDatabase()
        {
            var backupCommandText = @"BACKUP DATABASE [GajyunDb] TO DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\GajyunDb.bak' WITH NOFORMAT, INIT, NAME = N'GajyunDb-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
            ExecuteSqlCommand(backupCommandText);
        }

        private void LoadSettings()
        {
            // Supondo que você tem o caminho do arquivo de configurações acessível aqui
            _settings = AppSettings.Load(@"..\..\..\..\GajyunETL\bin\Debug\net8.0\appsettings.json");
        }

        private async void ProcessCsvButton_Click(object sender, RoutedEventArgs e)
        {
            processCsvButton.IsEnabled = false;

            // Carrega as configurações diretamente antes de iniciar o processamento
            _settings = AppSettings.Load(@"..\..\..\..\GajyunETL\bin\Debug\net8.0\appsettings.json");

            try
            {
                // Agora usa o caminho atualizado das configurações
                string csvPath = _settings.FilePaths.CsvPath;
                bool processed = await _csvProcessor.ProcessCsvFiles(csvPath);

                // Verifica se algum arquivo foi processado e exibe a mensagem apropriada
                if (processed)
                {
                    MessageBox.Show("CSVファイルの処理が正常に完了しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information); //CSV file processing completed successfully!
                    ReloadSettingsAndData();
                }
                else
                {
                    MessageBox.Show("CSVは処理されませんでした。", "情報", MessageBoxButton.OK, MessageBoxImage.Information); //CSV was not processed.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSVファイルの処理に失敗しました：{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); //Failed to process CSV files: 
            }
            finally
            {
                processCsvButton.IsEnabled = true;
            }

        }


        private async void ProcessPdfButton_Click(object sender, RoutedEventArgs e)
        {
            processPdfButton.IsEnabled = false; // Certifique-se de que você tem um controle com este nome ou ajuste conforme necessário

            // Carrega as configurações diretamente antes de iniciar o processamento
            _settings = AppSettings.Load(@"..\..\..\..\GajyunETL\bin\Debug\net8.0\appsettings.json");

            try
            {
                // Atualiza a instância do PdfProcessor com o caminho e logService atualizados
                _pdfProcessor = new PdfProcessor(_connectionString, _settings.FilePaths.PdfFolderPath, _logService);

                // Chama o método para processar arquivos PDF de forma assíncrona
                bool processed = await _pdfProcessor.ProcessPdfFiles();
                if (processed)
                {
                    MessageBox.Show("PDFファイルの処理が正常に完了しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information); //PDF file processing completed successfully!
                    ReloadSettingsAndData();
                }
                else
                {
                    MessageBox.Show("処理するPDFファイルが見つかりませんでした。", "情報", MessageBoxButton.OK, MessageBoxImage.Information); //No PDF files were found to process.
                }
            }
            catch (Exception ex)
            {
                // Display a message if an error occurs
                MessageBox.Show($"PDFファイルの処理に失敗しました：{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); //Failed to process PDF files:
            }
            finally
            {
                processPdfButton.IsEnabled = true;
            }
        }



        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigurationWindow();
            configWindow.Show();
        }

        private async Task<DateTime?> GetLastProcessedDateAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var commandText = "SELECT MAX(COALESCE(CreatedDate, UpdatedDate)) FROM Booking_Data";
                using (var command = new SqlCommand(commandText, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return result as DateTime?;
                }
            }
        }

        private async void UpdateLastProcessedDate()
        {
            var lastDate = await GetLastProcessedDateAsync();
            lastProcessLabel.Content = $"最後のCSVインポート： {(lastDate.HasValue ? lastDate.Value.ToString("g") : "未処理")}";
        }



        private async Task<DateTime?> GetLastPdfProcessedDateAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Supondo que os dados dos PDFs estejam em uma tabela chamada Pdf_Data e o campo da data seja pdf_date
                var commandText = "SELECT MAX(pdf_date) FROM Booking_Data";
                using (var command = new SqlCommand(commandText, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return result as DateTime?;
                }
            }
        }

        private async void UpdateLastPdfProcessedDate()
        {
            var lastPdfDate = await GetLastPdfProcessedDateAsync();
            lastProcessLabelPDF.Content = $"最後のPDFインポート： {(lastPdfDate.HasValue ? lastPdfDate.Value.ToString("g") : "未処理")}";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLastProcessedDate(); // Atualiza a data do último processamento de CSV
            UpdateLastPdfProcessedDate(); // Atualiza a data do último processamento de PDF
        }

        private void ReloadSettingsAndData()
        {
            // Recarrega as configurações
            LoadSettings();

            // Atualiza a interface do usuário baseada nas configurações recarregadas
            UpdateLastProcessedDate();
            UpdateLastPdfProcessedDate();
        }

        private void SearchClientButton_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new SearchWindow(_connectionString);
            searchWindow.Show();
        }

        private void LogError(Exception ex)
        {
            var logPath = "errorLog.txt"; // O caminho para o arquivo de log de erro.
            var message = $"{DateTime.Now}: {ex.ToString()}\n"; // Formata a mensagem de log com a data atual e a exceção.
            File.AppendAllText(logPath, message); // Escreve a mensagem de log no arquivo, anexando ao conteúdo existente.
        }

        private void OtherButton_Click(object sender, RoutedEventArgs e)
        {
            var adminWindow = new AdminWindow(_connectionString);
            adminWindow.Show();
        }
    }
}