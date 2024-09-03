
# GajyunETL

The **GajyunETL** project is a .NET application designed to process CSV and PDF files, performing data insertion and updates into a SQL Server database. This project was developed to address a specific issue faced by a guesthouse, where the daily printing of multiple passport copies was leading to excessive paper usage and storage challenges. The goal of this project is to eliminate the need for paper by digitizing the process of handling client passport information.

## Overview

- **Framework**: .NET 8.0
- **Purpose**: Process CSV and PDF files to manage client reservation and passport data electronically, reducing or eliminating paper use.
- **Key Features**:
  - CSV file processing to insert and update reservation data.
  - PDF file processing to store client passport documents linked to reservations.
  - Detailed logging of all operations for auditing and monitoring purposes.

## Project Structure

1. **`CsvProcessor.cs`**
   - Responsible for processing CSV files.
   - Reads files, validates data, and inserts or updates records in the database.
   - Integrates with the logging service to record all actions taken during processing.

2. **`PdfProcessor.cs`**
   - Handles the processing of PDF files.
   - Updates existing database records with the PDF content associated with the correct reservation number.
   - Logs all operations to ensure traceability.

3. **`LogService.cs`**
   - A service dedicated to logging events in a SQL database.
   - Stores detailed information such as severity level, action performed, log messages, and more.

4. **`Program.cs`**
   - The entry point of the application.
   - Loads configuration from the `appsettings.json` file, initializes the CSV and PDF processors, and executes processing asynchronously.

5. **`appsettings.json`**
   - Configuration file defining the database connection string and file paths for CSV and PDF files.

6. **`GajyunETL.csproj`**
   - Project configuration file specifying necessary dependencies, target framework, and other technical details.

7. **`Resources.Designer.cs` and `Resources.resx`**
   - Files responsible for managing localized resources (like strings) used in the project.

8. **`GajyunETL.sln`**
   - Visual Studio solution file that organizes and manages the `GajyunETL`  project.

## Setup and Execution

### Prerequisites

- **.NET 8.0 SDK** or higher
- **SQL Server** configured and accessible
- **Visual Studio 2022** (optional, but recommended for development)

### Setup

1. Clone the repository to your local machine:
   ```bash
   git clone https://github.com/your-username/gajyunetl.git
   ```

2. Navigate to the project directory and restore dependencies:
   ```bash
   cd gajyunetl
   dotnet restore
   ```

3. Configure the connection string and file paths in the `appsettings.json` file:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=GajyunDb;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "FilePaths": {
       "PdfFolderPath": "C:\Users\Owner\Downloads\CSVProcessing\Passaports", /*Example*/
       "CsvPath": "C:\Users\Owner\Downloads\CSVProcessing\2 - PreProcessed" /*Example*/
     }
   }
   ```

### Execution

To run the application, navigate to the project directory and use the following command:

```bash
dotnet run
```

## Dependencies

- **CsvHelper**: Used for easy reading and manipulation of CSV files.
- **Microsoft.Data.SqlClient**: Library for connecting to and interacting with SQL Server.
- **Microsoft.Extensions.Configuration**: For loading and managing application settings.

## Logging

Logs are stored in a `Logs` table in the configured SQL database. They include detailed information about the severity of the event, the action performed, messages, and other relevant details for auditing purposes.
