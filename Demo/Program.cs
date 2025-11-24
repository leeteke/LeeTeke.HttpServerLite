using LeeTeke.HttpServerLite;
using System.Runtime.CompilerServices;
using System.Timers;
using LeeTeke.HttpServerLite.AOT;
using Microsoft.Extensions.DependencyInjection;

namespace Demo
{
    internal class Program
    {

        

        static void Main(string[] args)
        {


            _services = ConfigureServices();

            //创建http服务
            //var httpBuilder = HttpServerLite.CreateBuilder(new HttpApplicationOptions()
            //{
            //    Port = 1443,//默认80端口
            //    // Prefixes = ["https://127.0.0.1:443/"],与Port属性冲突，如果此选项有数值则port选项不生效,单独使用port只会监听http协议
            //    // RootPath = "./wwwroot/"
            //    //,ArgStr="" 自定义文本参数
            //});

            //创建http服务，并支持AOT
            var httpBuilder = HttpServerLite.CreateBuilder(HttpServerLiteRuterAOT.Router, new HttpApplicationOptions()
            {
                 Port = 1443,//默认80端口
                // Prefixes = ["https://127.0.0.1:443/"],与Port属性冲突，如果此选项有数值则port选项不生效,单独使用port只会监听http协议
                // RootPath = "./wwwroot/"
                //,ArgStr="" 自定义文本参数
            } );

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
                //if (!context.HSTS())
                //    return;

                ////解决跨域问题
                //context.CorsAllowHeaders().CorsAllowMethods().CorsAllowOrigin();

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

            //路由时发生异常捕获(log会捕捉非Task任务，如果是Task任务则只能在这里捕获。注意 async void 无法捕获。)
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


            //启用AOT路由
            httpBuilder.UseRouterAOT(Services);
            
            //手动使用ControllerAdd
            //【启用AOT路由后此方法不可用】
            //httpBuilder.ControllerAdd(new TestRootController());
            //httpBuilder.ControllerAdd(new TestController>());

            //或者直接从IOC里面找
            //【启用AOT路由后此方法不可用】
            //httpBuilder.ControllerAddFromIoc(Services);

            //提供了Vue文件的快速构建路由
            //参数输入基于RootPath位置
            //访问 www.xxx.com/vue/ 会直接运行 单页面模式 vue。
            //【启用AOT路由后此方法不可用，可创建基于VueHistoryModeRouter的类，并注册以及编辑构造函数】
            //httpBuilder.ControllerAdd(new VueHistoryModeRouter("/vue/") { UseCache_Lastmodified = true, UseGZip = true });

            //开始服务
            httpBuilder.Run();

            Console.ReadLine();
        }


        #region 若想使用AOT，则必须使用Ioc


        private static IServiceProvider _services = null!;
        /// <summary>
        /// 服务
        /// </summary>
        public static IServiceProvider Services { get => _services; }


        /// <summary>
        /// 配置IOC
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestRootController>();
            services.AddSingleton<TestController>();
            services.AddSingleton<VueHistoryModeRouter>();
            return services.BuildServiceProvider();
        }
        #endregion



    }
}
