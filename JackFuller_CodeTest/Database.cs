using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using System;

namespace JackFuller_CodeTest
{
    //Handles the creation of the database, tables, writing and reading of data
    class Database
    {
        private SQLiteConnection m_dbConnection;
        private string m_dataBasePath;
        public string DataBasePath { get { return m_dataBasePath; } }
       
        public Database()
        {
            CreateDatabase();
        }

        private void CreateDatabase()
        {
            m_dataBasePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            m_dataBasePath = Path.Combine(m_dataBasePath, "UKDatabase");

            if (!Directory.Exists(m_dataBasePath))
            {
                Directory.CreateDirectory(m_dataBasePath);
            }
          
            SQLiteConnection.CreateFile($@"{m_dataBasePath}\UKDatabase.db");

            m_dbConnection = new SQLiteConnection($@"DATA SOURCE = {m_dataBasePath}\UKDatabase.db");
            m_dbConnection.Open();

            CreatePersonTable();
            CreateCompanyTable();
            CreateContactInformationTable();
        }

        private void CreatePersonTable()
        {
            string sql = $"CREATE TABLE IF NOT EXISTS people" +
                         $"(ID INTEGER PRIMARY KEY AUTOINCREMENT," +
                         $"first_name TEXT," +
                         $"last_name TEXT)";                         

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private void CreateCompanyTable()
        {
            string sql = $"CREATE TABLE IF NOT EXISTS company" +
                         $"(ID INTEGER PRIMARY KEY AUTOINCREMENT," +                        
                         $"company TEXT," +
                         $"companyWebsite TEXT)";

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private void CreateContactInformationTable()
        {
            string sql = $"CREATE TABLE IF NOT EXISTS contactInfo" +
                         $"(ID INTEGER PRIMARY KEY AUTOINCREMENT," +
                         $"address TEXT," +
                         $"city TEXT," +
                         $"county TEXT," +
                         $"postal TEXT," +
                         $"phone1 TEXT," +
                         $"phone2 TEXT," +
                         $"email TEXT)";

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);

            command.ExecuteNonQuery();

            sql = "CREATE INDEX postal_index ON contactInfo(postal)";
            command.ExecuteNonQuery();            
        }

        public SQLiteDataReader GetDataFromTable(string column, string tableName)
        {
            SQLiteCommand sqlCommand = new SQLiteCommand();

            sqlCommand.Connection = m_dbConnection;
            sqlCommand.CommandType = System.Data.CommandType.Text;
            sqlCommand.CommandText = $"Select {column} FROM {tableName}";

            SQLiteDataReader dataReader = sqlCommand.ExecuteReader();
            return dataReader;
        }

        public SQLiteDataReader GetDataFromTable(string column, string tableName, string whereCondition, string parameter, string parameterValue, bool parameterIsNumeric)
        {
            SQLiteCommand sqlCommand = new SQLiteCommand();

            sqlCommand.Connection = m_dbConnection;
            sqlCommand.CommandType = System.Data.CommandType.Text;            

            sqlCommand.CommandText = $"Select {column} FROM {tableName} WHERE {whereCondition}";

            if(!parameterIsNumeric)
                sqlCommand.Parameters.AddWithValue($"@{parameter}", $"\"{parameterValue}\"");
            else
            {
                int numericParameter = Int32.Parse(parameterValue); 
                sqlCommand.Parameters.AddWithValue($"@{parameter}", $"{parameterValue}");
            }

            SQLiteDataReader dataReader = sqlCommand.ExecuteReader();
            return dataReader;
        }

        public void WriteDataToTables(List<Person> people, List<Company> company, List<ContactInformation> contactInformation)
        {
            //Originally started with a for loop
            using (SQLiteCommand command = new SQLiteCommand(m_dbConnection))
            {
                using (SQLiteTransaction transaction = m_dbConnection.BeginTransaction())
                {
                    for (int i = 0; i < company.Count; i++)
                    {
                        command.CommandText = $"INSERT INTO people (first_name, last_name) VALUES " +
                                              $"('{people[i].FirstName}','" +                                            
                                              $"{people[i].LastName}')";

                        command.ExecuteNonQuery();

                        command.CommandText = $"INSERT INTO company (company, companyWebsite) VALUES " +
                                              $"('{company[i].CompanyName}','" +                                             
                                              $"{company[i].Website}')";

                        command.ExecuteNonQuery();

                        

                        command.CommandText = $"INSERT INTO contactInfo (address, city, county, postal, phone1, phone2, email) VALUES" +
                                              $" ('{contactInformation[i].Address}'," +
                                              $"'{contactInformation[i].City}'," +
                                              $"'{contactInformation[i].County}'," +
                                              $"'{contactInformation[i].Postal}'," +
                                              $"'{contactInformation[i].Phone1}'," +
                                              $"'{contactInformation[i].Phone2}'," +
                                              $"'{contactInformation[i].Email}')";

                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }
    }
}

public struct Person
{
    public string FirstName;
    public string LastName;
}

public struct Company
{
    public string CompanyName;
    public string Website;
}

public struct ContactInformation
{
    public string Address;
    public string City;
    public string County;
    public string Postal;

    public string Phone1;
    public string Phone2;
    public string Email;
}

