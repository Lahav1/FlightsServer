using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace FlightsServer.Models
{
    class DatabaseHandler
    {
        /// <summary>
        /// The connection object to the servcer.
        /// </summary>
        private MySqlConnection connection;

        /// <summary>
        /// The server's ip.
        /// </summary>
        private string server;

        /// <summary>
        /// The connection port.
        /// </summary>
        private string port;

        /// <summary>
        /// The database's name.
        /// </summary>
        private string database;

        /// <summary>
        /// The user's username.
        /// </summary>
        private string uid;

        /// <summary>
        /// The user's password.
        /// </summary>
        private string password;


        /// <summary>
        /// The database handler constructor.
        /// </summary>
        /// <param name="path">A path to the configuration file.</param>
        public DatabaseHandler(string path = @"D:\config.json")
        {
            string config = DatabaseUtils.ReadFile(path);
            JObject parsedConfig = JObject.Parse(config);
            this.server = parsedConfig["Server"]["ip"].ToString();
            this.port = parsedConfig["Server"]["port"].ToString();
            this.database = parsedConfig["Server"]["database"].ToString();
            this.uid = parsedConfig["Server"]["User"]["username"].ToString();
            this.password = parsedConfig["Server"]["User"]["password"].ToString();
            string connectionString = $"SERVER={this.server};PORT={this.port};DATABASE={this.database};UID={this.uid};PASSWORD={this.password}";
            this.connection = new MySqlConnection(connectionString);
        }


        /// <summary>
        /// The function execute a query into the server.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A tuple of all the fields of the SQL table and the table's values.</returns>
        public Tuple<Dictionary<string, int>, List<List<string>>> ExecuteQuery(string query)
        {
            Dictionary<string, int> headers = new Dictionary<string, int>();

            //Open connection to the server.
            if (this.OpenConnection())
            {
                // Create a list to store the result
                List<List<string>> tableValues = new List<List<string>>();
                // Create Command
                MySqlCommand cmd = new MySqlCommand(query, connection);
                // Create a data reader and Execute the command
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    int count = reader.FieldCount;
                    for (int i = 0; i < count; i++)
                    {
                        headers[reader.GetName(i)] = i;
                    }

                    //Read the data and store them in the list
                    while (reader.Read())
                    {
                        List<string> temp = new List<string>();
                        for (int i = 0; i < count; i++)
                        {
                            temp.Add(reader.GetValue(i).ToString());
                        }
                        tableValues.Add(temp);
                    }
                }
                //close Connection to the server.
                this.CloseConnection();


                //return list to be displayed.
                return new Tuple<Dictionary<string, int>, List<List<string>>>(headers, tableValues);
            }
            else
            {
                return null;
            }

        }


        public void ExecuteNonQuery(List<string> queries)
        {
            if (this.OpenConnection())
            {
                MySqlCommand command = new MySqlCommand();
                MySqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction();

                // Must assign both transaction object and connection
                // to Command object for a pending local transaction
                command.Connection  = connection;
                command.Transaction = transaction;
                try
                {
                    foreach(string query in queries)
                    {
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch(Exception e)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch(SqlException ex)
                    {
                        if(transaction.Connection != null)
                        {
                            Console.WriteLine($"An exception of type {ex.GetType()} " +
                                $"was encountered while attempting to roll back the transaction.");
                        }
                    }
                    Console.WriteLine($"An exception of type {e.GetType()} " +
                       $"was encountered while inserting the data.");
                    Console.WriteLine("Neither record was written to database.");
                    throw e;
                }
                finally
                {
                    connection.Close();
                }
            }
        }


        /// <summary>
        /// The function opens a connection to the server.
        /// </summary>
        /// <returns>Return if the connection was successful.</returns>
        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Console.WriteLine("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Console.WriteLine("Invalid username/password, please try again");
                        break;
                }
                return false;
            }
        }

        /// <summary>
        /// The function closes the connection to the server.
        /// </summary>
        /// <returns>Returns if the closing of the connection to the server was successful.</returns>
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
