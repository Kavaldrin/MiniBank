using System;
using System.Collections.Generic;
using System.Data.SqlClient;


namespace Project_distributed_transactions
{

    class DatabaseManager
    {
        static private String connectionTemplate = "Data Source = {0};Integrated security = SSPI;MultipleActiveResultSets=True;database = {1}";
        static private Dictionary<String, String> savedConfigurations = new Dictionary<String, String>();


        static public bool setUp(IBank bank, bool withClean, String server)
        {
            String databaseName = bank.getName().Replace(" ", String.Empty);

            try
            {
                String connectionString = String.Format(connectionTemplate, server, databaseName);
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {

                    sqlConnection.Open();
                    validateConnection(sqlConnection);

                    validateDatabase(sqlConnection, bank.getTableList());

                    if (withClean)
                    {
                        clearDatabase(sqlConnection, bank.getTableList());
                    }
                    savedConfigurations.Add(bank.getName(), connectionString);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in database connection: Closing program");
                Console.ReadKey();
                System.Environment.Exit(1);
            }
            return true;
        }


        static public void validateDatabase(SqlConnection sqlConnection, List<String> validationList)
        {
            foreach(var table in validationList)
            {
                validateTable(sqlConnection, table);
            }
        }

        static private void clearDatabase(SqlConnection sqlConnection, List<String> tableList)
        {


            foreach (var table in tableList)
            {
                using (SqlCommand clearCommand = new SqlCommand(String.Format("DELETE FROM {0} WHERE 1=1", table), sqlConnection))
                {
                    clearCommand.ExecuteNonQuery();
                }
            }
        }


        static public void validateConnection(SqlConnection sqlConnection)
        {
            if(sqlConnection.State != System.Data.ConnectionState.Open)
            {
                throw new Exception("Cannot establish connection with database");
            }
        }

        static public void validateTable(SqlConnection sqlConnection, String tableName)
        {
            using (SqlCommand validationCommand = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE table_name = @tableName", sqlConnection))
            {
                validationCommand.Parameters.AddWithValue("@tableName", tableName);
                if((int)validationCommand.ExecuteScalar() != 1)
                {
                    throw new Exception("Cannot find required table in database");
                }
            }

        }

        static public String getConnectionString(IBank bank)
        {
            return savedConfigurations[bank.getName()];
        }

        static public String getConnectionString(String name)
        {
            return savedConfigurations[name];
        }
    }
}
