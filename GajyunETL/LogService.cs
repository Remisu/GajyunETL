using System.Data.SqlClient;

public class LogService
{
    private readonly string _connectionString;

    public LogService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertLogAsync(string severityLevel, string action, string logMessage, string eventDetails, string userIP, string entityReference, string category)
    {
        var query = @"INSERT INTO Logs (Timestamp, SeverityLevel, Action, LogMessage, EventDetails, UserIP, EntityReference, Category) 
                      VALUES (@Timestamp, @SeverityLevel, @Action, @LogMessage, @EventDetails, @UserIP, @EntityReference, @Category)";

        using (var connection = new SqlConnection(_connectionString))
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Timestamp", DateTime.Now);
            command.Parameters.AddWithValue("@SeverityLevel", severityLevel);
            command.Parameters.AddWithValue("@Action", action);
            command.Parameters.AddWithValue("@LogMessage", logMessage);
            command.Parameters.AddWithValue("@EventDetails", eventDetails);
            command.Parameters.AddWithValue("@UserIP", userIP);
            command.Parameters.AddWithValue("@EntityReference", entityReference);
            command.Parameters.AddWithValue("@Category", category);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }


}
