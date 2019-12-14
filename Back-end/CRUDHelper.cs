using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

public static class CRUDHelper
{
    // Used for running get queries and returning the results in a List<string>
    public static List<string> getQuery(string query)
    {
        try
        {
            List<string> return_arr = new List<string>();

            // Setup the connection to the Data base and preps the query string
            SqlConnection connectionString = new SqlConnection(System.Environment.GetEnvironmentVariable("Connection String"));
            SqlCommand querystring = new SqlCommand(query, connectionString);

            // Opens the connection to the database and runs the query getting data.
            connectionString.Open();
            SqlDataReader reader = querystring.ExecuteReader();
            while (reader.Read())
            {
                string temp_string = "";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    temp_string += reader[i].ToString();
                    temp_string += (i != reader.FieldCount - 1 ? "," : "");
                }
                return_arr.Add(temp_string);

            }

            //Close the connection to the database
            reader.Close();
            connectionString.Close();

            return return_arr;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new List<String> { null };
        }

    }
    // Deals with Queries that do not return results (POST)
    public static int executeNonQuery(string query)
    {
        try
        {
            // Setup the connection to the Data base and preps the query string
            SqlConnection connectionString = new SqlConnection(System.Environment.GetEnvironmentVariable("Connection String"));
            SqlCommand querystring = new SqlCommand(query, connectionString);
            connectionString.Open();
            int affectRows = querystring.ExecuteNonQuery();
            connectionString.Close();

            return affectRows;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 0;
        }
    }

    public static int executeScalar(string query)
    {
        try
        {
            // Setup the connection to the Data base and preps the query string
            SqlConnection connectionString = new SqlConnection(System.Environment.GetEnvironmentVariable("Connection String"));
            SqlCommand querystring = new SqlCommand(query, connectionString);
            connectionString.Open();
            int affectRows = (int) querystring.ExecuteScalar();
            connectionString.Close();
            return affectRows;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 0;
        }
    }


    public static string formatMember(string[] input_array)
    {
        string query = @"SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = 'members'";
        List<string> columnNames = getQuery(query);

        query = @"SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'medicalinfo'";
        columnNames.AddRange(getQuery(query));

        var dict = columnNames.Zip(input_array, (k, v) => new { Key = k, Value = v }).Distinct().ToDictionary(x => x.Key, x => x.Value);

        return JsonConvert.SerializeObject(dict);
    }

    public static string formatEvent(string[] input_array)
    {
        // Formatting the Date String
        DateTime date_time = DateTime.Parse(input_array[1]);
        string formattedDate = date_time.ToString("yyyy-MM-dd");

        // Creating a human readable URL
        string[] title_array = input_array[0].ToLower().Split(" ");
        string url = String.Join("-", title_array);
        input_array[2] = input_array[2].Replace("~~~", ",");

        // Getting Count for Events
        int eventID = MotleyMoves.fa_MotleyMoves.getEventID(url);
        string query = String.Format(@"SELECT COUNT(*) FROM modestomovesdb.eventattendees
                                    WHERE eventID = {0}", eventID);
        string count = CRUDHelper.getQuery(query)[0];

        string output = "\"title\": \"" + input_array[0] + "\", \"start\": \"" + formattedDate + "\", \"url\": \"map.html?name=" + url + "\", \"description\": \"" + input_array[2] + "\", \"numRegistered\":\"" + count + "\"";

        return output;
    }

}