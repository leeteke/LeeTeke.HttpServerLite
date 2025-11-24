

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{ /// <summary>
  /// http路由器
  /// </summary>
    public class HttpRouter : IHttpServierLiteRouter
    {
        private readonly ConcurrentDictionary<string, HttpRoute> _mainDic;//主路由dir
        private readonly ConcurrentDictionary<string, HttpRoute> _takeOverDic;//接管路由dir
        private readonly ConcurrentDictionary<string, Action<HttpListenerContext>> _mapDic;//直接（且优先）

        /// <summary>
        /// 路由器
        /// </summary>
        public HttpRouter()
        {
            _mainDic = new ConcurrentDictionary<string, HttpRoute>();
            _takeOverDic = new ConcurrentDictionary<string, HttpRoute>();
            _mapDic = new ConcurrentDictionary<string, Action<HttpListenerContext>>();
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <returns></returns>
        public bool Routing(HttpListenerContext listenerContext)
        {
            try
            {
                if (listenerContext.Request.Url == null)
                    return false;

                string url = listenerContext.Request.Url.AbsolutePath;

                return RoutingGO(url, listenerContext);
            }
            catch (Exception ex)
            {

                throw new Exception($"路由策略执行异常[{listenerContext.Request.Url!.AbsolutePath}]", ex);
            }
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="urlLevel">路由等级</param>
        /// <returns></returns>
        public bool Routing(HttpListenerContext listenerContext, int urlLevel)
        {
            try
            {
                if (listenerContext.Request.Url == null)
                    return false;

                string url;
                if (listenerContext.Request.Url.Segments.Length > urlLevel)
                {
                    url = string.Join("", listenerContext.Request.Url.Segments.Skip(urlLevel));
                    if (!url.StartsWith("/"))
                        url = $"/{url}";
                }
                else if (listenerContext.Request.Url.Segments.Length == urlLevel)
                {
                    url = "/";
                }
                else
                {
                    return false;
                }

                return RoutingGO(url, listenerContext);
            }
            catch (Exception ex)
            {
                throw new Exception($"路由策略执行异常[{listenerContext.Request.Url!.AbsolutePath}]", ex);
            }
        }

        /// <summary>
        /// 添加导航
        /// </summary>
        /// <param name="controller">控制器类</param>
        /// <param name="builder">HttpListenerBuilder</param>
        /// <exception cref="Exception">非HttpControllerBase继承类</exception>
        public void ControllerAdd(object controller, HttpListenerBuilder builder)
        {

            if (controller is HttpControllerBase _base)
            {
                //获取缀
                var prefix = _base.RoutePrefix == "/" ? _base.RoutePrefix : $"/{_base.RoutePrefix}";

                _base._builder = builder;
                //获取所有的方法
                foreach (var method in controller.GetType().GetMethods())
                {
                    //获取方法是由存在RA
                    var raList = method.GetCustomAttributes<RouteAttribute>();
                    if (raList != null)
                    {
                        //多个RA列表加入dic
                        foreach (var item in raList)
                        {
                            //先判断是否存在更多的选项，有则忽略单个
                            if (item.RoutePaths != null && item.RoutePaths.Length > 0)
                            {
                                foreach (var urlItem in item.RoutePaths)
                                {
                                    _ = _mainDic.TryAdd(prefix + urlItem, new HttpRoute(method, controller, _base.BeforeRouteAction));
                                }
                            }
                            else
                            {
                                _ = _mainDic.TryAdd(prefix + item.RoutePath, new HttpRoute(method, controller, _base.BeforeRouteAction));
                                if (item.TakeOver)
                                    _ = _takeOverDic.TryAdd(prefix + item.RoutePath, new HttpRoute(method, controller, _base.BeforeRouteAction));
                            }
                        }
                    }
                }

            }
            else
            {
                throw new Exception("need to extend HttpControllerBase.");
            }
        }

        /// <summary>
        /// 路径映射
        /// 优先级高
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="context"></param>
        public void Map(string path, Action<HttpListenerContext> context)
        {
            _ = _mapDic.TryAdd(path, context);
        }

        /// <summary>
        /// 重置路由
        /// </summary>
        public void Reset()
        {
            _mainDic.Clear();
            _takeOverDic.Clear();
            _mapDic.Clear();
        }

        #region PrivateMethod

        private bool RoutingGO(string url, HttpListenerContext listenerContext)
        {
            //判断直接参数
            if (_mapDic.TryGetValue(url, out Action<HttpListenerContext>? go))
            {
                go.Invoke(listenerContext);
                return true;
            }

            //先找直接的
            if (_mainDic.TryGetValue(url, out HttpRoute? routing))
            {
                routing.Invoke(listenerContext);
                return true;
            }
            //再找包含的
            var to = _takeOverDic.Where(p => url.StartsWith(p.Key));
            if (to != null && to.Any())
            {
                to.OrderByDescending(p => p.Key.Length).First().Value.Invoke(listenerContext);
                return true;
            }


            return false;
        }



        #endregion
    }
}
