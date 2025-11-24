using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{

    /// <summary>
    /// HttpListener构建器
    /// </summary>
    public sealed class HttpListenerBuilder
    {

        internal HttpListener Listener => _listener;
        internal HttpApplicationOptions Options => _opt;

        internal Action<HttpListenerContext, Exception> RaiseRouteException { get; set; } = (l, ex) => l.Close(HttpStatusCode.InternalServerError);

        private readonly HttpListener _listener;//http服务
        private Action<HttpListenerContext, Action> _brforeRoute = (l, next) => next();//route前的操作
        private Action<HttpListenerContext> _routeFailure = l => l.Abort();//导航失败
   
        private readonly IHttpServierLiteRouter _httpRouter;//路由
        private HttpApplicationOptions _opt = null!;//参数


        /// <summary>
        /// 服务器名称
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// HttpListener构建器
        /// </summary>
        public HttpListenerBuilder(IHttpServierLiteRouter router)
        {
            _listener = new HttpListener();
            _httpRouter = router;
        }

        #region PublicMethods


        /// <summary>
        /// 允许此实例接收传入的请求。
        /// </summary>
        public void Run()
        {
            if (!HttpListener.IsSupported)
            {
                Logger("PlatformNotSupportedException！");
                throw new PlatformNotSupportedException();
            }

            try
            {
                _listener.Start();
                _listener.BeginGetContext(new AsyncCallback(GetContextCallCack), _listener);
                //启动成功

                if (_opt.Prefixes != null)
                {
                    Logger($"http服务器启动成功!监听地址为：{string.Join("\t", _opt.Prefixes)}\tHtml文件根目录为:{_opt?.RootPath}");

                }
                else
                {
                    Logger($"http服务器启动成功!监听端口为：{_opt?.Port}\tHtml文件根目录为:{_opt?.RootPath}");

                }

            }
            catch (Exception ex)
            {
                Logger("Http服务器启动异常!", ex);
                throw;
            }


        }
        /// <summary>
        /// 使此实例停止接收新的传入请求，并终止处理所有正在进行的请求
        /// </summary>
        public void Stop()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception ex)
            {
                Logger("Http服务器停止异常!", ex);
                throw;
            }

        }
        /// <summary>
        /// 立即关闭对象，丢弃所有当前排队的请求。
        /// </summary>
        public void Abort()
        {
            try
            {
                _listener.Abort();

            }
            catch (Exception ex)
            {
                Logger("Http服务器关闭对象失败!", ex);
                throw;
            }
        }
        /// <summary>
        ///立即关闭对象
        /// </summary>
        public void Close()
        {
            try
            {
                _listener.Close();
            }
            catch (Exception ex)
            {
                Logger("Http服务器关闭失败!", ex);
                throw;
            }

        }

        internal HttpListenerBuilder Build(HttpApplicationOptions opt)
        {
            _opt = opt;

            if (opt.Prefixes == null || opt.Prefixes.Length < 1)
            {
                opt.Prefixes = [$"http://*:{opt.Port}/"];
            }

            foreach (var item in opt.Prefixes)
                _listener.Prefixes.Add(item);
            return this;
        }

        /// <summary>
        /// 添加控制器
        /// </summary>
        /// <param name="controller"></param>
        public void ControllerAdd(object controller) => _httpRouter.ControllerAdd(controller, this);


        /// <summary>
        /// 映射添加
        /// 优先级最高
        /// </summary>
        /// <param name="path"></param>
        /// <param name="action"></param>
        public void Map(string path, Action<HttpListenerContext> action) => _httpRouter.Map(path, action);

        /// <summary>
        /// 在路由之前的方法，返回
        /// </summary>
        /// <param name="before"></param>
        public void BeforeRoute(Action<HttpListenerContext, Action> before) => _brforeRoute = before;

        /// <summary>
        /// 路由失败后
        /// </summary>
        /// <param name="after"></param>
        public void AfterRouteFailure(Action<HttpListenerContext> after) => _routeFailure = after;

        /// <summary>
        /// 路由异常捕获工厂
        /// </summary>
        /// <param name="factory"></param>
        public void RouteExceptionFactory(Action<HttpListenerContext, Exception> factory) => RaiseRouteException = factory;

        /// <summary>
        /// Router重设
        /// </summary>
        public void RouterReset() => _httpRouter?.Reset();
        #endregion


        #region Event

        /// <summary>
        /// 触发日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        private void Logger(string message, Exception? ex = null) => Task.Run(() => HttpServerLiteLogTrigger?.Invoke(this, new HttpServerLiteLogEventArgs(message, ex)));

        /// <summary>
        /// Log触发器
        /// </summary>
        public event EventHandler<HttpServerLiteLogEventArgs>? HttpServerLiteLogTrigger;

        #endregion

        #region PrivateMethod


        private void GetContextCallCack(IAsyncResult ar)
        {
            try
            {
                if (ar.AsyncState is HttpListener http)
                    if (http.IsListening)
                    {
                        HttpListenerContext httpListenerContext = http.EndGetContext(ar);
                        http.BeginGetContext(GetContextCallCack, http);//继续监听
                        HttpWork(httpListenerContext);
                    }
            }
            catch (Exception ex)
            {
                Logger("Http服务器上下文回调发生异常！", ex);
            }
        }

        private void HttpWork(HttpListenerContext listenerContext)
        {
            //服务头
            listenerContext.Response.Headers.Add("Server", ServerName);
            _brforeRoute(listenerContext, () =>
            {
                try
                {
                    if (!_httpRouter.Routing(listenerContext))
                    {
                        _routeFailure(listenerContext);
                    }
                }
                catch (Exception ex)
                {
                    Logger(ex.Message, ex);
                    RaiseRouteException(listenerContext, ex);
                }
            });

        }
        #endregion

    }

}
