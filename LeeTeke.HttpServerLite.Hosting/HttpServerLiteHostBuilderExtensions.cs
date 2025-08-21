using LeeTeke.HttpServerLite.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace LeeTeke.HttpServerLite
{
    public static class HttpServerLiteHostBuilderExtensions
    {
        /// <summary>
        /// 使用Builder
        /// </summary>
        /// <param name="builder">IHostBuilder</param>
        /// <param name="options">配置</param>
        /// <param name="configureServer">配置</param>
        /// <returns></returns>
        public static IHostBuilder UseHttpServerLite(this IHostBuilder builder, HttpApplicationOptions options, Action<HostBuilderContext, IServiceProvider, HttpListenerBuilder> configureServer)
        {


            builder.ConfigureServices((context, collection) =>
            {

                collection.AddHostedService(services =>
                {
                    var httpServer = HttpServerLite.CreateBuilder(options);
                    return new HttpServerListHostedService(
                        () =>
                        {
                            configureServer(context, services, httpServer);
                            httpServer.Run();
                            collection.AddSingleton(httpServer);
                        },
                        () =>
                        {
                            httpServer.Close();
                        });
                });
            });

            return builder;

        }

        /// <summary>
        /// 使用Builder,从IConfiguration读取"HttpServerLite",并反序列化成HttpApplicationOptions
        /// </summary>
        /// <param name="builder">IHostBuilder</param>
        /// <param name="configureServer">配置</param>
        /// <returns></returns>
        public static IHostBuilder UseHttpServerLite(this IHostBuilder builder, Action<HostBuilderContext, IServiceProvider, HttpListenerBuilder> configureServer)
        {

            builder.ConfigureServices((context, collection) =>
            {

                collection.AddHostedService(services =>
                {
                    var httpServer = HttpServerLite.CreateBuilder(GetOptionsAppSettinJson(context.Configuration) ?? new HttpApplicationOptions());
                    return new HttpServerListHostedService(
                        () =>
                        {
                            configureServer(context, services, httpServer);
                            httpServer.Run();
                            collection.AddSingleton(httpServer);
                        },
                        () =>
                        {
                            httpServer.Close();
                        });
                });
            });

            return builder;

        }

        /// <summary>
        /// 从IoC容器里面找
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static HttpListenerBuilder ControllerAddFromIoc(this HttpListenerBuilder builder, IServiceProvider service)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(p => p.BaseType == typeof(HttpControllerBase));
                if (types.Any())
                {
                    foreach (var @type in types)
                    {
                        var controller = service.GetService(@type);
                        if (controller != null)
                            builder.ControllerAdd(controller);
                    }
                }
            }
            return builder;
        }



        private static HttpApplicationOptions? GetOptionsAppSettinJson(IConfiguration configuration)
        {
            try
            {
                var options = configuration.GetSection("HttpServerLite").Get<HttpApplicationOptions>();
                return options;
            }
            catch
            {
                return null;
            }
        }

    }
}
