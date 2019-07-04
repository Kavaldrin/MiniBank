using System;


namespace Project_distributed_transactions
{
    class Transaction
    {
        private String id;
        private String transactionType;
        private DateTime dateTime;
        private String senderUniqueID;
        private String receiverUniqueID;
        private double amount;


        public Transaction(String id, String transactionType, DateTime dateTime, String senderUniqueID, String receiverUniqueID, double amount)
        {
            this.id = id;
            this.transactionType = transactionType;
            this.dateTime = dateTime;
            this.senderUniqueID = senderUniqueID;
            this.receiverUniqueID = receiverUniqueID;
            this.amount = amount;
        }


        override public String ToString()
        {
            return "Transaction ID: " + id + " Transaction type: " + transactionType
                + "\nDate: " + dateTime.ToString() 
                + "\nSender ID: " + senderUniqueID + " Receiver ID: " + receiverUniqueID
                + "\nAmount: " + amount;
        }
    }
}
