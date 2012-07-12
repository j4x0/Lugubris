using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lugubris
{
    public class ServerInfo
    {
        public long BytesIn { get; set; }
        public long BytesOut { get; set; }
        public long RequestsHandled { get; set; }

        public ServerInfo()
        {
            this.BytesIn = 0;
            this.BytesOut = 0;
            this.RequestsHandled = 0;
        }
    }
}
