using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab0
{
    internal class dbItem
    {
        public string shopName;
        public string shopAddress;
        public string clientName;
        public string clientEmail;
        public string clientPhone;
        public string categotyName;
        public string distrName;
        public int itemId;
        public string itemName;
        public int amount;
        public decimal price;
        public DateTime purchDate;

        public dbItem() { }

        public override string ToString()
        {
            return shopName + " " + shopAddress + " " + clientName + " " + clientEmail + " " + clientPhone + " " + categotyName + " " + distrName + " " + itemId + " " + itemName + " " + amount + " " + price + " " + purchDate;
        }
    }
}
