
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace JackFuller_CodeTest
{
    //Class used to pull data from PostCode.IO API
    class PostCodeIOAPI
    {
        //Splits postcodes into groups of 100 as the Bulk Postcode lookup only accepts 100 postcodes at a time
        public static List<string> GetPostCodesInGroupsOfOneHundred(SQLiteDataReader dataReader)
        {
            int numberOfPostCodes = 0;

            string allPostCodes = String.Empty;

            List<string> groupedPostCodeStrings = new List<string>();

            //Splits Postcodes into strings containing 100 entries as
            //The WebApi can only take 100 at a time.
            while (dataReader.Read())
            {
                if (numberOfPostCodes < 100)
                {
                    string postcode = dataReader.GetString(0);

                    if (numberOfPostCodes == 0)
                    {
                        allPostCodes = postcode;
                    }
                    else
                    {
                        allPostCodes = allPostCodes + $",{postcode}";
                    }

                    numberOfPostCodes++;
                }

                if (numberOfPostCodes == 100)
                {
                    groupedPostCodeStrings.Add(allPostCodes);
                    allPostCodes = String.Empty;
                    numberOfPostCodes = 0;
                }
            }

            dataReader.Close();
            return groupedPostCodeStrings;
        }


        public static List<PostcodeData> GetDataFromWebAPI(List<string> postCodes, string[] filters)
        {
            string api = "https://api.postcodes.io/postcodes";

            //Adds filters to the API string
            if (filters.Length != 0)
            {
                api = api + "?filter=";

                for (int i = 0; i < filters.Length; i++)
                {
                    if (i == 0)
                    {
                        api = api + filters[0];
                    }
                    else
                    {
                        api = api + "," + filters[i];
                    }
                }
            }

            Postcodeobject data = new Postcodeobject();
            List<PostcodeData> postcodeData = new List<PostcodeData>();

            for (int i = 0; i < postCodes.Count; i++)
            {
                WebRequest request = WebRequest.Create(api);
                request.ContentType = "application/json";

                request.Method = "POST";

                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    string json = "{\"postcodes\" : [" + postCodes[i] + "] } ";

                    writer.Write(json);
                    writer.Flush();
                    writer.Close();
                }
                WebResponse response = request.GetResponse();
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd();

                    data = JsonConvert.DeserializeObject<Postcodeobject>(result);

                    for (int j = 0; j < data.result.Length; j++)
                    {
                        if (data.result[j].result != null)
                        {
                            postcodeData.Add(data.result[j].result);
                        }
                    }
                }
            }

            return postcodeData;
        }
    }
}
