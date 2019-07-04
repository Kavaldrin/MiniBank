using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Project_distributed_transactions
{
    public interface IBank
    {
        String getName();
        List<String> getTableList();
    }

}
