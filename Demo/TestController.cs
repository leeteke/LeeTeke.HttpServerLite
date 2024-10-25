using LeeTeke.HttpServerLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{

    //必须基于 HttpControllerBase
    public class TestController : HttpControllerBase
    {
      

        public TestController()
        {
            //这里填写路由地址;
            //默认地址为‘/’
            //此地址的含义为 www.xxx.com/testcontroll/
            base.RoutePrefix = "/testcontroll/";
        }


        public override void BeforeRoute(HttpListenerContext listenerContext, Action<object[]?> next)
        {
            //这里是控制器内部的先行判断
            switch (listenerContext.Request.Url!.LocalPath)
            {
                case "/testcontroll/all/before":
                    //拦截
                    listenerContext.SendString("被拦截了");
                    break;
                case "/testcontroll/before":
                    //传参，传递的参数不要包含 HttpListenerContext，并且请按照相应路由方法的参数顺序进行传参（除去HttpListenerContext）。
                    next([DateTime.Now.ToString()]);
                    break;
                default:
                    //默认下一步，传参为空Array即可
                    next([]);
                    break;
            }


        }


        //1、方法必须是Public；
        //2、必须有 RouteAttribute，可以多个;
        //3、可以无参，但是http路由会在进入方法前结束http会话，并返回 httpstatuscode=200;
        //4、若有参数则首位参数必须是 HttpListenerContext listenerContext
        // www.xxx.com/testcontroll/ 路由到这里
        [Route("/")]
        public void Root(HttpListenerContext context)
        {
            context.SendString("这里是 TestController");
        }

        // www.xxx.com/testcontroll/a 路由到这里
        [LeeTeke.HttpServerLite.Route("a")]
        public void A(HttpListenerContext context)
        {
            context.SendString(context.Request.Url!.LocalPath);
        }

        // www.xxx.com/testcontroll/b 与 www.xxx.com/testcontroll/c路由到这里
        [LeeTeke.HttpServerLite.Route("b")]
        [LeeTeke.HttpServerLite.Route("c")]
        public void B(HttpListenerContext context)
        {
            context.SendString(context.Request.Url!.LocalPath);
        }

        // www.xxx.com/testcontroll/d 路由到这里，但是进入方法之前已被响应
        [LeeTeke.HttpServerLite.Route("d")]
        public void D()
        {
            Console.WriteLine("/testcontroll/d 发生了访问");
        }

        // www.xxx.com/testcontroll/all/ 以及 所有地址前缀为 www.xxx.com/testcontroll/all/ 都会路由到这里
        [LeeTeke.HttpServerLite.Route("all/", TakeOver =true)]
        public void All(HttpListenerContext context)
        {
            context.SendString(context.Request.Url!.LocalPath);
        }

        //多参数属性，如果在 BeforeRoute 方法里传递的参数个数多余本方法则会截取，若小于本方法，则不会进入此方法，并且http会做 501（NotImplemented）回应。
        // www.xxx.com/testcontroll/before路由到这里
        [LeeTeke.HttpServerLite.Route("before")]
        public void Before(HttpListenerContext context,string obj)
        {
            context.SendString(context.Request.Url!.LocalPath+"_"+obj);
        }
    }
}
