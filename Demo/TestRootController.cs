using LeeTeke.HttpServerLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    internal class TestRootController : HttpControllerBase
    {


        public TestRootController()
        {
            //监听的默认路由地址以 ‘/’ 为结尾（会自动格式化）
            //base.RoutePrefix="/" ;代表监听一级路由地址，www.xxx.com与www.xxx.com/级别下的
        }

              
               
        //www.xxx.com与www.xxx.com/ 将路由到这里
        [Route("/")]
        public void Root(HttpListenerContext listenerContext)
        {
            listenerContext.SendString("这里是首页");
        }

        //www.xxx.com/testcontroll 将路由到这里,但是 www.xxx.com/testcontroll/ 不会路由到这里
        [Route("/testcontroll")]
        public void Testcontroll(HttpListenerContext httpListenerContext)
        {
            httpListenerContext.SendString("这里跟 TestController 不是一个路径");
        }

        //这里列举了关于HttpListenerContext 的响应扩展方法
        [Route("/test")]
        public void Test(HttpListenerContext listenerContext)
        {




            //发送Bytes,httpcode=200
            listenerContext.SendBytes(bytes: [], contentType: "text/plain", encoding: "utf-8");

            //发送 object 自动序列化，使用JsonHelper方法带的已经编辑好的序列化规则
            listenerContext.SendJSObject(new
            {
                success = true,
                msg = "OK",
                data = new
                {
                    time = HttpServerLite.LocalToGMT(DateTime.Now),
                }
            }, JsonHelper.JSOCamelNotNull);

            //发送 stream数据流
            using var st = new FileStream("./wwwroot/vue/index.html", FileMode.Open);
            listenerContext.SendStream(st, HttpContextType.Default_Html);

            //发送 字符串
            listenerContext.SendString("字符串");

            //使用 Sever-Sent-Event 服务
            listenerContext.UseSse(out string? lastEventID);
            listenerContext.SseSend(new HttpSSEModel() { Data = "你好", Id = lastEventID });

            //弹窗跳转
            listenerContext.AlertJump("这是弹窗信息,点击确定跳转连接~", "https://github.com/leeteke");

            //发送文件
            listenerContext.FileTransfer("./wwwroot/vue/index.html", HttpContextType.Default_Html);
            //发送文件限速
            listenerContext.FileRateLimitingTransferResult(10, 1024, "./wwwroot/vue/index.html", HttpContextType.Default_Html);
            //文件断点续传
            listenerContext.FileBreakpointResume("./wwwroot/vue/index.html", HttpContextType.Default_Html);
            //文件带限速的断点续传
            listenerContext.FileRateLimitingBreakpointResume(10, 1024, "./wwwroot/vue/index.html", HttpContextType.Default_Html);


            listenerContext.LastmodifiedCheck("./wwwroot/vue/index.html");
            listenerContext.HistroyMode("./wwwroot/vue/index.html");
            listenerContext.Close(HttpStatusCode.OK);
            listenerContext.NotModified();
            listenerContext.Abort();

           
        }
    }
}
