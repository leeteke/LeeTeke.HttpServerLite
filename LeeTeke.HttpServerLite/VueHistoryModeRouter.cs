using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 适用于Vue History导航的html文件路由
    /// </summary>
    public class VueHistoryModeRouter : HttpControllerBase
    {

      


        /// <summary>
        /// 鉴权
        /// </summary>
        public Func<HttpListenerContext, bool> AuthCallBack { get; set; } = p => true;

        /// <summary>
        /// 是否对鉴权资源也文件
        /// </summary>
        public bool IsAuthAssets { get; set; } = false;

        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool UseGZip { get; set; } = false;

        /// <summary>
        /// 是否启用缓存判断
        /// </summary>
        public bool UseCache_Lastmodified { get; set; } = true;

        /// <summary>
        /// 适用于Vue History导航的html文件路由
        /// </summary>
        public VueHistoryModeRouter(string prefix = "/")
        {
            RoutePrefix = prefix;
        }


        /// <summary>
        /// 首页
        /// </summary>
        /// <param name="listenerContext"></param>
        [Route("/", TakeOver = true)]
        public void Index(HttpListenerContext listenerContext)
        {
            if (!AuthCallBack(listenerContext))
                return;

            var requestUrl = listenerContext.Request.Url!;

            string indexPath = requestUrl.LocalPathConverter(HttpServerLite.GetBuilderOptions(Builder).RootPath);
           
            //判断是否有此文件
            if (File.Exists(indexPath))
            {
                indexPath= UseGZip ? listenerContext.GZipPathConverter(indexPath) : indexPath;
                if (UseCache_Lastmodified && listenerContext.LastmodifiedCheck(indexPath))
                {
                    listenerContext.NotModified();
                    return;
                }
                listenerContext.FileTransfer(indexPath);
            }
            else
            {
                indexPath = HttpServerLite.GetBuilderOptions(Builder).RootPath+ RoutePrefix+"index.html";
                if (!File.Exists(indexPath))
                {
                    listenerContext.Close();
                    return;
                }
                if (UseCache_Lastmodified && listenerContext.LastmodifiedCheck(indexPath))
                {
                    listenerContext.NotModified();
                    return;
                }

                listenerContext.HistroyMode(indexPath);
            }
        }

        /// <summary>
        /// 页面的资源(包含assets)
        /// </summary>
        /// <param name="listenerContext"></param>
        [Route("/assets/", TakeOver = true)]
        public void Assets(HttpListenerContext listenerContext)
        {
            if (IsAuthAssets && !AuthCallBack(listenerContext))
                return;
            var opt = HttpServerLite.GetBuilderOptions(Builder);
            var localPath = listenerContext.Request.Url!.LocalPathConverter(opt.RootPath);
            //先转换gzip
            var gPath = UseGZip ? listenerContext.GZipPathConverter(localPath) : localPath;
            if (!File.Exists(gPath))
            {
                listenerContext.Close();
                return;
            }

            if (UseCache_Lastmodified && listenerContext.LastmodifiedCheck(gPath))
            {
                listenerContext.NotModified();
                return;
            }

            listenerContext.FileTransfer(gPath, HttpContextType.AnalysisSuffix(localPath));
        }
    }
}
