using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LeeTeke.HttpServerLite
{
    public class HttpRouterAOT : IHttpServierLiteRouter
    {

        private readonly ConcurrentDictionary<string, Action<HttpListenerContext>> _mainDic;//主路由dir

        private readonly ConcurrentDictionary<string, Action<HttpListenerContext>> _takeOverDic;//接管路由dir

        private readonly ConcurrentDictionary<string, Action<HttpListenerContext>> _mapDic;//直接（且优先）
        public HttpRouterAOT()
        {
            _mainDic = new ConcurrentDictionary<string, Action<HttpListenerContext>>();
            _takeOverDic = new ConcurrentDictionary<string, Action<HttpListenerContext>>();
            _mapDic = new ConcurrentDictionary<string, Action<HttpListenerContext>>();
        }
        public void ControllerAdd(object controller, HttpListenerBuilder builder)
        {
            if (controller is HttpControllerBase _base)
            {
                _base._builder = builder;
            }
        }

        public void Map(string path, Action<HttpListenerContext> context)
        {
            _ = _mapDic.TryAdd(path, context);
        }

        public void Reset()
        {
            _mainDic.Clear();
            _takeOverDic.Clear();
            _mapDic.Clear();
        }

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

        private bool RoutingGO(string url, HttpListenerContext listenerContext)
        {
            //判断直接参数
            if (_mapDic.TryGetValue(url, out Action<HttpListenerContext>? go))
            {
                go.Invoke(listenerContext);
                return true;
            }

            //先找直接的
            if (_mainDic.TryGetValue(url, out Action<HttpListenerContext>? routing))
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

        public void MainAdd(string routePath, Action<HttpListenerContext> context)=> _mainDic.TryAdd(routePath, context);
        public void TakeOverAdd(string routePath, Action<HttpListenerContext> acontext) => _takeOverDic.TryAdd(routePath, acontext);
    }
}
