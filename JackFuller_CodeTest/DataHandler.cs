
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;

namespace JackFuller_CodeTest
{
    //The class used for sorting data from the database and pushing data to report handler
    class DataHandler
    {
        private Database m_database;
        private SQLiteConnection m_dbConnection;
        private ReportHandler m_reportHandler;
       
        public DataHandler(Database database, ReportHandler reportHandler)
        {
            m_database = database;
            m_reportHandler = reportHandler;
        }

        public void OpenConnectionToDatabase()
        {
            m_dbConnection = new SQLiteConnection($@"DATA SOURCE = {m_database.DataBasePath}\UKDatabase.db");
            m_dbConnection.Open();
        }

        #region Frequency Sorting Methods       

        public void FindCommonEmailDomains()
        {
            SQLiteDataReader dataReader = m_database.GetDataFromTable("email", "contactInfo");

            List<string> emailDomains = new List<string>();

            while (dataReader.Read())
            {
                string emailDomain = dataReader.GetString(0);
                emailDomain = emailDomain.Substring(emailDomain.IndexOf("@") + 1);
                emailDomain = emailDomain.Substring(0, emailDomain.Length - 1);

                emailDomains.Add(emailDomain);
            }

            Dictionary<string, int> emailDomainFrequencies = GetFrequencies(emailDomains);
            List<DataFrequency> data = SortFrequenciesByDescending(emailDomainFrequencies);

            string[] columnNames = new string[] { "Email Domain", "Count" };
            string targetWorkSheet = "Common Email Domains";

            //Create WorkSheet
            m_reportHandler.SetColumnNames(targetWorkSheet, columnNames);
            WriteFrequencyDataToReport(targetWorkSheet, data);
        }

        //Choice of Interesting Data 1) 
        //Was curious to see how London/South-east centric the companies were
        public void FindNumberOfCompaniesPerCounty()
        {
            List<string> counties = new List<string>();

            SQLiteDataReader dataReader = m_database.GetDataFromTable("county", "contactInfo");         

            while (dataReader.Read())
            {
                string county = RemoveQuotesFromString(dataReader.GetString(0));
                counties.Add(county);
            }

            Dictionary<string, int> companyByCounty = GetFrequencies(counties);
            List<DataFrequency> data = SortFrequenciesByDescending(companyByCounty);

            string targetWorkSheet = "Companies Per County";
            string[] columnNames = new string[] { "County", "Number of Companies" };

            m_reportHandler.CreateNewWorkSheet(targetWorkSheet, columnNames);

            WriteFrequencyDataToReport(targetWorkSheet, data);
        }

        private Dictionary<string, int> GetFrequencies(List<string> list)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (string entry in list)
            {
                if (result.TryGetValue(entry, out int count))
                {
                    result[entry] = count + 1;
                }
                else
                {
                    result.Add(entry, 1);
                }
            }
            return result;
        }

        private List<DataFrequency> SortFrequenciesByDescending(Dictionary<string, int> dict)
        {
            List<DataFrequency> data = new List<DataFrequency>();

            foreach (KeyValuePair<string, int> entry in dict)
            {
                DataFrequency dataPoint = new DataFrequency(entry.Key, entry.Value);
                data.Add(dataPoint);
            }

            data = data.OrderByDescending(location => location.frequency).ToList();
            return data;
        }

        private void WriteFrequencyDataToReport(string targetWorkSheet, List<DataFrequency> data)
        {
            //Write Data
            for (int i = 0; i < data.Count; i++)
            {
                m_reportHandler.WriteToCell(i + 1, 0, data[i].data, targetWorkSheet);
                m_reportHandler.WriteToCell(i + 1, 1, data[i].frequency.ToString(), targetWorkSheet);
            }
        }      

        #endregion

        #region Grouping Methods

        public void GroupPeopleByGeography(int gridSquareSize)
        {
            List<PostcodeData> postcodeData = GetPostcodeLocationDataFromWebAPI();
            SortGeographicalGroups(postcodeData,gridSquareSize);
        }

        private List<PostcodeData> GetPostcodeLocationDataFromWebAPI()
        {
            SQLiteDataReader dataReader = m_database.GetDataFromTable("postal", "contactInfo");
            List<string> groupedPostCodes = PostCodeIOAPI.GetPostCodesInGroupsOfOneHundred(dataReader);

            string[] filters = new string[] { "postcode", "eastings", "northings" };

            List<PostcodeData> postcodeData = PostCodeIOAPI.GetDataFromWebAPI(groupedPostCodes, filters);

            return postcodeData;
        }

        private void SortGeographicalGroups(List<PostcodeData> postcodeData, int gridSquareSize)
        {
            //Get Grid Size X & Y
            postcodeData = postcodeData.OrderBy(grid => grid.eastings).ToList();
            int maxX = postcodeData[postcodeData.Count - 1].eastings;            

            postcodeData = postcodeData.OrderBy(grid => grid.northings).ToList();
            int maxY = postcodeData[postcodeData.Count - 1].northings;              

            //Break Grid into Columns and Rows Based on Grouping Distance
            int numOfColumns = (maxX / gridSquareSize) + 1;
            int numOfRows = (maxY  / gridSquareSize) + 1;

            List<PostcodeData>[,] gridCount = new List<PostcodeData>[numOfColumns, numOfRows];

            //Cycle through the postcodes and calculates in which grid square each postcode would be placed and adds them to a 2d array containing lists of postcodes in that square area
            for (int locationIndex = 0; locationIndex < postcodeData.Count; locationIndex++)
            {
                PostcodeData result = postcodeData[locationIndex];

                int column = result.eastings / gridSquareSize;
                int row = result.northings / gridSquareSize;

                List<PostcodeData> entries = gridCount[column,row];

                if (entries == null)
                {
                    entries = new List<PostcodeData>();
                }

                entries.Add(result);
                gridCount[column, row] = entries;
            }
           
            List<List<PostcodeData>> sortedPostCodes = new List<List<PostcodeData>>();
            
            //Adds groupings of postcodes to a new master list
            for (int x = 0; x < numOfColumns; x++)
            {
                for (int y = 0; y < numOfRows; y++)
                {
                    List<PostcodeData> ls = gridCount[x, y];

                    if (ls != null)
                    {
                        if (ls.Count > 1)
                        {
                            sortedPostCodes.Add(ls);
                        }
                    }
                }
            }

            //Sort the master list by number of postcodes within that area
            sortedPostCodes = sortedPostCodes.OrderByDescending(ls => ls.Count).ToList();

            //Grabs the id associated with the postcode from contact info table so it can then be cross referenced with the ID from the people table
            //Assumption being made here is that postcodes are unique
            List<List<int>> postCodeIDs = new List<List<int>>();
            for (int i = 0; i < sortedPostCodes.Count; i++)
            {
                postCodeIDs.Add(new List<int>());

                for (int j = 0; j < sortedPostCodes[i].Count; j++)
                {
                    SQLiteDataReader reader = m_database.GetDataFromTable("ID", "contactInfo", "postal = @postcode", "postcode", sortedPostCodes[i][j].postcode, false);

                    while (reader.Read())
                    {
                        long ID = (long)reader["ID"];
                        postCodeIDs[i].Add((int)ID);
                    }
                }
            }

            //Create worksheet
            string[] columnNames = new string[] { "First Name", "Last Name", "Post Code" };
            m_reportHandler.CreateNewWorkSheet("Geographical Groupings",columnNames);

            int excelCellRow = 0;

            //Grabs the details from the people table to put into the report alongside the associated postcodes
            for (int i = 0; i < postCodeIDs.Count; i++)
            {
                for (int j = 0; j < postCodeIDs[i].Count; j++)
                {
                    SQLiteCommand cmd = new SQLiteCommand();

                    cmd.Connection = m_dbConnection;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "Select first_name, last_name FROM people WHERE ID = @ID";
                    cmd.Parameters.AddWithValue("@ID", postCodeIDs[i][j]);

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string firstName = RemoveQuotesFromString(reader["first_name"].ToString());
                            string secondName = RemoveQuotesFromString(reader["last_name"].ToString());
                            string postCode = sortedPostCodes[i][j].postcode;

                            m_reportHandler.WriteToCell(excelCellRow + 1, 0, firstName, "Geographical Groupings");
                            m_reportHandler.WriteToCell(excelCellRow + 1, 1, secondName, "Geographical Groupings");
                            m_reportHandler.WriteToCell(excelCellRow + 1, 2, postCode, "Geographical Groupings");
                        }
                    }

                    excelCellRow++;
                }
                excelCellRow++;
            }
        }

        //Choice of Interesting Data 2) 
        //Interested to see how companies broke down via Electorial district as data like this could be then used to inform political decisions
        public void GroupCompaniesByTheirCountyElectoralDistrict()
        {
            SQLiteDataReader dataReader = m_database.GetDataFromTable("postal", "contactInfo");
            List<string> groupedPostCodes = PostCodeIOAPI.GetPostCodesInGroupsOfOneHundred(dataReader);

            string[] filters = new string[] {"postcode","parliamentary_constituency" };

            List<PostcodeData> locations = PostCodeIOAPI.GetDataFromWebAPI(groupedPostCodes, filters); 
            locations = locations.OrderBy(location => location.parliamentary_constituency).ToList();

            List<int> IDs = new List<int>();
            SQLiteCommand sqlCommand = new SQLiteCommand();

            for (int i = 0; i < locations.Count; i++)
            {
                SQLiteDataReader reader = m_database.GetDataFromTable("ID", "contactInfo", "postal = @postcode", "postcode", locations[i].postcode,false);
                                
                while (reader.Read())
                {
                    long ID = (long)reader["ID"];                  
                    IDs.Add((int)ID);
                }                
            }

            sqlCommand.Cancel();

            List<string> companies = new List<string>();

            for (int i = 0; i < IDs.Count; i++)
            {
                SQLiteDataReader reader = m_database.GetDataFromTable("company", "company", "ID = @ID", "ID", IDs[i].ToString(),true);
                
                while (reader.Read())
                {
                    string companyName = RemoveQuotesFromString(reader["company"].ToString());
                    companies.Add(companyName);
                }
            }

            string[] columnNames = new string[] { "Company", "Constituency" };
            m_reportHandler.CreateNewWorkSheet("Constituency Companies", columnNames);
            

            for (int i = 0; i < companies.Count; i++)
            {
                m_reportHandler.WriteToCell(i + 1, 0, companies[i], "Constituency Companies");
                m_reportHandler.WriteToCell(i + 1, 1, locations[i].parliamentary_constituency, "Constituency Companies");
            }
        }

        #endregion

        //Used to remove quotes from strings when passing data into the report
        private string RemoveQuotesFromString(string trimmedString)
        {
            return trimmedString = trimmedString.Trim('"');
        }
    }
}

public struct DataFrequency
{
    public string data;
    public int frequency;

    public DataFrequency(string name, int freq)
    {
        data = name;
        frequency = freq;
    }
}
