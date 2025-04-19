using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using ServerSimulation.Models;


/// <summary>
/// DBServices is a class created by me to provides some DataBase Services
/// </summary>
public class DBservices
{


    public DBservices()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    //--------------------------------------------------------------------------------------------------
    // This method creates a connection to the database according to the connectionString name in the web.config 
    //--------------------------------------------------------------------------------------------------
    public SqlConnection connect(String conString)
    {

        // read the connection string from the configuration file
        IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json").Build();
        string cStr = configuration.GetConnectionString("myProjDB");
        SqlConnection con = new SqlConnection(cStr);
        con.Open();
        return con;
    }

    //--------------------------------------------------------------------------------------------------
    // Get all shelters from database
    //--------------------------------------------------------------------------------------------------

    public List<Shelter> GetAllShelters()
    {
        SqlConnection con = null;
        SqlCommand cmd = null;
        List<Shelter> sheltersList = new List<Shelter>();

        try
        {
            Console.WriteLine("Attempting to connect to database...");
            con = connect("myProjDB");
            Console.WriteLine("Database connection successful");

            Console.WriteLine("Creating command for FC_SP_getAllShelters...");
            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getAllShelters", con, null);

            Console.WriteLine("Executing stored procedure...");
            SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            int rowCount = 0;
            Console.WriteLine("Reading results...");

            while (dr.Read())
            {
                rowCount++;
                Shelter shelter = new Shelter();

                try
                {
                    shelter.shelter_id = Convert.ToInt32(dr["shelter_id"]);
                    shelter.provider_id = dr["provider_id"] != DBNull.Value ? (int?)dr["provider_id"] : null;
                    shelter.shelter_type = dr["shelter_type"] as string ?? "";
                    shelter.name = dr["name"] as string ?? "";
                    shelter.latitude = Convert.ToDouble(dr["latitude"]);
                    shelter.longitude = Convert.ToDouble(dr["longitude"]);
                    shelter.address = dr["address"] as string ?? "";
                    shelter.capacity = Convert.ToInt16(dr["capacity"]);
                    shelter.is_accessible = dr["is_accessible"] != DBNull.Value ? (bool?)dr["is_accessible"] : null;
                    shelter.is_active = dr["is_active"] != DBNull.Value ? (bool?)dr["is_active"] : null;
                    shelter.created_at = dr["created_at"] != DBNull.Value ? (DateTime?)dr["created_at"] : null;
                    shelter.last_updated = dr["last_updated"] != DBNull.Value ? (DateTime?)dr["last_updated"] : null;
                    shelter.additional_information = dr["additional_information"] as string ?? "";

                    sheltersList.Add(shelter);
                    Console.WriteLine($"Added shelter: ID={shelter.shelter_id}, Name={shelter.name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error mapping row {rowCount}: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine($"Finished reading data: {rowCount} rows found, {sheltersList.Count} shelters added to list");
            return sheltersList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllShelters: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            if (con != null && con.State == ConnectionState.Open)
            {
                Console.WriteLine("Closing database connection");
                con.Close();
            }
        }
    }

    //---------------------------------------------------------------------------------
    // Create the SqlCommand
    //---------------------------------------------------------------------------------
    private SqlCommand CreateCommandWithStoredProcedureGeneral(String spName, SqlConnection con, Dictionary<string, object> paramDic)
    {

        SqlCommand cmd = new SqlCommand(); // create the command object

        cmd.Connection = con;              // assign the connection to the command object

        cmd.CommandText = spName;      // can be Select, Insert, Update, Delete 

        cmd.CommandTimeout = 10;           // Time to wait for the execution' The default is 30 seconds

        cmd.CommandType = System.Data.CommandType.StoredProcedure; // the type of the command, can also be text

        if (paramDic != null)
            foreach (KeyValuePair<string, object> param in paramDic)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value);

            }


        return cmd;
    }

}
