using System.Data;
using Microsoft.Data.SqlClient;

namespace GajyunETL
{
    public class PdfProcessor
    {
        private string connectionString;
        private string pdfFolderPath;
        private readonly LogService _logService;

        public PdfProcessor(string connectionString, string pdfFolderPath, LogService logService)
        {
            this.connectionString = connectionString;
            this.pdfFolderPath = pdfFolderPath;
            this._logService = logService;
        }

        public async Task<bool> ProcessPdfFiles()
        {
            var pdfFiles = Directory.GetFiles(pdfFolderPath, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                await _logService.InsertLogAsync("INFO", "ProcessPdfFiles", "No PDF files found in the specified folder.", "", "IP not applicable", "PdfProcessor", "PDF processing");
                return false; // Retorna false se nenhum arquivo for encontrado
            }

            bool anyProcessed = false;

            foreach (var pdfFilePath in pdfFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pdfFilePath);
                byte[] pdfContent = File.ReadAllBytes(pdfFilePath);

                try
                {
                    if (UpdateDatabaseWithPdf(fileNameWithoutExtension, pdfContent))
                    {
                        File.Delete(pdfFilePath); // Exclui o arquivo após o processamento
                        await _logService.InsertLogAsync("INFO", "ProcessPdfFiles", $"PDF {fileNameWithoutExtension} processed and deleted.", "", "IP not applicable", "PdfProcessor", "PDF processing");
                        anyProcessed = true;
                    }
                    else
                    {
                        // Se a atualização do banco de dados falhar, não faz nada com o arquivo (não move para Error)
                        await _logService.InsertLogAsync("WARNING", "ProcessPdfFiles", $"Reservation number {fileNameWithoutExtension} not found. PDF not moved.", "", "IP not applicable", "PdfProcessor", "PDF processing");
                    }
                }

                catch (Exception ex)
                {
                    // Se ocorrer uma exceção durante o processamento, apenas registra o log, não move o arquivo para Error
                    await _logService.InsertLogAsync("ERROR", "ProcessPdfFiles", $"Error processing PDF {fileNameWithoutExtension}: {ex.Message}. PDF not moved.", ex.ToString(), "IP not applicable", "PdfProcessor", "PDF processing");
                }
            }

            return anyProcessed;
        }

        private bool UpdateDatabaseWithPdf(string reservationNumber, byte[] pdfContent)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var commandText = @"UPDATE Booking_Data SET pdf_file = @PdfContent, pdf_date = GETDATE() WHERE [予約番号] = @ReservationNumber";

                using (var command = new SqlCommand(commandText, connection))
                {
                    command.Parameters.Add("@PdfContent", SqlDbType.VarBinary, -1).Value = pdfContent;
                    command.Parameters.AddWithValue("@ReservationNumber", reservationNumber);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
