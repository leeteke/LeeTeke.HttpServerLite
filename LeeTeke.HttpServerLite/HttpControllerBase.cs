﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 控制器基本类
    /// </summary>
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
        public virtual async void BeforeRoute(HttpListenerContext listenerContext, Action<object[]?> next)
        {
            try
            {
                await BeforeRouteAsync(listenerContext, next);

            }
            catch (Exception ex)
            {
                RaiseException(listenerContext, ex);
            }
        }

        /// <summary>
        /// 路由之前的调用【异步】
        /// <para>与同步方法排斥，二者存在，则本方法不生效</para>
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="next"></param>
        public virtual async Task BeforeRouteAsync(HttpListenerContext listenerContext, Action<object[]?> next)
        {
            await Task.CompletedTask;
            next(null);
        }

        /// <summary>
        /// 异常上升至RouteExceptionFactory处理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ex"></param>
        public void RaiseException(HttpListenerContext context, Exception ex)
        {
            _builder._routeExceptionFactory(context, ex);
        }

    }
}
