using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Data.SqlClient;


namespace Project_distributed_transactions
{
    class BranchBank : IBank
    {

        enum ClientTable
        {
            ID = 0,
            Name = 1,
            Surname = 2,
            Amount = 3,
            idCentral = 4
        }

        enum DistributedTransactionTable
        {
            ID = 0,
            TransactionType = 1,
            Date = 2,
            SenderID = 3,
            ReceiverID = 4,
            Amount = 5
        }


        private String name;
        private CentralBank centralBank;

        public BranchBank(String name, CentralBank centralBank)
        {
            this.name = name;
            this.centralBank = centralBank;
        }


        public String getName()
        {
            return name;
        }

        public List<String> getTableList()
        {
            List<String> tableList = new List<String>();
            tableList.Add("Client");
            tableList.Add("DistributedTransaction");
            return tableList;
        }


        /***************** Transaction ***************/


        public void registerClient(Client newClient, String centralId)
        {
            using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(newClient.departmentName)))
            {
                sqlConnection.Open();
                DatabaseManager.validateConnection(sqlConnection);
                DatabaseManager.validateDatabase(sqlConnection, getTableList());

                using (SqlCommand insertCommand = new SqlCommand("INSERT INTO Client VALUES(@id, @name, @surname, @amount, @centralId)",sqlConnection))
                {
                    insertCommand.Parameters.AddWithValue("@id", newClient.uniqueID);
                    insertCommand.Parameters.AddWithValue("@name", newClient.name);
                    insertCommand.Parameters.AddWithValue("@surname", newClient.surname);
                    insertCommand.Parameters.AddWithValue("@amount", newClient.amount);
                    insertCommand.Parameters.AddWithValue("@centralId", centralId);
                    
                    if(insertCommand.ExecuteNonQuery() != 1)
                    {
                        throw new Exception("Cannot register client in branch bank...");
                    }
                }
            }
        }

        public void depositMoney(String uniqueID, double amount)
        {
            using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
            {
                sqlConnection.Open();
                DatabaseManager.validateConnection(sqlConnection);
                DatabaseManager.validateDatabase(sqlConnection, getTableList());

                using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount += @amount WHERE id = @id", sqlConnection))
                {
                    updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                    updateBalanceCommand.Parameters.AddWithValue("@id", uniqueID);
                    if (updateBalanceCommand.ExecuteNonQuery() != 1)
                    {
                        throw new Exception("Error in updating client's balance, probably invalid id of client [Branch bank]");

                    }
                }

                using (SqlCommand addTransactionCommand = new SqlCommand("INSERT INTO DistributedTransaction VALUES(@id, @type, @date, @senderId, @receiverId, @amount)",
                    sqlConnection))
                {
                    String guid = Guid.NewGuid().ToString();
                    addTransactionCommand.Parameters.AddWithValue("@id", guid);
                    addTransactionCommand.Parameters.AddWithValue("@type", "Deposit");
                    addTransactionCommand.Parameters.AddWithValue("@date", DateTime.Now);
                    addTransactionCommand.Parameters.AddWithValue("@senderId", uniqueID);
                    addTransactionCommand.Parameters.AddWithValue("@receiverId", uniqueID);
                    addTransactionCommand.Parameters.AddWithValue("@amount", amount);

                    if (addTransactionCommand.ExecuteNonQuery() != 1)
                    {
                        throw new Exception("Error with creating transaction in branch bank");
                    }
                }
            }
        }

        public void withdrawMoney(String uniqueID, double amount)
        {
            using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
            {
                sqlConnection.Open();
                DatabaseManager.validateConnection(sqlConnection);
                DatabaseManager.validateDatabase(sqlConnection, getTableList());
                double currentBalance;

                using (SqlCommand getCurrentBalanceCommand = new SqlCommand("SELECT amount from CLIENT WHERE id = @id", sqlConnection))
                {
                    getCurrentBalanceCommand.Parameters.AddWithValue("@id", uniqueID);
                    currentBalance = (double)getCurrentBalanceCommand.ExecuteScalar();
                }

                if (currentBalance - amount < 0)
                {
                    throw new Exception("Not enough money [Branch Bank]");
                }

                using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount -= @amount WHERE id = @id", sqlConnection))
                {
                    updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                    updateBalanceCommand.Parameters.AddWithValue("@id", uniqueID);
                    if (updateBalanceCommand.ExecuteNonQuery() != 1)
                    {
                        throw new Exception("Error with updating balance in branch bank");
                    }
                }

                using (SqlCommand addTransactionCommand = new SqlCommand("INSERT INTO DistributedTransaction VALUES(@id, @type, @date, @senderId, @receiverId, @amount)",
                    sqlConnection))
                {
                    String guid = Guid.NewGuid().ToString();
                    addTransactionCommand.Parameters.AddWithValue("@id", guid);
                    addTransactionCommand.Parameters.AddWithValue("@type", "Withdraw");
                    addTransactionCommand.Parameters.AddWithValue("@date", DateTime.Now);
                    addTransactionCommand.Parameters.AddWithValue("@senderId", uniqueID);
                    addTransactionCommand.Parameters.AddWithValue("@receiverId", uniqueID);
                    addTransactionCommand.Parameters.AddWithValue("@amount", amount);

                    if (addTransactionCommand.ExecuteNonQuery() != 1)
                    {
                        throw new Exception("Error with creating transaction in branch bank");
                    }
                }

            }
        }

        public List<Transaction> getClientsTransactions(String uniqueID)
        {

            List<Transaction> transactionHistory = new List<Transaction>();

            using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
            {

                sqlConnection.Open();
                DatabaseManager.validateConnection(sqlConnection);
                DatabaseManager.validateDatabase(sqlConnection, getTableList());
                using (SqlCommand getTransactionsCommand = new SqlCommand("SELECT * FROM DistributedTransaction WHERE receiverID = @uniqueId OR senderId = @uniqueId", sqlConnection))
                {
                    getTransactionsCommand.Parameters.AddWithValue("@uniqueId", uniqueID);

                    SqlDataReader sqlDataReader = getTransactionsCommand.ExecuteReader();
                    while (sqlDataReader.Read())
                    {
                        String id = sqlDataReader.GetValue((int)DistributedTransactionTable.ID).ToString();
                        String transactionType = (String)sqlDataReader.GetValue((int)DistributedTransactionTable.TransactionType);
                        DateTime dateTime = (DateTime)sqlDataReader.GetValue((int)DistributedTransactionTable.Date);
                        String senderID = (String)sqlDataReader.GetValue((int)DistributedTransactionTable.SenderID);
                        String receiverID = (String)sqlDataReader.GetValue((int)DistributedTransactionTable.ReceiverID);
                        double amount = (double)sqlDataReader.GetValue((int)DistributedTransactionTable.Amount);

                        transactionHistory.Add(new Transaction(id, transactionType, dateTime, senderID, receiverID, amount));
                    }
                }
            }
            return transactionHistory;
        }


        public void transferMoney(String uniqueID, String otherPersonUniqueID, double amount, String guid, CentralBank.TransferRole role)
        {
            using (SqlConnection sqlConnection = new SqlConnection(DatabaseManager.getConnectionString(this)))
            {
                sqlConnection.Open();
                DatabaseManager.validateConnection(sqlConnection);
                DatabaseManager.validateDatabase(sqlConnection, getTableList());


                if(role == CentralBank.TransferRole.Sender)
                {
                    double currentBalance;
                    using (SqlCommand getCurrentBalanceCommand = new SqlCommand("SELECT amount from CLIENT WHERE id = @id", sqlConnection))
                    {
                        getCurrentBalanceCommand.Parameters.AddWithValue("@id", uniqueID);
                        currentBalance = (double)getCurrentBalanceCommand.ExecuteScalar();
                    }

                    if (currentBalance - amount < 0)
                    {
                        throw new Exception("Not enough money [Branch Bank]");
                    }

                    using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount -= @amount WHERE id = @senderId", sqlConnection))
                    {
                        updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                        updateBalanceCommand.Parameters.AddWithValue("@senderId", uniqueID);
                        if (updateBalanceCommand.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Error with transfer -> updating sender balance [Branch Bank]");
                        }
                    }
                }
                else // if role = receiver
                {
                    using (SqlCommand updateBalanceCommand = new SqlCommand("UPDATE Client SET Amount += @amount WHERE id = @receiverId", sqlConnection))
                    {
                        updateBalanceCommand.Parameters.AddWithValue("@amount", amount);
                        updateBalanceCommand.Parameters.AddWithValue("@receiverId", uniqueID);
                        if (updateBalanceCommand.ExecuteNonQuery() != 1)
                        {
                            throw new Exception("Error with transfer -> updating receiver balance [Branch Bank]");
                        }
                    }
                }


                //if for the same department for sender and receiver
                using (SqlCommand checkIfAlreadyExistsCommand = new SqlCommand("SELECT COUNT(*) FROM DistributedTransaction WHERE id = @guid", sqlConnection))
                {
                    checkIfAlreadyExistsCommand.Parameters.AddWithValue("@guid", guid);
                    if((int)checkIfAlreadyExistsCommand.ExecuteScalar() == 1)
                    {
                        return;
                    }
                }

                using (SqlCommand addTransactionCommand = new SqlCommand("INSERT INTO DistributedTransaction VALUES(@id, @type, @date, @senderId, @receiverId, @amount)",
                sqlConnection))
                {
                    addTransactionCommand.Parameters.AddWithValue("@id", guid);
                    addTransactionCommand.Parameters.AddWithValue("@type", "Transfer");
                    addTransactionCommand.Parameters.AddWithValue("@date", DateTime.Now);

                    if (role == CentralBank.TransferRole.Sender)
                    {
                        addTransactionCommand.Parameters.AddWithValue("@senderId", uniqueID);
                        addTransactionCommand.Parameters.AddWithValue("@receiverId", otherPersonUniqueID);
                    }
                    else
                    {
                        addTransactionCommand.Parameters.AddWithValue("@senderId", otherPersonUniqueID);
                        addTransactionCommand.Parameters.AddWithValue("@receiverId", uniqueID);
                    }

                    addTransactionCommand.Parameters.AddWithValue("@amount", amount);

                    if (addTransactionCommand.ExecuteNonQuery() == 0)
                    {
                        throw new Exception("Error with creating transaction in branch bank");
                    }
                }
            }
        }
    }


}
