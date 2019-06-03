using System;

namespace JackFuller_CodeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Creating Database & Tables");
            Database database = new Database();
            
            ReportHandler reportHandler = new ReportHandler();
            DataHandler dataHandler = new DataHandler(database,reportHandler);

            Console.WriteLine("Importing Data from CSV");
            dataHandler.OpenConnectionToDatabase();        
            CSVReader.ImportDataFromCSV(database);

            //Sorting Data
            Console.WriteLine("Finding Most Common Domain Name & Writing to Report");
            dataHandler.FindCommonEmailDomains();

            Console.WriteLine("Sorting People Via Geographical Location");
            dataHandler.GroupPeopleByGeography(10000);

            Console.WriteLine("Finding Number of Companies Per County");
            dataHandler.FindNumberOfCompaniesPerCounty();

            Console.WriteLine("Sorting Companies Via Parliamentry Constitency");
            dataHandler.GroupCompaniesByTheirCountyElectoralDistrict();

            Console.WriteLine($"Report Saved at {reportHandler.ReportPath}");
            reportHandler.SaveAndCloseReport();

            Console.WriteLine("");
            Console.WriteLine("Press Any Key To Exit");
            Console.ReadKey();            
        }
    }
}
