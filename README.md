# LeeTeke.HttpServerLite
【现已支持Native AOT！！！】
· 基于http.sys的HttpListener制作的轻量http代理服务器； 

· 内置路由方法，可通过映射或者创建控制器来进行路由操作；[1.2.0以后的版本支持将路由方法接口化，以支持AOT，详情请见LeeTeke.HttpServerLite.AOT]

· 内置多个HttpListenerContext扩展方法，方便进行http请求或响应操作；

## Nuget
[![NUGET](https://img.shields.io/badge/nuget-1.2.0-blue.svg)](https://www.nuget.org/packages/LeeTeke.HttpServerLite)

    dotnet add package LeeTeke.HttpServerLite --version 1.2.0

## 基本使用方法

### 构建 
```csharp
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
```

### HttpControllerBase 的基本使用

``` csharp
// Controller 使用
// 继承 HttpControllerBase

  using LeeTeke.HttpServerLite;

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

      //请注意 此方法 谨慎使用 async void 形式 ，若必须使用，则请注意在内部使用try catch;或者使用 override async Task BeforeRouteAsync;
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

      //与BeforeRoute仅可使用一种。两者存在，则此异步方法不生效。
      public override Task BeforeRouteAsync(HttpListenerContext listenerContext, Action<object[]?> next)
      {
          return base.BeforeRouteAsync(listenerContext, next);
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


      //关于方法体里的异常说明
        //关于方法的异常捕获，此写法可触发RouteExceptionFactory，同时会触发Log里的Exception;
        [Route("/taskvoid")]
        public void TaskVoid(HttpListenerContext listenerContext)
        {
            throw new Exception("这是个常规任务测试异常");
        }


        //关于方法的异常捕获，此写法可触发RouteExceptionFactory，但不会触发Log里的Exception;
        [Route("/task")]
        public async Task TaskA(HttpListenerContext listenerContext)
        {
            await Task.CompletedTask;
            throw new Exception("这是个Task任务测试异常");
        }
        //这种异常不会捕获，请谨慎使用！请谨慎使用！请谨慎使用！
        //若必须使用，则请注意在内部使用try catch;
        [Route("/taskasync")]
        public async void TaskB(HttpListenerContext listenerContext)
        {
            try
            {

                await Task.CompletedTask;
                throw new Exception("这种异常不会捕获，请请注意使用！");
            }
            catch (Exception ex)
            {

                //自己处理异常或者将异常传递给路由异常工厂处理。不会触发Log里的Exception;
                this.RaiseException(listenerContext, ex);
            }
        }
  }
```

### HttpListenerContext 扩展方法的基本使用
```csharp
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
```

