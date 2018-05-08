using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CfSampleAppDotNetCore.Models
{
    public class KittenRepository : IKittenRepository
    {
        public string ConnectionString;
        public MySqlConnection Connection;
        public KittenRepository()
        {
            if (Environment.GetEnvironmentVariable("VCAP_SERVICES") != null)
            {
                var vcapServices = JsonConvert.DeserializeObject<VcapServices>(Environment.GetEnvironmentVariable("VCAP_SERVICES"));
                ConnectionString = "server="+vcapServices.mariadbent[0].credentials.host
                                                         +";user="+vcapServices.mariadbent[0].credentials.username
                                                         +";database="+vcapServices.mariadbent[0].credentials.database
                                                         +";port="+vcapServices.mariadbent[0].credentials.port
                                                         +";password="+vcapServices.mariadbent[0].credentials.password;

            }
            else
            {
                Console.WriteLine("Using the local Mariadb");
                ConnectionString = "server=localhost;user=root;database=mysql;port=3306;password=test";;
            }
        }


        public List<string> Find()
        {
            List<String> columnData = new List<String>();
            Connection = new MySqlConnection(ConnectionString);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                Connection.Open();
                string sql = "SELECT Name FROM Kittens;";
                MySqlCommand cmd = new MySqlCommand(sql, Connection);
                MySql.Data.MySqlClient.MySqlDataReader reader = cmd.ExecuteReader();
                if (reader != null)
                {
                    while (reader.Read())
                    {
                      columnData.Add(reader.GetString(0));
                     }
                    Connection.Close();
                    Console.WriteLine("Done.");
                    return columnData; 

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Connection.Close();
            Console.WriteLine("Done.");
            return columnData;
        }

        public Kitten Create(Kitten kitten)
        {
            Connection = new MySqlConnection(ConnectionString);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                Console.WriteLine(ConnectionString);
                Connection.Open();
                string sql = "INSERT INTO Kittens (Name) VALUES ('" + kitten.Name + "');";
                Console.WriteLine(sql);
                MySqlCommand cmd = new MySqlCommand(sql, Connection);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Connection.Close();
            Console.WriteLine("Done.");
            return kitten;
        }
    }
}

