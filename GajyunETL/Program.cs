using Microsoft.Extensions.Configuration;
using System.Text;

namespace GajyunETL
{
    class Program
    {
        static async Task Main(string[] args) // Modificado para ser um método assíncrono
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Obrigatório para dados em japonês.

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string connectionString = configuration.GetConnectionString("DefaultConnection");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8604 // Possible null reference argument.
            LogService logService = new LogService(connectionString); // Criação da instância LogService
#pragma warning restore CS8604 // Possible null reference argument.
            CsvProcessor csvProcessor = new CsvProcessor(connectionString, logService); // Passa LogService como argumento
            await csvProcessor.ProcessCsvFiles(@"C:\Users\Owner\Downloads\CSVProcessing\2 - PreProcessed"); // Uso de await para chamada assíncrona
        }
    }
}



