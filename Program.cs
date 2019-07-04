using System;
using System.Collections.Generic;


namespace Project_distributed_transactions
{
    enum MainLoopStatus
    {
        Run,
        End
    }

    class Program
    {



        public static CentralBank centralBank = new CentralBank();
        public static List<BranchBank> branchBanks = new List<BranchBank>();
        public static BranchBank wroclawDepartment = new BranchBank("WroclawDepartment", centralBank);
        public static BranchBank krakowDepartment = new BranchBank("KrakowDepartment", centralBank);

        private static Dictionary<int, Action> menus;


        static void Main(string[] args)
        {
            /* Startup */

            Console.WriteLine("------- BANK APPLICATON ---------");
            Console.WriteLine("Choose run configuration");
            Console.WriteLine("Option [AnyKey] -> Run as usual");
            Console.WriteLine("Option [c] -> Init | Clear tables before start");

            bool withClean = false;
            if(Console.ReadKey().Key == ConsoleKey.C)
            {
                withClean = true;
            }

            Console.WriteLine("Setup info:");


            DatabaseManager.setUp(centralBank, withClean, @".\SQLExpress");
            DatabaseManager.setUp(wroclawDepartment, withClean, @".\SQLExpress");
            DatabaseManager.setUp(krakowDepartment, withClean, @".\SQLExpress");


            centralBank.registerBranchBank(wroclawDepartment);
            centralBank.registerBranchBank(krakowDepartment);
            branchBanks.Add(krakowDepartment);
            branchBanks.Add(wroclawDepartment);

            Console.WriteLine("Application successfully started, please hit any hit");
            Console.ReadKey();


            menus = new Dictionary<int, Action>()
            {
                {0, exitProgram},
                {1, showRegisterClient},
                {2, showFindClient},
                {3, showClientInfo},
                {4, showWithdrawMoney},
                {5, showDepositMoney},
                {6, showTransferMoney},
                {7, showTransactionHistory}
            };


            /* End of startup */


            while (displayMainMenu() == MainLoopStatus.Run) { } //forever loop
        }



        static private MainLoopStatus displayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("------- BANK APPLICATON ---------");
            Console.WriteLine("Option [1] -> Register Client");
            Console.WriteLine("Option [2] -> Find Client");
            Console.WriteLine("Option [3] -> Show info about Client");
            Console.WriteLine("Option [4] -> Withdraw money");
            Console.WriteLine("Option [5] -> Deposit money");
            Console.WriteLine("Option [6] -> Transfer money");
            Console.WriteLine("Option [7] -> Show history of transactions");
            Console.WriteLine("Option [0] -> Exit program");


            try
            {
                menus[(int)Console.ReadKey().Key - (int)ConsoleKey.D0].Invoke();
            }
            catch(Exception)
            {
                Console.WriteLine("Wrong input - try again");
                Console.WriteLine("Press any key to confirm");
                Console.ReadKey();
            }

            return MainLoopStatus.Run;

        }

        private static void exitProgram()
        {
            System.Environment.Exit(0);
        }



        private static void showRegisterClient()
        {
            Console.Clear();
            Console.WriteLine("------- Register user menu --------");
            Console.Write("Name: "); String name = Console.ReadLine();
            Console.Write("Surname: "); String surname = Console.ReadLine();
            Console.Write("Unique country ID: "); String uniqueID = Console.ReadLine();
            Console.Write("With deposit of _____ money: "); float amount = float.Parse(Console.ReadLine()); //maybe add later error handling
            Console.WriteLine("Avalible departments: ");
            foreach(var department in branchBanks)
            {
               Console.WriteLine("Department Name: " + department.getName());
            }
            Console.Write("Department: "); String departmentName = Console.ReadLine();

            if(centralBank.registerClient(new Client(name, surname, uniqueID, amount, departmentName,"")))
            {
                Console.WriteLine("Client has been registered");
            }
            else
            {
                Console.WriteLine("Aborted transaction! Try again");
            }


            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        private static void showFindClient()
        {
            Console.Clear();
            Console.WriteLine("------- Find client menu -------");
            Console.Write("Name: "); String name = Console.ReadLine();
            Console.Write("Surname: "); String surname = Console.ReadLine();
            List<Client> clients = centralBank.findClient(name, surname);

            if(clients.Count != 0)
            {
                Console.WriteLine("Search results: ");
            }
            else
            {
                Console.WriteLine("Cannot find any user with such name and surname");
            }
            foreach (var client in clients)
            {
                Console.WriteLine(client);
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        private static void showTransferMoney()
        {
            Console.Clear();
            Console.WriteLine("-------- Transfer menu ----------");
            Console.Write("Sender Unique ID: "); String senderID = Console.ReadLine();
            Console.Write("Receiver Unique ID: "); String receiverID = Console.ReadLine();
            Console.Write("Amount: "); double amount = double.Parse(Console.ReadLine());

            bool result = centralBank.transferMoney(senderID, receiverID, amount);
            if (result)
            {
                Console.WriteLine("Successfuly transfered money");
            }
            else
            {
                Console.WriteLine("Transaction aborted! Try again");
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

        }
        private static void showClientInfo()
        {
            Console.Clear();
            Console.Write("Client's Unique ID: "); String uniqueID = Console.ReadLine();

            Client client = centralBank.getClientInfo(uniqueID);

            if(client != null)
            {
                Console.WriteLine(client);
            }
            else
            {
                Console.WriteLine("There is not client with such a ID");
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
        private static void showTransactionHistory()
        {
            Console.Clear();

            Console.WriteLine("Client's Unique ID: "); String uniqueID = Console.ReadLine();
            List<Transaction> clientsTransactions = centralBank.getClientTransactions(uniqueID);
            if (clientsTransactions.Count != 0)
            {
                Console.WriteLine("Search result: ");
            }
            else
            {
                Console.WriteLine("Cannot find any transactions for provided unique ID");
            }
            foreach(var transaction in clientsTransactions)
            {
                Console.WriteLine(transaction);
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

        }
        private static void showWithdrawMoney()
        {
            Console.Clear();

            Console.WriteLine("------ Withdraw Money Menu -------");
            Console.Write("Client's unique ID: "); String uniqueID = Console.ReadLine();
            Console.Write("Amount of money: "); double amount = double.Parse(Console.ReadLine());

            bool result = centralBank.withdrawMoney(uniqueID, amount);
            if (result)
            {
                Console.WriteLine("Successfuly withdrew");
            }
            else
            {
                Console.WriteLine("Transaction aborted. Try again");
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

        }

        private static void showDepositMoney()
        {
            Console.Clear();

            Console.WriteLine("------ Deposit Money Menu -------");
            Console.Write("Client's unique ID: "); String uniqueID = Console.ReadLine();
            Console.Write("Amount of money: "); double amount = double.Parse(Console.ReadLine());

            bool result = centralBank.depositMoney(uniqueID, amount);
            if (result)
            {
                Console.WriteLine("Successfuly deposited");
            }
            else
            {
                Console.WriteLine("Transaction aborted. Try again");
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }

}

