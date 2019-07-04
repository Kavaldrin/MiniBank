using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;


namespace Project_distributed_transactions
{
    class CentralBank : IBank
    {

        enum ClientTable
        {
            ID = 0,
            Name = 1,
            Surname = 2,
            UniqueID = 3,
            Amount = 4,
            DepartmentID = 5
        }

        enum DepartmentTable
        {
            ID = 0,
            Name = 1
        }

        public enum TransferRole
        {
            Sender = 0,
            Receiver = 1
        }

        private String name = "Central Bank";
        private Dictionary<String, BranchBank> branchBanks = new Dictionary<String, BranchBank>();


        public String getName()
        {
            return name;
        }
        public List<String> getTableList()
        {
            List<String> tableList = new List<String>();
            tableList.Add("Client");
            tableList.Add("Department");
            return tableList;
        }


        /************ Transactions ************/

        public void registerBranchBank(BranchBank newBranchBank)
        {

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {

                    using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                    {
                        sqlConnection.Open();

                        DatabaseManager.validateConnection(sqlConnection);
                        DatabaseManager.validateDatabase(sqlConnection, getTableList());

                        using (SqlCommand checkCommand = new SqlCommand("SELECT COUNT(*) FROM Department WHERE name = @name", sqlConnection))
                        {
                            checkCommand.Parameters.AddWithValue("@name", newBranchBank.getName());
                            if((int)checkCommand.ExecuteScalar() > 0)
                            {
                                branchBanks.Add(newBranchBank.getName(), newBranchBank); //because bank is already registered
                                throw new Exception(String.Format("Branch bank {0} already registered", newBranchBank.getName()));
                            }
                        }

                        String newGuid = Guid.NewGuid().ToString();
                        using (SqlCommand insertCommand = new SqlCommand("INSERT INTO Department VALUES(@guid, @name)", sqlConnection))
                        {
                            insertCommand.Parameters.AddWithValue("@name", newBranchBank.getName());
                            insertCommand.Parameters.AddWithValue("@guid", newGuid);

                            if (insertCommand.ExecuteNonQuery() != 1)
                            {
                                throw new Exception(String.Format("Cannot register new department {0} to database", newBranchBank.getName()));
                            }
                        }



                    }
                    scope.Complete();
                }
            }
            
            catch (SqlException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }



        private String getClientsBranchBank(String uniqueID)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                {
                    sqlConnection.Open();
                    DatabaseManager.validateConnection(sqlConnection);
                    DatabaseManager.validateDatabase(sqlConnection, getTableList());

                    String departmentID;
                    String departmentName;
                    using (SqlCommand getDepartmentIDCommand = new SqlCommand("SELECT DepartmentId FROM Client WHERE uniqueId = @uniqueId", sqlConnection))
                    {
                        getDepartmentIDCommand.Parameters.AddWithValue("@uniqueId", uniqueID);
                        departmentID = (String)getDepartmentIDCommand.ExecuteScalar();
                    }

                    using (SqlCommand getDepartmentNameCommand = new SqlCommand("SELECT Name FROM Department WHERE @id = id", sqlConnection))
                    {
                        getDepartmentNameCommand.Parameters.AddWithValue("@id", departmentID);
                        departmentName = (String)getDepartmentNameCommand.ExecuteScalar();
                    }
                    return departmentName;
                }
            }
            catch(SqlException)
            {
                Console.WriteLine("Invalid input -> cannot find department for provided Client's ID");
                return null;
            }
            catch(NullReferenceException)
            {
                Console.WriteLine("Cannot find branch bank for client with provided unique ID");
                return null;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }


        private String getDepartmentID(SqlConnection sqlConnection,String branchBankName)
        {

            using (SqlCommand getDepartamentIDCommand = new SqlCommand("SELECT id FROM Department WHERE @name = name", sqlConnection))
            {
                getDepartamentIDCommand.Parameters.AddWithValue("@name", branchBankName);
                String name = (String)getDepartamentIDCommand.ExecuteScalar().ToString();
                if(name == null)
                {
                    throw new Exception("Cannot find registered branch bank");
                }
                return name;
            }


        }


        public List<Client> findClient(String name, String surname)
        {

            List<Client> foundClients = new List<Client>();

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                {
                    sqlConnection.Open();

                    DatabaseManager.validateConnection(sqlConnection);
                    DatabaseManager.validateDatabase(sqlConnection, getTableList());

                    using (SqlCommand searchInCentralCommand = new SqlCommand(@"SELECT * FROM Client WHERE name LIKE @name AND surname LIKE @surname", sqlConnection))
                    {

                        searchInCentralCommand.Parameters.AddWithValue("@name", "%" + name + "%");
                        searchInCentralCommand.Parameters.AddWithValue("@surname", "%" + surname + "%");
                        SqlDataReader sqlDataReader = searchInCentralCommand.ExecuteReader();

                        while (sqlDataReader.Read())
                        {
                            String guidCentral = (String)sqlDataReader.GetValue((int)ClientTable.ID).ToString();
                            String trueName = (String)sqlDataReader.GetValue((int)ClientTable.Name);
                            String trueSurname = (String)sqlDataReader.GetValue((int)ClientTable.Surname);
                            String uniqueID = (String)sqlDataReader.GetValue((int)ClientTable.UniqueID);
                            double amount = (double)(sqlDataReader.GetValue((int)ClientTable.Amount));
                            String departmentID = (String)sqlDataReader.GetValue((int)ClientTable.DepartmentID);
                            String departmentName = getClientsBranchBank(uniqueID);

                            foundClients.Add(new Client(trueName, trueSurname, uniqueID, amount, departmentName, departmentID));
                            
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return foundClients;
        }

        public bool registerClient(Client client)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    String guid = Guid.NewGuid().ToString();
                    using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                    {
                        sqlConnection.Open();
                        DatabaseManager.validateConnection(sqlConnection);
                        DatabaseManager.validateDatabase(sqlConnection, getTableList());
                        String departmentID = getDepartmentID(sqlConnection, client.departmentName);


                        using (SqlCommand insertCommand = new SqlCommand("INSERT INTO Client VALUES(@guid, @name, @surname, @uniqueId, @amount, @departmentId)", 
                            sqlConnection))
                        {
                           
                            insertCommand.Parameters.AddWithValue("@guid", guid);
                            insertCommand.Parameters.AddWithValue("@name", client.name);
                            insertCommand.Parameters.AddWithValue("@surname", client.surname);
                            insertCommand.Parameters.AddWithValue("@uniqueId", client.uniqueID);
                            insertCommand.Parameters.AddWithValue("@amount", client.amount);
                            insertCommand.Parameters.AddWithValue("@departmentId", departmentID);

                            if (insertCommand.ExecuteNonQuery() != 1)
                            {
                                throw new Exception("Error in transaction to central bank");
                            }

                        }
                    }
                    branchBanks[client.departmentName].registerClient(client, guid);
                    scope.Complete();
                    return true;
                }

            }
            catch(SqlException)
            {
                Console.WriteLine("Client with unique ID already registered");
                return false;
            }

            catch(NullReferenceException)
            {
                Console.WriteLine("Invalid input data -> cannot find provided department");
                return false;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }


        public Client getClientInfo(String uniqueID)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                {
                    sqlConnection.Open();
                    DatabaseManager.validateConnection(sqlConnection);
                    DatabaseManager.validateDatabase(sqlConnection, getTableList());


                    using (SqlCommand getClientCommand = new SqlCommand("SELECT * FROM Client WHERE @uniqueId = uniqueID", sqlConnection))
                    {
                        getClientCommand.Parameters.AddWithValue("@uniqueId", uniqueID);
                        SqlDataReader sqlDataReader = getClientCommand.ExecuteReader();
                        if (!sqlDataReader.HasRows)
                        {
                            return null;
                        }
                        sqlDataReader.Read();
                        String guidCentral = (String)sqlDataReader.GetValue((int)ClientTable.ID).ToString();
                        String trueName = (String)sqlDataReader.GetValue((int)ClientTable.Name);
                        String trueSurname = (String)sqlDataReader.GetValue((int)ClientTable.Surname);
                        double amount = (double)(sqlDataReader.GetValue((int)ClientTable.Amount));
                        String departmentID = (String)sqlDataReader.GetValue((int)ClientTable.DepartmentID);
                        String departmentName = getClientsBranchBank(uniqueID);

                        return new Client(trueName, trueSurname, uniqueID, amount, departmentName, departmentID);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }


        public bool depositMoney(String uniqueID, double amount)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                    {
                        sqlConnection.Open();
                        DatabaseManager.validateConnection(sqlConnection);
                        DatabaseManager.validateDatabase(sqlConnection, getTableList());

                        using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount += @amount WHERE uniqueId = @uniqueId", sqlConnection))
                        {
                            updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                            updateBalanceCommand.Parameters.AddWithValue("@uniqueId", uniqueID);
                            if(updateBalanceCommand.ExecuteNonQuery() != 1)
                            {
                                throw new Exception("Error in updating client's balance, probably invalid id of client [Central Bank]");
                            }
                        }
                    }

                    String departmentName = getClientsBranchBank(uniqueID);
                    if (departmentName == null)
                    {
                        throw new Exception("Wrong input data -> cannot find client");
                    }
                    branchBanks[departmentName].depositMoney(uniqueID, amount);
                    scope.Complete();
                    return true;
                }
            }
            catch(NullReferenceException)
            {
                Console.WriteLine("Provided data is invalid -> cannot find client");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }


        public bool withdrawMoney(String uniqueID, double amount)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                    {
                        sqlConnection.Open();
                        DatabaseManager.validateConnection(sqlConnection);
                        DatabaseManager.validateDatabase(sqlConnection, getTableList());

                        double currentBalance;

                        using (SqlCommand getCurrentBalanceCommand = new SqlCommand("SELECT amount from CLIENT WHERE uniqueId = @uniqueId", sqlConnection))
                        {
                            getCurrentBalanceCommand.Parameters.AddWithValue("@uniqueId", uniqueID);
                            currentBalance = (double)getCurrentBalanceCommand.ExecuteScalar();
                        }

                        if(currentBalance - amount < 0)
                        {
                            throw new Exception("Not enough money [Central Bank]");
                        }
                        using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount -= @amount WHERE uniqueId = @uniqueId", sqlConnection))
                        {
                            updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                            updateBalanceCommand.Parameters.AddWithValue("@uniqueId", uniqueID);
                            if(updateBalanceCommand.ExecuteNonQuery() != 1)
                            {
                                throw new Exception("Problem with withdrowing money [Central Bank]");
                            }
                        }
                    }

                    String departmentName = getClientsBranchBank(uniqueID);
                    if (departmentName == null)
                    {
                        throw new Exception("Wrong input data -> cannot find client");
                    }
                    branchBanks[departmentName].withdrawMoney(uniqueID, amount);

                    scope.Complete();
                    return true;
                }
            }
            catch(NullReferenceException)
            {
                Console.WriteLine("Provided data is invalid -> cannot find client");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }


        public List<Transaction> getClientTransactions(String uniqueID)
        {
            List<Transaction> transactionHistory = new List<Transaction>();
            try
            {
                String departmentName = getClientsBranchBank(uniqueID);
                if(departmentName == null)
                {
                    throw new Exception("Wrong input data -> cannot find client");
                }
                transactionHistory = branchBanks[departmentName].getClientsTransactions(uniqueID);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return new List<Transaction>(); //returning empty
            }
            return transactionHistory;

        }

        public bool transferMoney(String senderID, String receiverID, double amount)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    String guid = Guid.NewGuid().ToString();
                    using (SqlConnection centralBankSqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
                    {
                        centralBankSqlConnection.Open();
                        double currentBalance;

                        using (SqlCommand getCurrentBalanceCommand = new SqlCommand("SELECT amount from CLIENT WHERE uniqueId = @senderId", centralBankSqlConnection))
                        {
                            getCurrentBalanceCommand.Parameters.AddWithValue("@senderId", senderID);
                            currentBalance = (double)getCurrentBalanceCommand.ExecuteScalar();
                        }

                        if (currentBalance - amount < 0)
                        {
                            throw new Exception("Not enough money [Central Bank]");
                        }

                        using (SqlCommand updateBalanceCommandSender = new SqlCommand("UPDATE Client SET Amount -= @amount WHERE uniqueId = @senderId", centralBankSqlConnection))
                        {
                            updateBalanceCommandSender.Parameters.AddWithValue("@amount", amount);
                            updateBalanceCommandSender.Parameters.AddWithValue("@senderId", senderID);
                            if(updateBalanceCommandSender.ExecuteNonQuery() != 1)
                            {
                                throw new Exception("Error with transfer: -> updating sender balance [Central Bank]");
                            }
                        }

                        using (SqlCommand updateBalanceCommandReceiver = new SqlCommand("UPDATE Client SET Amount += @amount WHERE uniqueId = @receiverId", centralBankSqlConnection))
                        {
                            updateBalanceCommandReceiver.Parameters.AddWithValue("@amount", amount);
                            updateBalanceCommandReceiver.Parameters.AddWithValue("@receiverId", receiverID);
                            if(updateBalanceCommandReceiver.ExecuteNonQuery() != 1)
                            {
                                throw new Exception("Error with transfer -> updating receiver balance [Central Bank]");
                            }
                        }
                    }

                    String departmentNameSender = getClientsBranchBank(senderID);
                    String departmentNameReceiver = getClientsBranchBank(receiverID);

                    branchBanks[departmentNameSender].transferMoney(senderID, receiverID, amount, guid, TransferRole.Sender);
                    branchBanks[departmentNameReceiver].transferMoney(receiverID, senderID, amount, guid, TransferRole.Receiver);

                    scope.Complete();
                    return true;
                }

            }
            catch(SqlException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            catch(NullReferenceException)
            {
                Console.WriteLine("Invalid input data -> cannot find client(s)");
                return false;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }

    } /* End of class */
} /*End of namespace */
