using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace JackFuller_CodeTest
{
    //Handles the pulling of data from the CSV file
    class CSVReader
    {
        public static void ImportDataFromCSV(Database targetDatabase)
        {
            string csvPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            csvPath = Path.Combine(csvPath, @"CSV\uk-500.csv");

            StreamReader reader = new StreamReader(File.OpenRead($@"{csvPath}"));

            List<Person> people = new List<Person>();
            List<Company> companies = new List<Company>();
            List<ContactInformation> contactInformation = new List<ContactInformation>();

            bool isFirstLine = true;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                if (!String.IsNullOrWhiteSpace(line))
                {
                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    //Had to use Regex.Split due to commas in the company names
                    string[] values = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                    Person person = new Person()
                    {
                        FirstName = values[0],
                        LastName = values[1],
                    };

                    Company company = new Company()
                    {
                        CompanyName = values[2],
                        Website = values[10],
                    };

                    ContactInformation contactInfo = new ContactInformation()
                    {
                        Address = values[3],
                        City = values[4],
                        County = values[5],
                        Postal = values[6],
                        Phone1 = values[7],
                        Phone2 = values[8],
                        Email = values[9],
                    };

                    people.Add(person);
                    contactInformation.Add(contactInfo);
                    companies.Add(company);
                }
            }

            targetDatabase.WriteDataToTables(people,companies, contactInformation);
        }
    }
}
