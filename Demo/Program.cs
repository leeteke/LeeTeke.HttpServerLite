using LeeTeke.HttpServerLite;
using System.Runtime.CompilerServices;
using System.Timers;

namespace Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {

            //创建http服务
            var httpBuilder = HttpServerLite.CreateBuilder(new HttpApplicationOptions()
            {
                // Port = 80,默认80端口
                // Prefixes = ["https://127.0.0.1:443/"],与Port属性冲突，如果此选项有数值则port选项不生效,单独使用port只会监听http协议
                // RootPath = "./wwwroot/"
                //,ArgStr="" 自定义文本参数
            });

            //日志输出
            httpBuilder.HttpServerLiteLogTrigger += (sender,  args)=>
            {
                if (args.Exception == null)
                {
                    Console.WriteLine($"info:{args.Msg}");
                }
                else
                {
                    Console.WriteLine($"error:{args.Msg}\t{args.Exception}");
                }
            };

            //直接映射，优先级最高
            httpBuilder.Map("/test", p => p.SendString("你好"));

            //所有路由发生前
            httpBuilder.BeforeRoute((context, next) =>
            {

                var url = context.Request.Url!.LocalPath;
                Console.WriteLine($"request:{url}");
                //拦截阻止
                if (url == "/stop")
                {
                    context.SendString("禁止访问");
                    return;
                }


                //使用HSTS
                if (!context.HSTS())
                    return;

                //解决跨域问题
                context.CorsAllowHeaders().CorsAllowMethods().CorsAllowOrigin();

                //继续下一步
                next();

            });

            //路由失败，无路由触发
            httpBuilder.AfterRouteFailure(context =>
            {
                var url = context.Request.Url!.LocalPath;
                Console.WriteLine($"no route:{url}");
                context.SendString($"不存在当前的地址:{url}");
            });

            //路由时发生异常捕获(log也会捕获异常)
            httpBuilder.RouteExceptionFactory((context, ex) =>
            {
                var url = context.Request.Url!.LocalPath;
                Console.WriteLine($"route exception:{ex.Message}");
                context.Close(System.Net.HttpStatusCode.ServiceUnavailable);
            });
            //测试异常
            httpBuilder.Map("/ex", context =>
            {
                throw new Exception("thrwo exception !");
            });

            //使用Controller
            httpBuilder.ControllerAdd(new TestRootController());
            httpBuilder.ControllerAdd(new TestController());

            //提供了Vue文件的快速构建路由
            //参数输入基于RootPath位置
            //访问 www.xxx.com/vue/ 会直接运行 单页面模式 vue。
            httpBuilder.ControllerAdd(new VueHistoryModeRouter("/vue/") { UseCache_Lastmodified = true, UseGZip = true });

            //开始服务
            httpBuilder.Run();

            Console.ReadLine();
        }


    }
}
