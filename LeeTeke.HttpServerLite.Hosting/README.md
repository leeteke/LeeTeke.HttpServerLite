
# LeeTeke.HttpServerLite.Hosting
· LeeTeke.HttpServerLite for Microsoft.Extensions.Hosting
## Nuget
[![NUGET](https://img.shields.io/badge/nuget-1.1.4-blue.svg)](https://www.nuget.org/packages/LeeTeke.HttpServerLite.Hosting)

    dotnet add package LeeTeke.HttpServerLite.Hosting --version 1.1.4


## 基本使用方法
``` csharp
using LeeTeke.HttpServerLite;
using LeeTeke.HttpServerLite.Hosting

  
            var _hosting = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configuration =>
                {
                    configuration.AddJsonFile("appsettings.json", false, true);
                    configuration.AddCommandLine(args);

                })
                .ConfigureServices((hosting, service) =>
                {
                    service.AddSingleton<TestRootController>();
                    service.AddSingleton<TestController>();

                })
                //使用 UseHttpServerLite 后会自动 将 HttpListenerBuilder 注册为 Singleton。 
                //手动配置
                //.UseHttpServerLite(new HttpApplicationOptions() { Port=81},HttpServerLiteConfigure)
                //使用配置文件配置版本
                //会自动读取 配置文件下的 HttServerLite 配置项，该配置项为 HttpApplicationOptions的序列化，如下所示
                /*
                     "HttpServerLite": {
                        //监听地址，值类型string[]
                        "Prefixes": null,
                        //监听端口，当Prefixes存在值时，此配置项不生效
                        "Port": 80,
                        //网页根目录
                        "RootPath": "./wwwroot/",
                        //其他自定义参数，值类型string
                        "ArgStr": null
                      }
                */
                .UseHttpServerLite(HttpServerLiteConfigure)
                .Build();

            _hosting.Start();
            _hosting.WaitForShutdown();



        static void HttpServerLiteConfigure(HostBuilderContext hosting, IServiceProvider services, HttpListenerBuilder httpBuilder)
        {
            //日志输出
            httpBuilder.HttpServerLiteLogTrigger += (sender, args) =>
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

            //使用Controller
            httpBuilder.ControllerAdd(services.GetRequiredService<TestRootController>());
            httpBuilder.ControllerAdd(services.GetRequiredService<TestController>());

            //或者直接从IOC里面找
            //httpBuilder.ControllerAddFromIoc(services)

            //提供了Vue文件的快速构建路由
            //参数输入基于RootPath位置
            //访问 www.xxx.com/vue/ 会直接运行 单页面模式 vue。
            httpBuilder.ControllerAdd(new VueHistoryModeRouter("/vue/") { UseCache_Lastmodified = true, UseGZip = true });


            //这里不用写Run了，服务会自动启动
            //httpBuilder.Run();
        }
```