using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    public class HttpServerLiteLogEventArgs : EventArgs
    {

        public Exception? Exception { get; init; }
        public string Msg { get; init; }
        public HttpServerLiteLogEventArgs(string msg, Exception? ex = null)
        {
            Msg = msg;
            Exception = ex;
        }

    }

}
