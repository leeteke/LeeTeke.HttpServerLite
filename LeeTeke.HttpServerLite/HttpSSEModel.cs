using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// server-sent-events 发送数据模型
    /// </summary>
    public class HttpSSEModel
    {
        /// <summary>
        /// 数据编号
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string? Event { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// 指定浏览器发起重连的时间间隔，单位ms
        /// </summary>
        public int? Retry { get; set; }


        /// <summary>
        /// 注释
        /// </summary>
        public string? Comment { get; set; }

            

        /// <summary>
        /// 输出为标准格式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Id != null)
                sb.Append($"id:{Id}\n");

            if (Event != null)
                sb.Append($"event:{Event}\n");

            if (Retry != null)
                sb.Append($"retry:{Retry}\n");

            if (Comment != null)
                sb.Append($":{Comment}\n");

            if (Data==null)
            {
                sb.Append('\n');
            }
            else
            {
                sb.Append($"data:{Data?.TrimEnd('\n')}\n\n");
            }
            return sb.ToString();
        }
    }




}
