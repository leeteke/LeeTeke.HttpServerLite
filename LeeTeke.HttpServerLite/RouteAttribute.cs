using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 路由
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute
    {
        
        /// <summary>
        /// 单个
        /// </summary>
        public string RoutePath { get; } = null!;
        /// <summary>
        /// 列表
        /// </summary>
        public string[]? RoutePaths { get; }
        /// <summary>
        /// 是否接管这个路径下之后所有的Url
        /// </summary>
        public bool TakeOver { get; set; }

        /// <summary>
        /// 规则：
        /// <para>会自动去除去后尾的 '/' ；</para>
        /// <para>当TookOver为True时自动后置追加'/'；</para> 
        /// </summary>
        /// <param name="routePath"></param>
        public RouteAttribute(string routePath="")
        {
            RoutePath = $"{routePath?.Trim('/')}";
            if (TakeOver)
                RoutePath += "/";
        }

        /// <summary>
        /// 多个地址
        /// </summary>
        /// <param name="routePaths"></param>
        public RouteAttribute(params string[] routePaths)
        {

            RoutePaths = new string[routePaths.Length];
            for (int i = 0; i < routePaths.Length; i++)
            {
                RoutePaths[i] = $"{routePaths[i].Trim('/')}";
            }
        }

    }
}
