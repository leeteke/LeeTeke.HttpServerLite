using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    public class HttpApplicationOptions
    {
        /// <summary>
        /// 监听地址
        /// </summary>
        public string[]? Prefixes { get; set; }
        /// <summary>
        /// 监听端口
        /// 当Prefixs存在时次参数无效
        /// </summary>
        public int Port { get; set; } = 80;

        /// <summary>
        /// Root路径
        /// </summary>
        public string RootPath { get; set; }= AppDomain.CurrentDomain.BaseDirectory + "/wwwroot/";

        /// <summary>
        /// 其他参数
        /// </summary>
        public string? ArgStr { get; set; }
    }
}
