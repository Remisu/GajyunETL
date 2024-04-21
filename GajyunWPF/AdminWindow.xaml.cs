using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static GajyunWPF.Settings;

namespace GajyunWPF
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private string _connectionString;
#pragma warning disable CS0169 // The field 'AdminWindow._settings' is never used
        private AppSettings _settings;
#pragma warning restore CS0169 // The field 'AdminWindow._settings' is never used
        private LogService _logService;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AdminWindow(string connectionString)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();
            InitializeServices();  // Assegura a inicialização dos serviços necessários
        }

        private void InitializeServices()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _connectionString = "Server=localhost;Database=GajyunDb;Trusted_Connection=True;TrustServerCertificate=True;";

                _logService = new LogService(_connectionString);
            }
            catch (Exception ex)
            {
                LogError(ex); // Log any initialization errors.
                MessageBox.Show("サービスの初期化中にエラーが発生しました。エラーログを確認してください。"); // An error occurred during service initialization. Please check the error log.
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            TextBox textBox = sender as TextBox;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Label placeholderLabel = this.FindName(textBox.Name + "Placeholder") as Label;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (placeholderLabel != null && string.IsNullOrEmpty(textBox.Text))
                placeholderLabel.Visibility = Visibility.Collapsed;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            TextBox textBox = sender as TextBox;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Label placeholderLabel = this.FindName(textBox.Name + "Placeholder") as Label;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (placeholderLabel != null && string.IsNullOrEmpty(textBox.Text))
                placeholderLabel.Visibility = Visibility.Visible;
        }

        // Employee Events
        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("INSERT INTO Employee (Name, Position) VALUES (@Name, @Position)", conn);
                cmd.Parameters.AddWithValue("@Name", txtEmployeeName.Text);
                cmd.Parameters.AddWithValue("@Position", txtEmployeePosition.Text);

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            LoadEmployees(); // Método para recarregar os dados no DataGrid
        }

        private void UpdateEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Atualizar um funcionário selecionado no banco de dados
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            // Deletar um funcionário selecionado no banco de dados
        }

        // ChecklistType Events
        private void AddChecklistType_Click(object sender, RoutedEventArgs e)
        {
            // Adicionar um novo tipo de checklist ao banco de dados
        }

        private void UpdateChecklistType_Click(object sender, RoutedEventArgs e)
        {
            // Atualizar um tipo de checklist selecionado no banco de dados
        }

        private void DeleteChecklistType_Click(object sender, RoutedEventArgs e)
        {
            // Deletar um tipo de checklist selecionado no banco de dados
        }

        // TaskType Events
        private void AddTaskType_Click(object sender, RoutedEventArgs e)
        {
            // Adicionar um novo tipo de tarefa ao banco de dados
        }

        private void UpdateTaskType_Click(object sender, RoutedEventArgs e)
        {
            // Atualizar um tipo de tarefa selecionado no banco de dados
        }

        private void DeleteTaskType_Click(object sender, RoutedEventArgs e)
        {
            // Deletar um tipo de tarefa selecionado no banco de dados
        }

        // Task Events
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            // Adicionar uma nova tarefa ao banco de dados
        }

        private void UpdateTask_Click(object sender, RoutedEventArgs e)
        {
            // Atualizar uma tarefa selecionada no banco de dados
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            // Deletar uma tarefa selecionada no banco de dados
        }


        // Manipulador para a seleção de Employee mudar
        private void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees.SelectedItem != null)
            {
                // Exemplo: Preencher TextBoxes com os valores do item selecionado
                // Employee selectedEmployee = dgEmployees.SelectedItem as Employee;
                // txtEmployeeName.Text = selectedEmployee.Name;
                // txtEmployeePosition.Text = selectedEmployee.Position;
            }
        }

        // Manipulador para a seleção de ChecklistType mudar
        private void dgChecklistTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgChecklistTypes.SelectedItem != null)
            {
                // Exemplo: Preencher TextBoxes com os valores do item selecionado
                // ChecklistType selectedType = dgChecklistTypes.SelectedItem as ChecklistType;
                // txtChecklistTypeDescription.Text = selectedType.Description;
            }
        }

        // Manipulador para a seleção de TaskType mudar
        private void dgTaskTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTaskTypes.SelectedItem != null)
            {
                // Exemplo: Preencher TextBoxes com os valores do item selecionado
                // TaskType selectedTaskType = dgTaskTypes.SelectedItem as TaskType;
                // txtTaskTypeDescription.Text = selectedTaskType.Description;
            }
        }

        // Manipulador para a seleção de Task mudar
        private void dgTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTasks.SelectedItem != null)
            {
                // Exemplo: Preencher TextBoxes com os valores do item selecionado
                // Task selectedTask = dgTasks.SelectedItem as Task;
                // txtTaskDescription.Text = selectedTask.Description;
            }
        }
        private void LogError(Exception ex)
        {
            var logPath = "errorLog.txt"; // O caminho para o arquivo de log de erro.
            var message = $"{DateTime.Now}: {ex.ToString()}\n"; // Formata a mensagem de log com a data atual e a exceção.
            File.AppendAllText(logPath, message); // Escreve a mensagem de log no arquivo, anexando ao conteúdo existente.
        }
        private void LoadEmployees()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand("SELECT EmployeeID, Name, Position FROM Employee", conn);
                var adapter = new SqlDataAdapter(cmd);
                var table = new DataTable();
                adapter.Fill(table);

                dgEmployees.ItemsSource = table.DefaultView;
            }
        }
    }
}

