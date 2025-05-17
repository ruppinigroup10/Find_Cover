using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using FC_Server.Models;
using System.Net.NetworkInformation;
using System.Net;
public class DBservicesShelter
{
        public DBservicesShelter ()
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
        // Create the SqlCommand
        //--------------------------------------------------------------------------------------------------
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

        //--------------------------------------------------------------------------------------------------
        // This method adds a shelter
        //--------------------------------------------------------------------------------------------------
        public Shelter? AddShelter(string shelter_type, string name, float latitude, float longitude,
                                            string address, int capacity,
                                            bool is_accessible, bool pets_friendly,
                                            string additional_information, int provider_id)
    {
            SqlConnection con;
            SqlCommand cmd;
            Shelter? shelter = null;

            try
            {
                con = connect("myProjDB");
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection error: " + ex.Message);
            }

            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@shelter_type", shelter_type);
            paramDic.Add("@name", name);
            paramDic.Add("@latitude", latitude);
            paramDic.Add("@longitude", longitude);
            paramDic.Add("@address", address);
            paramDic.Add("@capacity", capacity);
            paramDic.Add("is_accessible", is_accessible);
            paramDic.Add("pets_friendly", pets_friendly);
            paramDic.Add("@additional_information", additional_information);
            paramDic.Add("@provider_id", provider_id);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_AddShelter", con, paramDic);

            try
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        shelter = new Shelter
                        {
                            ShelterId = Convert.ToInt32(dr["shelter_id"]),
                            ShelterType = dr["shelter_type"].ToString() ?? "",
                            Name = dr["name"].ToString() ?? "",
                            Latitude = Convert.ToSingle(dr["latitude"]),
                            Longitude = Convert.ToSingle(dr["longitude"]),
                            Address = dr["address"].ToString() ?? "",
                            Capacity = Convert.ToInt32(dr["capacity"]),
                            AdditionalInformation = dr["additional_information"].ToString() ?? "",
                            ProviderId = Convert.ToInt32(dr["provider_id"]),
                            IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                            PetsFriendly = Convert.ToBoolean(dr["pets_friendly"]),
                            IsActive = Convert.ToBoolean(dr["is_active"])
                        };
                    }
                }
                return shelter;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("User added this shelter already"))
                {
                    throw new Exception("User added this shelter already");
                }
            if (ex.Message.Contains("User not exists"))
            {
                throw new Exception("User not exists");
            }
            throw new Exception("Addition failed");
            }
            finally
            {
                if (con != null && con.State == System.Data.ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------
        // This method updates a shelter
        //--------------------------------------------------------------------------------------------------
        public Shelter? UpdateShelter(int shelter_id, string shelter_type, string name, float latitude, float longitude,
                                string address, int capacity, string additional_information, int provider_id)
        {
            SqlConnection con;
            SqlCommand cmd;
            Shelter? shelter = null;

            try
            {
                con = connect("myProjDB");
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection error: " + ex.Message);
            }

            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@shelter_id", shelter_id);
            paramDic.Add("@shelter_type", shelter_type);
            paramDic.Add("@name", name);
            paramDic.Add("@latitude", latitude);
            paramDic.Add("@longitude", longitude);
            paramDic.Add("@address", address);
            paramDic.Add("@capacity", capacity);
            paramDic.Add("@additional_information", additional_information);
            paramDic.Add("@provider_id", provider_id);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_UpdateShelter", con, paramDic);

            try
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        shelter = new Shelter
                        {
                            ShelterId = Convert.ToInt32(dr["shelter_id"]),
                            ShelterType = dr["shelter_type"].ToString() ?? "",
                            Name = dr["name"].ToString() ?? "",
                            Latitude = Convert.ToSingle(dr["latitude"]),
                            Longitude = Convert.ToSingle(dr["longitude"]),
                            Address = dr["address"].ToString() ?? "",
                            Capacity = Convert.ToInt32(dr["capacity"]),
                            AdditionalInformation = dr["additional_information"].ToString() ?? "",
                            ProviderId = Convert.ToInt32(dr["provider_id"]),
                            IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                            IsActive = Convert.ToBoolean(dr["is_active"])
                        };
                    }
                }
                return shelter;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("User added this shelter already"))
                {
                    throw new Exception("User added this shelter already");
                }
                throw new Exception("Update failed");
            }
            finally
            {
                if (con != null && con.State == System.Data.ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        //--------------------------------------------------------------------------------------------------
        // This method geting shelters data
        //--------------------------------------------------------------------------------------------------
        public Shelter? getShelter(int shelter_id)
        {

            SqlConnection con;
            SqlCommand cmd;
            Shelter? shelter = null;

            try
            {
                con = connect("myProjDB"); // create the connection
            }
            catch (Exception)
            {
                // write to log
                throw;
            }

            Dictionary<string, object> paramDic = new Dictionary<string, object>();
            paramDic.Add("@provider_id", shelter_id);

            cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getShelter", con, paramDic);

            try
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        shelter = new Shelter
                        {
                            ShelterId = Convert.ToInt32(dr["shelter_id"]),
                            ShelterType = dr["shelter_type"].ToString() ?? "",
                            Name = dr["name"].ToString() ?? "",
                            Latitude = Convert.ToSingle(dr["latitude"]),
                            Longitude = Convert.ToSingle(dr["longitude"]),
                            Address = dr["address"].ToString() ?? "",
                            Capacity = Convert.ToInt32(dr["capacity"]),
                            AdditionalInformation = dr["additional_information"].ToString() ?? "",
                            ProviderId = Convert.ToInt32(dr["provider_id"]),
                            IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                            IsActive = Convert.ToBoolean(dr["is_active"])
                        };
                    }
                }
                return shelter;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid ID"))
                {
                    throw new Exception("Invalid ID");
                }
                throw new Exception("Shelter data transfer failed");
            }
            finally
            {
                if (con != null)
                {
                    // close the db connection
                    con.Close();
                }
            }
        }

    public Shelter? getMyShelter(int provider_id)
    {

        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB"); // create the connection
        }
        catch (Exception)
        {
            // write to log
            throw;
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@provider_id", provider_id);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_getMyShelter", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("Shelter data transfer failed");
        }
        finally
        {
            if (con != null)
            {
                // close the db connection
                con.Close();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------
    // This method geting shelters data
    //--------------------------------------------------------------------------------------------------
    public Shelter? shelterActiveStatus(int shelter_id, bool status)
    {

        SqlConnection con;
        SqlCommand cmd;
        Shelter? shelter = null;

        try
        {
            con = connect("myProjDB"); // create the connection
        }
        catch (Exception)
        {
            // write to log
            throw;
        }

        Dictionary<string, object> paramDic = new Dictionary<string, object>();
        paramDic.Add("@shelter_id", shelter_id);
        paramDic.Add("@status", status);

        cmd = CreateCommandWithStoredProcedureGeneral("FC_SP_shelterActiveStatus", con, paramDic);

        try
        {
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    shelter = new Shelter
                    {
                        ShelterId = Convert.ToInt32(dr["shelter_id"]),
                        ShelterType = dr["shelter_type"].ToString() ?? "",
                        Name = dr["name"].ToString() ?? "",
                        Latitude = Convert.ToSingle(dr["latitude"]),
                        Longitude = Convert.ToSingle(dr["longitude"]),
                        Address = dr["address"].ToString() ?? "",
                        Capacity = Convert.ToInt32(dr["capacity"]),
                        AdditionalInformation = dr["additional_information"].ToString() ?? "",
                        ProviderId = Convert.ToInt32(dr["provider_id"]),
                        IsAccessible = Convert.ToBoolean(dr["is_accessible"]),
                        IsActive = Convert.ToBoolean(dr["is_active"])
                    };
                }
            }
            return shelter;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Invalid ID"))
            {
                throw new Exception("Invalid ID");
            }
            throw new Exception("Shelter data transfer failed");
        }
        finally
        {
            if (con != null)
            {
                // close the db connection
                con.Close();
            }
        }
    }
}
