using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yeelight
{
    public class BulbManager
    {
        private static object locker = new object();

        public List<Bulb> bulbs { get; private set; } = new List<Bulb>();

        private static BulbManager _instance;
        public static BulbManager instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BulbManager();
                return _instance;
            }
        }

        public void addBulb(Bulb bulb)
        {
            if (bulbs.Where(p => p.id == bulb.id).FirstOrDefault() == null)
                lock (locker)
                {
                    bulb.initConnection();
                    bulbs.Add(bulb);
                }
        }
    }
}
