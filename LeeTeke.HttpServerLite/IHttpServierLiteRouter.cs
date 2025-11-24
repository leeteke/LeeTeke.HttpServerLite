using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 适用于httpServerLite的路由接口
    /// </summary>
    public interface IHttpServierLiteRouter
    {
        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <returns></returns>
        bool Routing(HttpListenerContext listenerContext);

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="urlLevel">路由等级</param>
        /// <returns></returns>
        bool Routing(HttpListenerContext listenerContext, int urlLevel);


        /// <summary>
        /// 添加导航
        /// </summary>
        /// <param name="controller">控制器类</param>
        /// <param name="builder">HttpListenerBuilder</param>
        /// <exception cref="Exception">非HttpControllerBase继承类</exception>
        void ControllerAdd(object controller, HttpListenerBuilder builder);

        /// <summary>
        /// 路径映射
        /// 优先级高
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="context"></param>
        void Map(string path, Action<HttpListenerContext> context);

        /// <summary>
        /// 重置添加
        /// </summary>
        void Reset();
    }
}
