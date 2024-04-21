using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using CsvHelper;
using CsvHelper.Configuration;
using System.Net;
using System.Data;

namespace GajyunETL
{
    public class CsvProcessor
    {
        private string connectionString;
        private readonly LogService _logService;

        public CsvProcessor(string connectionString, LogService logService)
        {
            this.connectionString = connectionString;
            this._logService = logService;

            // Obtenção do endereço IP do servidor no momento da criação do CsvProcessor
            var host = Dns.GetHostEntry(Dns.GetHostName());
        }

        public async Task<bool> ProcessCsvFiles(string folderPath)
        {
            bool anyFileProcessed = false;
            var csvFiles = Directory.GetFiles(folderPath, "*.csv");

            // Data de hoje para comparação com a data de criação do arquivo
            var today = DateTime.Today;

            if (csvFiles.Length == 0)
            {
                await _logService.InsertLogAsync("INFO", "ProcessCsvFiles", "No CSV files found in the specified folder.", "", "IP not applicable", "CsvProcessor", "CSV processing");
                return anyFileProcessed;
            }

            foreach (var file in csvFiles)
            {
                // Verificar se o nome do arquivo possui 14 dígitos numéricos
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Length == 14 && fileName.All(char.IsDigit))
                {
                    // Verificar se a data de criação do arquivo é igual a hoje
                    var creationTime = File.GetCreationTime(file).Date;
                    if (creationTime == today)
                    {
                        bool success = true;
                        try
                        {
                            using (var reader = new StreamReader(file, Encoding.GetEncoding("shift_jis")))
                            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ",", Encoding = Encoding.GetEncoding("shift_jis") }))
                            {
                                var records = csv.GetRecords<dynamic>();
                                await InsertRecordsIntoDatabase(records, connectionString);
                                await _logService.InsertLogAsync("INFO", "ProcessCsvFiles", $"CSV file processed successfully: {file}", "", "IP not applicable", "CsvProcessor", "CSV processing");
                                anyFileProcessed = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            await LogError("ProcessCsvFiles", $"Error processing CSV file: {file}", ex);
                        }
                        finally
                        {
                            if (success)
                            {
                                File.Delete(file); // Apaga o arquivo após o processamento
                            }
                        }
                    }
                }
            }

            if (anyFileProcessed)
            {
                await _logService.InsertLogAsync("INFO", "ProcessCsvFiles", "All CSV files were processed successfully.", "", "IP not applicable", "CsvProcessor", "CSV processing");
            }

            return anyFileProcessed;
        }


        //private void MoveFileToFolder(string file, string folderPath, string subFolder)
        //{
        //    var destinationFolder = Path.Combine(folderPath, subFolder);
        //    if (!Directory.Exists(destinationFolder))
        //    {
        //        Directory.CreateDirectory(destinationFolder);
        //    }

        //    var fileName = Path.GetFileName(file);
        //    var destinationFilePath = Path.Combine(destinationFolder, fileName);

        //    if (File.Exists(destinationFilePath))
        //    {
        //        File.Delete(destinationFilePath); // Remove o arquivo de destino se já existir
        //    }

        //    File.Move(file, destinationFilePath);
        //}



        private async Task InsertRecordsIntoDatabase(IEnumerable<dynamic> records, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                foreach (var record in records)
                {
                    var reservationType = record.予約区分;
                    var reservationNumber = record.予約番号;

                    // Verifica a existência da reserva na base de dados.
                    var checkExistenceCommandText = @"
                SELECT TOP 1 * FROM Booking_Data
                WHERE 予約番号 = @ResNumber";

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    dynamic existingRecord = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                    using (var checkExistenceCommand = new SqlCommand(checkExistenceCommandText, connection))
                    {
                        checkExistenceCommand.Parameters.AddWithValue("@ResNumber", reservationNumber);
                        using (var reader = await checkExistenceCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingRecord = new
                                {
                                    変更日時 = reader["変更日時"],
                                    pdf_file = reader["pdf_file"],
                                    pdf_date = reader["pdf_date"]
                                };
                            }
                        }
                    }

                    switch (reservationType)
                    {
                        case "キャンセル": // Reserva Cancelada
                            if (existingRecord != null)
                            {
                                var deleteCommandText = @"
                                                        DELETE FROM Booking_Data
                                                        WHERE 予約番号 = @ResNumber";
                                using (var deleteCommand = new SqlCommand(deleteCommandText, connection))
                                {
                                    deleteCommand.Parameters.AddWithValue("@ResNumber", reservationNumber);
                                    await deleteCommand.ExecuteNonQueryAsync();
                                }
                            }
                            break;

                        case "予約": // Nova Reserva
                            if (existingRecord == null)
                            {
                                await InsertNewRecord(record, connection);
                            }
                            break;

                        case "予約変更": // Alteração de Reserva
                            DateTime newChangeDateTime;
                            bool parseResultNewChangeDateTime = DateTime.TryParse(record.変更日時, out newChangeDateTime);

                            if (existingRecord != null)
                            {
                                DateTime existingChangeDateTime;
                                // Se o registro existir, verifica a data de alteração
                                bool parseResultExistingChangeDateTime = DateTime.TryParse(existingRecord?.変更日時?.ToString(), out existingChangeDateTime);

                                if (parseResultNewChangeDateTime && parseResultExistingChangeDateTime && newChangeDateTime > existingChangeDateTime)
                                {
                                    // Se a data de alteração do novo registro for posterior, atualiza o registro existente
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                    await UpdateRecordPreservingPdf(record, connection, existingRecord.pdf_file, existingRecord.pdf_date);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                }
                                // Se a data de alteração do novo registro não for posterior, não faz nada (ou loga que a alteração é redundante)
                            }
                            else
                            {
                                // Se o registro não existir, insere como nova reserva
                                if (parseResultNewChangeDateTime)
                                {
                                    await InsertNewRecord(record, connection);
                                }
                                else
                                {
                                    // Lide com o caso em que a conversão da data falha para um registro novo
                                    await LogError("InsertRecordsIntoDatabase", $"Failed to parse change date for new or updated record with reservation number: {record.予約番号}", new Exception("Date parsing failed for new or updated record"));
                                }
                            }
                            break;

                    }
                }
            }
        }

        private async Task InsertNewRecord(dynamic record, SqlConnection connection)
        {
            var commandText = @"
                            INSERT INTO Booking_Data
                            (予約サイト名, 予約番号, 予約日時, 予約区分, チェックイン日, チェックアウト日, 宿泊者氏名, 部屋名称, 部屋数, 大人人数,
                            子供人数, 合計料金, ポイント, 請求料金, 支払方法, 泊数, 変更日時, キャンセル日時, 宿泊者氏名カナ, チェックイン時刻, 会員氏名カナ, 郵便番号, 住所,
                            電話番号, FAX番号, メールアドレス, プラン名称, 備考, 部屋マスタ, 会員氏名, メモ, 施設名, 部屋記号,
                            プラン記号, 連泊情報, 部屋料金情報, 男性人数, 女性人数, 会員住所, 会員電話番号, 会員FAX番号, 会員メールアドレス, 会員性別,
                            会員年齢, 会員会社名, 会員会社郵便番号, 会員会社住所, 会員会社電話番号, 会員会社FAX番号, 食事条件, 詳細, pdf_file, pdf_date, CreatedDate)
                            VALUES (@Provider, @ResNumber, @ResDateTime, @ResType, @CheckInDate, @CheckOutDate, @GuestName, @RoomName, @RoomCount, @AdultCount,
                            @ChildCount, @TotalFee, @Points, @Charge, @PaymentMethod, @Nights, @ChangeDateTime, @CancelDateTime, @GuestNameKana, @CheckInTime, @MemberNameKana, @PostalCode, @Address,
                            @PhoneNumber, @FaxNumber, @EmailAddress, @PlanName, @Remark, @RoomMaster, @MemberName, @Memo, @FacilityName, @RoomSymbol,
                            @PlanSymbol, @ConsecutiveNightsInfo, @RoomRateInfo, @MaleCount, @FemaleCount, @MemberAddress, @MemberPhoneNumber, @MemberFaxNumber, @MemberEmailAddress, @MemberGender,
                            @MemberAge, @MemberCompanyName, @MemberCompanyPostalCode, @MemberCompanyAddress, @MemberCompanyPhoneNumber, @MemberCompanyFaxNumber, @MealConditions, @Detail, NULL, NULL, @CreatedDate);";

            using (var command = new SqlCommand(commandText, connection))
            {
                command.Parameters.AddWithValue("@Provider", record.予約サイト名);
                command.Parameters.AddWithValue("@ResNumber", record.予約番号);
                command.Parameters.AddWithValue("@ResDateTime", record.予約日時);
                command.Parameters.AddWithValue("@ResType", record.予約区分);
                command.Parameters.AddWithValue("@CheckInDate", record.チェックイン日);
                command.Parameters.AddWithValue("@CheckOutDate", record.チェックアウト日);
                command.Parameters.AddWithValue("@GuestName", record.宿泊者氏名);
                command.Parameters.AddWithValue("@RoomCount", record.部屋数);
                command.Parameters.AddWithValue("@AdultCount", record.大人人数);
                command.Parameters.AddWithValue("@ChildCount", record.子供人数);
                command.Parameters.AddWithValue("@TotalFee", record.合計料金);
                command.Parameters.AddWithValue("@Points", record.ポイント);
                command.Parameters.AddWithValue("@Charge", record.請求料金);
                command.Parameters.AddWithValue("@PaymentMethod", record.支払方法);
                command.Parameters.AddWithValue("@Nights", record.泊数);
                command.Parameters.AddWithValue("@ChangeDateTime", record.変更日時);
                command.Parameters.AddWithValue("@CancelDateTime", record.キャンセル日時);
                command.Parameters.AddWithValue("@Address", record.住所);
                command.Parameters.AddWithValue("@GuestNameKana", record.宿泊者氏名カナ);
                command.Parameters.AddWithValue("@PhoneNumber", record.電話番号);
                command.Parameters.AddWithValue("@EmailAddress", record.メールアドレス);
                command.Parameters.AddWithValue("@RoomMaster", record.部屋マスタ);
                command.Parameters.AddWithValue("@RoomName", record.部屋名称);
                command.Parameters.AddWithValue("@CheckInTime", record.チェックイン時刻);
                command.Parameters.AddWithValue("@MemberNameKana", record.会員氏名カナ);
                command.Parameters.AddWithValue("@PostalCode", record.郵便番号);
                command.Parameters.AddWithValue("@FaxNumber", record.FAX番号);
                command.Parameters.AddWithValue("@PlanName", record.プラン名称);
                command.Parameters.AddWithValue("@Remark", record.備考);
                command.Parameters.AddWithValue("@MemberName", record.会員氏名);
                command.Parameters.AddWithValue("@Memo", record.メモ);
                command.Parameters.AddWithValue("@FacilityName", record.施設名);
                command.Parameters.AddWithValue("@RoomSymbol", record.部屋記号);
                command.Parameters.AddWithValue("@PlanSymbol", record.プラン記号);
                command.Parameters.AddWithValue("@ConsecutiveNightsInfo", record.連泊情報);
                command.Parameters.AddWithValue("@RoomRateInfo", record.部屋料金情報);
                command.Parameters.AddWithValue("@MaleCount", record.男性人数);
                command.Parameters.AddWithValue("@FemaleCount", record.女性人数);
                command.Parameters.AddWithValue("@MemberAddress", record.会員住所);
                command.Parameters.AddWithValue("@MemberPhoneNumber", record.会員電話番号);
                command.Parameters.AddWithValue("@MemberFaxNumber", record.会員FAX番号);
                command.Parameters.AddWithValue("@MemberEmailAddress", record.会員メールアドレス);
                command.Parameters.AddWithValue("@MemberGender", record.会員性別);
                command.Parameters.AddWithValue("@MemberAge", record.会員年齢);
                command.Parameters.AddWithValue("@MemberCompanyName", record.会員会社名);
                command.Parameters.AddWithValue("@MemberCompanyPostalCode", record.会員会社郵便番号);
                command.Parameters.AddWithValue("@MemberCompanyAddress", record.会員会社住所);
                command.Parameters.AddWithValue("@MemberCompanyPhoneNumber", record.会員会社電話番号);
                command.Parameters.AddWithValue("@MemberCompanyFaxNumber", record.会員会社FAX番号);
                command.Parameters.AddWithValue("@MealConditions", record.食事条件);
                command.Parameters.AddWithValue("@Detail", record.詳細);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateRecordPreservingPdf(dynamic record, SqlConnection connection, object existingPdfFile, object existingPdfDate)
        {
            var commandText = @"
                            UPDATE Booking_Data
                            SET 予約サイト名 = @Provider, 予約日時 = @ResDateTime, 予約区分 = @ResType, チェックイン日 = @CheckInDate, チェックアウト日 = @CheckOutDate, 宿泊者氏名 = @GuestName, 
                            部屋名称 = @RoomName, 部屋数 = @RoomCount, 大人人数 = @AdultCount, 子供人数 = @ChildCount, 合計料金 = @TotalFee, ポイント = @Points, 請求料金 = @Charge, 支払方法 = @PaymentMethod, 
                            泊数 = @Nights, 変更日時 = @ChangeDateTime, キャンセル日時 = @CancelDateTime, 宿泊者氏名カナ = @GuestNameKana, チェックイン時刻 = @CheckInTime, 会員氏名カナ = @MemberNameKana, 
                            郵便番号 = @PostalCode, 住所 = @Address, 電話番号 = @PhoneNumber, FAX番号 = @FaxNumber, メールアドレス = @EmailAddress, プラン名称 = @PlanName, 備考 = @Remark, 
                            部屋マスタ = @RoomMaster, 会員氏名 = @MemberName, メモ = @Memo, 施設名 = @FacilityName, 部屋記号 = @RoomSymbol, プラン記号 = @PlanSymbol, 連泊情報 = @ConsecutiveNightsInfo, 
                            部屋料金情報 = @RoomRateInfo, 男性人数 = @MaleCount, 女性人数 = @FemaleCount, 会員住所 = @MemberAddress, 会員電話番号 = @MemberPhoneNumber, 会員FAX番号 = @MemberFaxNumber, 
                            会員メールアドレス = @MemberEmailAddress, 会員性別 = @MemberGender, 会員年齢 = @MemberAge, 会員会社名 = @MemberCompanyName, 会員会社郵便番号 = @MemberCompanyPostalCode, 
                            会員会社住所 = @MemberCompanyAddress, 会員会社電話番号 = @MemberCompanyPhoneNumber, 会員会社FAX番号 = @MemberCompanyFaxNumber, 食事条件 = @MealConditions, 詳細 = @Detail, 
                            pdf_file = ISNULL(@PdfFile, pdf_file), pdf_date = ISNULL(@PdfDate, pdf_date)
                            WHERE 予約番号 = @ResNumber;"; 

            using (var command = new SqlCommand(commandText, connection))
            {
                command.Parameters.AddWithValue("@Provider", record.予約サイト名);
                command.Parameters.AddWithValue("@ResNumber", record.予約番号);
                command.Parameters.AddWithValue("@ResDateTime", record.予約日時);
                command.Parameters.AddWithValue("@ResType", record.予約区分);
                command.Parameters.AddWithValue("@CheckInDate", record.チェックイン日);
                command.Parameters.AddWithValue("@CheckOutDate", record.チェックアウト日);
                command.Parameters.AddWithValue("@GuestName", record.宿泊者氏名);
                command.Parameters.AddWithValue("@RoomCount", record.部屋数);
                command.Parameters.AddWithValue("@AdultCount", record.大人人数);
                command.Parameters.AddWithValue("@ChildCount", record.子供人数);
                command.Parameters.AddWithValue("@TotalFee", record.合計料金);
                command.Parameters.AddWithValue("@Points", record.ポイント);
                command.Parameters.AddWithValue("@Charge", record.請求料金);
                command.Parameters.AddWithValue("@PaymentMethod", record.支払方法);
                command.Parameters.AddWithValue("@Nights", record.泊数);
                command.Parameters.AddWithValue("@ChangeDateTime", record.変更日時);
                command.Parameters.AddWithValue("@CancelDateTime", record.キャンセル日時);
                command.Parameters.AddWithValue("@Address", record.住所);
                command.Parameters.AddWithValue("@GuestNameKana", record.宿泊者氏名カナ);
                command.Parameters.AddWithValue("@PhoneNumber", record.電話番号);
                command.Parameters.AddWithValue("@EmailAddress", record.メールアドレス);
                command.Parameters.AddWithValue("@RoomMaster", record.部屋マスタ);
                command.Parameters.AddWithValue("@RoomName", record.部屋名称);
                command.Parameters.AddWithValue("@CheckInTime", record.チェックイン時刻);
                command.Parameters.AddWithValue("@MemberNameKana", record.会員氏名カナ);
                command.Parameters.AddWithValue("@PostalCode", record.郵便番号);
                command.Parameters.AddWithValue("@FaxNumber", record.FAX番号);
                command.Parameters.AddWithValue("@PlanName", record.プラン名称);
                command.Parameters.AddWithValue("@Remark", record.備考);
                command.Parameters.AddWithValue("@MemberName", record.会員氏名);
                command.Parameters.AddWithValue("@Memo", record.メモ);
                command.Parameters.AddWithValue("@FacilityName", record.施設名);
                command.Parameters.AddWithValue("@RoomSymbol", record.部屋記号);
                command.Parameters.AddWithValue("@PlanSymbol", record.プラン記号);
                command.Parameters.AddWithValue("@ConsecutiveNightsInfo", record.連泊情報);
                command.Parameters.AddWithValue("@RoomRateInfo", record.部屋料金情報);
                command.Parameters.AddWithValue("@MaleCount", record.男性人数);
                command.Parameters.AddWithValue("@FemaleCount", record.女性人数);
                command.Parameters.AddWithValue("@MemberAddress", record.会員住所);
                command.Parameters.AddWithValue("@MemberPhoneNumber", record.会員電話番号);
                command.Parameters.AddWithValue("@MemberFaxNumber", record.会員FAX番号);
                command.Parameters.AddWithValue("@MemberEmailAddress", record.会員メールアドレス);
                command.Parameters.AddWithValue("@MemberGender", record.会員性別);
                command.Parameters.AddWithValue("@MemberAge", record.会員年齢);
                command.Parameters.AddWithValue("@MemberCompanyName", record.会員会社名);
                command.Parameters.AddWithValue("@MemberCompanyPostalCode", record.会員会社郵便番号);
                command.Parameters.AddWithValue("@MemberCompanyAddress", record.会員会社住所);
                command.Parameters.AddWithValue("@MemberCompanyPhoneNumber", record.会員会社電話番号);
                command.Parameters.AddWithValue("@MemberCompanyFaxNumber", record.会員会社FAX番号);
                command.Parameters.AddWithValue("@MealConditions", record.食事条件);
                command.Parameters.AddWithValue("@Detail", record.詳細);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                command.Parameters.Add("@PdfFile", SqlDbType.VarBinary).Value = (existingPdfFile != null) ? existingPdfFile : DBNull.Value;
                command.Parameters.AddWithValue("@PdfDate", (existingPdfDate != null) ? existingPdfDate : DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LogError(string action, string message, Exception ex)
        {
            await _logService.InsertLogAsync("ERROR", action, message, ex.ToString(), "IP not applicable", "CsvProcessor", "CSV processing");
        }
    }
}


