using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 服务自身的日志输出参数
    /// </summary>
    public class HttpServerLiteLogEventArgs : EventArgs
    {

        /// <summary>
        /// 异常
        /// </summary>
        public Exception? Exception { get;  }
        /// <summary>
        /// 消息
        /// </summary>
        public string Msg { get;  }

        /// <summary>
        /// 服务自身的日志输出参数
        /// </summary>
        public HttpServerLiteLogEventArgs(string msg, Exception? ex = null)
        {
            Msg = msg;
            Exception = ex;
        }

    }

}
