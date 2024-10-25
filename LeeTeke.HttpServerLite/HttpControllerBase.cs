using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    public abstract class HttpControllerBase
    {

        private string _rootPrefix = "/";
        /// <summary>
        /// 路由前缀，默认为根路由'/'
        /// </summary>
        public string RoutePrefix
        {
            get => _rootPrefix; init
            {
                _rootPrefix = $"{value.Trim('/')}/";
                if (_rootPrefix == "//")
                    _rootPrefix = "/";
            }
        }



        internal Action<HttpListenerContext, Action<object[]?>> BeforeRouteAction { get => BeforeRoute; }

        internal HttpListenerBuilder _builder = null!;

        protected HttpListenerBuilder Builder => _builder;

        /// <summary>
        /// 路由之前的调用
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="next"></param>
        public virtual void BeforeRoute(HttpListenerContext listenerContext, Action<object[]?> next)
        {
            next(null);
        }

    }
}
