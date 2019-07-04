using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_distributed_transactions
{
    class Client
    {

        public String name { private set; get; }
        public String surname { private set; get; }
        public String uniqueID { private set; get; }
        public double amount { private set; get; }
        public String departmentName { private set; get; }
        public String departmentID { set; get; }


        public Client(String name, String surname, String uniqueID, double amount, String departmentName,  String departmentID)
        {
            this.name = name;
            this.surname = surname;
            this.uniqueID = uniqueID;
            this.amount = amount;
            this.departmentName = departmentName;
            this.departmentID = departmentID;
        }


        override public string ToString()
        {
            return "Name: " + name + " Surname: " + surname 
                + "\nUnique country ID: " + uniqueID  + " Account balance: " + amount 
                + "\nDepartment Name: " + departmentName + " Department ID: " + departmentID;
        }
    }
}
