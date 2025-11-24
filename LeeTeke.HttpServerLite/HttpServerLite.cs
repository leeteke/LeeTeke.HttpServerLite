using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Collections.Specialized;
using System.Web;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// HttpServerLite
    /// </summary>
    public static class HttpServerLite
    {

        /// <summary>
        /// 创建构造器
        /// </summary>
        /// <param name="args">启动参数</param>
        /// <returns></returns>
        public static HttpListenerBuilder CreateBuilder(params string[] args)
        {


            if (args.Length > 1)
            {
                HttpApplicationOptions? options = null;
                var aIndex = Array.IndexOf(args, "-a");

                if (aIndex > -1 && aIndex < args.Length - 1 && System.IO.File.Exists(args[aIndex + 1]))
                {
                    options = JsonSerializer.Deserialize<HttpApplicationOptions>(File.ReadAllText(args[aIndex + 1]));
                }

                var pIndex = Array.IndexOf(args, "-p");
                if (pIndex > -1 && pIndex < args.Length - 1 && int.TryParse(args[pIndex + 1], out int p))
                {
                    options ??= new HttpApplicationOptions();
                    options.Port = p;
                }


                var hIndex = Array.IndexOf(args, "-h");
                if (hIndex > -1 && hIndex < args.Length - 1 && Directory.Exists(args[hIndex + 1]))
                {
                    options ??= new HttpApplicationOptions();
                    options.RootPath = args[hIndex + 1];
                }

                options ??= new HttpApplicationOptions();

                return CreateBuilder(options);


            }
            else
            {
                return CreateBuilder();
            }

        }
        /// <summary>
        /// 创建构造器
        /// </summary>
        /// <param name="port">监听端口号，默认80</param>
        /// <returns></returns>
        public static HttpListenerBuilder CreateBuilder(int port = 80)
        {
            return CreateBuilder(new HttpApplicationOptions() { Port = port });
        }
        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="opt">完整参数</param>
        /// <returns></returns>
        public static HttpListenerBuilder CreateBuilder(HttpApplicationOptions opt)
        {
            return CreateBuilder(null, opt);
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="router">自定义router</param>
        /// <param name="opt">完整参数</param>
        /// <returns></returns>
        public static HttpListenerBuilder CreateBuilder(IHttpServierLiteRouter? router, HttpApplicationOptions opt)
        {
            return new HttpListenerBuilder(router ?? new HttpRouter()).Build(opt);
        }

        /// <summary>
        /// 获取http参数
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static HttpApplicationOptions GetBuilderOptions(HttpListenerBuilder builder)
        {
            return builder.Options;
        }


        /// <summary>
        /// 获取HttpListener
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static HttpListener GetBuilderListener(HttpListenerBuilder builder)
        {
            return builder.Listener;
        }






        /// <summary>
        /// 默认的编码方式
        /// </summary>
        public const string DefaultEncoding = "utf-8";

        #region 数据流传输


        /// <summary>
        /// 发送String
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="data">数据</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static (bool success, string? error) SendString(this HttpListenerContext listenerContext, string data, string encoding = DefaultEncoding)
        {
            return SendBytes(listenerContext, Encoding.GetEncoding(encoding).GetBytes(data), HttpContextType.Encoder(HttpContextType.Default_Txt, encoding), encoding);
        }

        /// <summary>
        /// 发送Byte[]
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="bytes">数据</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static (bool isSend, string? error) SendBytes(this HttpListenerContext listenerContext, byte[]? bytes, string? contentType = null, string encoding = DefaultEncoding)
        {
            try
            {
                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentType = contentType ?? listenerContext.Request.Url.AnalysisUri(encoding);
                listenerContext.Response.ContentLength64 = 0;
                if (bytes == null)
                {
                    listenerContext.Response.Close();
                }
                else
                {
                    listenerContext.Response.ContentLength64 = bytes.Length;
                    Stream stream = listenerContext.Response.OutputStream;
                    stream.Write(bytes, 0, bytes.Length);
                    listenerContext.Response.Close();
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                listenerContext.Abort();
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// 以JS的形式发送结果
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="obj">Object</param>
        /// <param name="serializerOptions">序列化参数</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static (bool success, string? error) SendJSObject(this HttpListenerContext listenerContext, object obj, JsonSerializerOptions? serializerOptions = null, string encoding = DefaultEncoding)
        {
            return listenerContext.SendBytes(JsonHelper.SerializeToBytes(obj, serializerOptions), HttpContextType.Encoder(HttpContextType.Default_Json, encoding), encoding);
        }

        /// <summary>
        /// 发送数据流
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="stream">Stream</param>
        /// <param name="contentType">数据类型</param>
        /// <returns></returns>
        public static bool SendStream(this HttpListenerContext listenerContext, Stream stream, string contentType)
        {
            try
            {

                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentType = contentType;
                listenerContext.Response.ContentLength64 = stream.Length;
                stream.CopyTo(listenerContext.Response.OutputStream);
                listenerContext.Response.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// 跳转
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="url">跳转地址</param>
        /// <param name="code">默认Redirect</param>
        public static void Redirect(this HttpListenerContext listenerContext, string url, HttpStatusCode code = HttpStatusCode.Redirect)
        {
            try
            {
                listenerContext.Response.Headers.Add("location", url);
                listenerContext.Response.StatusCode = (int)code;
                listenerContext.Response.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 相应状态并关闭
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="statusCode">默认404</param>
        public static void Close(this HttpListenerContext listenerContext, HttpStatusCode statusCode = HttpStatusCode.NotFound)
        {
            try
            {
                listenerContext.Response.StatusCode = (int)statusCode;
                listenerContext.Response.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 关闭连接不做任何返回
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        public static void Abort(this HttpListenerContext listenerContext)
        {
            try
            {
                listenerContext.Response.Abort();
            }
            catch
            {
            }
        }

        /// <summary>
        /// NotModified
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        public static void NotModified(this HttpListenerContext listenerContext)
        {

            try
            {
                listenerContext.Response.StatusCode = 304;
                listenerContext.Response.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// history模式传输
        /// <para>也就是将所有路径指定传输同一个html文件</para>
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="htmlPath">html文件地址</param>
        public static void HistroyMode(this HttpListenerContext listenerContext, string htmlPath)
        {
            listenerContext.FileTransfer(htmlPath, HttpContextType.Encoder(HttpContextType.Default_Html, Encoding.UTF8.WebName));
        }

        #endregion


        #region 文件资源传输



        /// <summary>
        /// 文件传输，不返回结果无论成功与否关闭本次请求
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">路径</param>
        /// <param name="contentType">内容类型</param>
        public static void FileTransfer(this HttpListenerContext listenerContext, string filePath, string? contentType = null)
        {
            if (!FileTransferResult(listenerContext, filePath, contentType))
            {
                listenerContext.Close();
            }
        }


        /// <summary>
        /// 文件传输并返回传输的结果
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">路径</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>true:文件传输并关闭了连接</returns>
        public static bool FileTransferResult(this HttpListenerContext listenerContext, string filePath, string? contentType = null)
        {
            try
            {
                using FileStream fs = File.OpenRead(filePath);
                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentType = contentType ?? HttpContextType.AnalysisSuffix(filePath);
                listenerContext.Response.ContentLength64 = fs.Length;
                fs.CopyTo(listenerContext.Response.OutputStream);
                listenerContext.Response.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }



        /// <summary>
        /// 文件断点续传
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否执行成功</returns>
        public static bool FileBreakpointResume(this HttpListenerContext listenerContext, string filePath, string? fileType = null)
        {
            try
            {



                var file = new FileInfo(filePath);

                var (startIndex, endIndex) = GetRangeHeader(listenerContext, file);

                using FileStream fs = file.OpenRead();

                listenerContext.Response.ContentType = fileType ?? HttpContextType.AnalysisSuffix(filePath);
                listenerContext.Response.Headers.Add("Last-Modified", LocalToGMT(file.LastAccessTime));
                listenerContext.Response.Headers.Add("Accept-Ranges", "bytes");
                if (fs.CanSeek && (startIndex != null || endIndex != null))
                {
                    listenerContext.Response.StatusCode = 206;
                    if (startIndex != null)
                    {
                        fs.Seek((long)startIndex, SeekOrigin.Begin);

                        //块级别传输
                        if (endIndex != null && endIndex < fs.Length - 1)
                        {

                            long rangeLen = (long)(endIndex - startIndex + 1);

                            listenerContext.Response.ContentLength64 = rangeLen;
                            listenerContext.Response.Headers.Add("Content-Range", $"bytes {startIndex}-{endIndex}/{fs.Length}");

                            byte[] buffer = new byte[1024];
                            int read = 0;
                            long total = 0;
                            while (total < rangeLen && (read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                total += read;
                                if (total > rangeLen) //超出的长度，截取
                                {
                                    read -= (int)(total - rangeLen);
                                    total = rangeLen;
                                }
                                listenerContext.Response.OutputStream.Write(buffer, 0, read);
                            }

                            listenerContext.Response.Close();
                            return true;
                        }
                        listenerContext.Response.ContentLength64 = fs.Length - (long)startIndex;
                        listenerContext.Response.Headers.Add("Content-Range", $"bytes {startIndex}-{fs.Length - 1}/{fs.Length}");
                    }
                    else if (endIndex != null)
                    {
                        listenerContext.Response.ContentLength64 = (long)endIndex;
                        listenerContext.Response.Headers.Add("Content-Range", $"bytes {endIndex}-{fs.Length - 1}/{fs.Length}");
                        fs.Seek((long)endIndex, SeekOrigin.End);
                    }

                    fs.CopyTo(listenerContext.Response.OutputStream);
                    listenerContext.Response.Close();
                    return true;
                }

                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentLength64 = fs.Length;
                listenerContext.Response.Headers.Add("Content-Range", $"bytes {0}-{fs.Length - 1}/{fs.Length}");

                fs.CopyTo(listenerContext.Response.OutputStream);
                listenerContext.Response.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }





        /// <summary>
        /// 文件限速传输不返回结果无论成功与否关闭本次请求
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">文件地址</param>
        /// <param name="millisecondsInterval">毫秒范围</param>
        /// <param name="size">范围传输大小</param>
        /// <param name="fileType">文件类型</param>
        public static void FileRateLimitingTransfer(this HttpListenerContext listenerContext, int millisecondsInterval, int size, string filePath, string? fileType = null)
        {
            if (!FileRateLimitingTransferResult(listenerContext, millisecondsInterval, size, filePath, fileType))
            {
                listenerContext.Close();
            }
        }

        /// <summary>
        /// 文件限速传输返回结果
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">文件地址</param>
        /// <param name="millisecondsInterval">毫秒范围</param>
        /// <param name="size">范围传输大小</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否执行</returns>
        public static bool FileRateLimitingTransferResult(this HttpListenerContext listenerContext, int millisecondsInterval, int size, string filePath, string? fileType = null)
        {
            try
            {
                using FileStream fs = File.OpenRead(filePath);
                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentType = fileType ?? HttpContextType.AnalysisSuffix(filePath);
                listenerContext.Response.ContentLength64 = fs.Length;
                byte[] buffer = new byte[size];
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Task.Delay(millisecondsInterval).Wait();
                    listenerContext.Response.OutputStream.Write(buffer, 0, read);
                }

                listenerContext.Response.Close();
                return true;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 支持断点续传的限速文件传输
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">文件地址</param>
        /// <param name="millisecondsInterval">毫秒范围</param>
        /// <param name="size">范围传输大小</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否执行</returns>
        public static bool FileRateLimitingBreakpointResume(this HttpListenerContext listenerContext, int millisecondsInterval, int size, string filePath, string? fileType = null)
        {
            try
            {



                var file = new FileInfo(filePath);

                var (startIndex, endIndex) = GetRangeHeader(listenerContext, file);

                using FileStream fs = file.OpenRead();

                listenerContext.Response.ContentType = fileType ?? HttpContextType.AnalysisSuffix(filePath);
                listenerContext.Response.Headers.Add("Last-Modified", LocalToGMT(file.LastAccessTime));
                listenerContext.Response.Headers.Add("Accept-Ranges", "bytes");
                if (fs.CanSeek && (startIndex != null || endIndex != null))
                {
                    listenerContext.Response.StatusCode = 206;
                    if (startIndex != null)
                    {
                        fs.Seek((long)startIndex, SeekOrigin.Begin);

                        //块级别传输
                        if (endIndex != null && endIndex < fs.Length - 1)
                        {

                            long rangeLen = (long)(endIndex - startIndex + 1);

                            listenerContext.Response.ContentLength64 = rangeLen;
                            listenerContext.Response.Headers.Add("Content-Range", $"bytes {startIndex}-{endIndex}/{fs.Length}");

                            byte[] r_buffer = new byte[size];
                            int r_read = 0;
                            long r_total = 0;
                            while (r_total < rangeLen && (r_read = fs.Read(r_buffer, 0, r_buffer.Length)) > 0)
                            {
                                Task.Delay(millisecondsInterval).Wait();
                                r_total += r_read;
                                if (r_total > rangeLen) //超出的长度，截取
                                {
                                    r_read -= (int)(r_total - rangeLen);
                                    r_total = rangeLen;
                                }
                                listenerContext.Response.OutputStream.Write(r_buffer, 0, r_read);
                            }

                            listenerContext.Response.Close();
                            return true;
                        }
                        listenerContext.Response.ContentLength64 = fs.Length - (long)startIndex;
                        listenerContext.Response.Headers.Add("Content-Range", $"bytes {startIndex}-{fs.Length - 1}/{fs.Length}");
                    }
                    else if (endIndex != null)
                    {
                        listenerContext.Response.ContentLength64 = (long)endIndex;
                        listenerContext.Response.Headers.Add("Content-Range", $"bytes {endIndex}-{fs.Length - 1}/{fs.Length}");
                        fs.Seek((long)endIndex, SeekOrigin.End);
                    }
                    byte[] z_buffer = new byte[size];
                    int z_read;
                    while ((z_read = fs.Read(z_buffer, 0, z_buffer.Length)) > 0)
                    {
                        Task.Delay(millisecondsInterval).Wait();
                        listenerContext.Response.OutputStream.Write(z_buffer, 0, z_read);
                    }
                    listenerContext.Response.Close();
                    return true;
                }

                listenerContext.Response.StatusCode = 200;
                listenerContext.Response.ContentLength64 = fs.Length;
                listenerContext.Response.Headers.Add("Content-Range", $"bytes {0}-{fs.Length - 1}/{fs.Length}");

                byte[] buffer = new byte[size];
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Task.Delay(millisecondsInterval).Wait();
                    listenerContext.Response.OutputStream.Write(buffer, 0, read);
                }
                listenerContext.Response.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }



        #endregion


        #region 加工方法



        /// <summary>
        /// 通过Lastmodified 缓存判断
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">本地真实文件</param>
        /// <returns>请求是否跟本地的文件一致</returns>
        public static bool LastmodifiedCheck(this HttpListenerContext listenerContext, string filePath)
        {
            try
            {
                var file = new FileInfo(filePath);
                var gtTime = LocalToGMT(file.LastWriteTime);
                //判断是否有缓存时间
                if (listenerContext.Request.Headers["If-Modified-Since"] == gtTime)
                {
                    listenerContext.Response.Headers.Add("Data", gtTime);
                    listenerContext.Response.StatusCode = 304;
                    listenerContext.Response.Close();
                    return true;
                }

                listenerContext.Response.Headers.Add("Last-Modified", gtTime);

                return false;
            }
            catch
            {
                return false;
            }


        }


        /// <summary>
        /// GZip路径处理
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="filePath"></param>
        /// <param name="gzipMade">参数为当前请求的文件路径，返回制造后gzip文件路径</param>
        /// <returns>若请求支持gzip,则通过加工厂制造后的gzip路径</returns>
        public static string GZipPathConverter(this HttpListenerContext listenerContext, string filePath, Func<string, string> gzipMade)
        {
            string path = filePath;
            if (listenerContext.Request.Headers["Accept-Encoding"]?.Contains("gzip") == true)
            {

                path = gzipMade(filePath);
                listenerContext.Response.Headers.Add("Content-Encoding", "gzip");
            }
            return path;
        }

        /// <summary>
        /// GZip路径处理
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>默认同木下压缩文件为.gz格式，若存在文件，则返回.gz文件路径，并最佳ContentEncoding头</returns>
        public static string GZipPathConverter(this HttpListenerContext listenerContext, string filePath)
        {
            string path = filePath;
            if (listenerContext.Request.Headers["Accept-Encoding"]?.Contains("gzip") == true)
            {
                var newPath = $"{path}.gz";
                if (System.IO.File.Exists(newPath))
                {
                    path = newPath;
                    listenerContext.Response.Headers.Add("Content-Encoding", "gzip");
                }
            }
            return path;
        }




        /// <summary>
        /// 弹窗跳转
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="alert">弹窗内容</param>
        /// <param name="JumpUrl">跳转链接地址</param>
        public static void AlertJump(this HttpListenerContext listenerContext, string alert, string JumpUrl)
        {
            string html = $"<!DOCTYPE html><html><body><script>alert('{alert}'); window.location.href = '{JumpUrl}' </script></body></html>";
            SendBytes(listenerContext, Encoding.Default.GetBytes(html), HttpContextType.Encoder(HttpContextType.Default_Html, Encoding.UTF8.WebName));
        }

        /// <summary>
        /// HSTS判断
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="includeSubDomains">includeSubDomains</param>
        /// <param name="maxage">maxage</param>
        /// <returns><para>true:本条为HTTPS安全链接</para><para>false:代表非安全连接，并且进行了HSTS处理，已处理本此的HTTP请求，无需继续处理；</para></returns>
        public static bool HSTS(this HttpListenerContext listenerContext, bool includeSubDomains = true, long maxage = 31536000)
        {
            try
            {
                if (listenerContext.Request.Url?.Scheme != Uri.UriSchemeHttps)
                {
                    var newurl = "https" + listenerContext.Request.Url!.AbsoluteUri.Remove(0, listenerContext.Request.Url.Scheme.Length);
                    listenerContext.Response.Headers.Add("Strict-Transport-Security", $"max-age={maxage};" + (includeSubDomains ? "includeSubDomains" : string.Empty));
                    listenerContext.Response.Headers.Add("location", newurl);
                    listenerContext.Response.StatusCode = 301;
                    listenerContext.Response.Close();
                    return false;
                }
                listenerContext.Response.Headers.Add("Strict-Transport-Security", $"max-age={maxage}; " + (includeSubDomains ? "includeSubDomains" : string.Empty));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 跨域问题
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="origin">默认全部</param>
        /// <returns></returns>
        public static HttpListenerContext CorsAllowOrigin(this HttpListenerContext listenerContext, string origin = "*")
        {
            listenerContext.Response.Headers.Add("Access-Control-Allow-Origin", origin);
            return listenerContext;
        }
        /// <summary>
        /// 跨域问题
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="methods">多个值请用，分割</param>
        /// <returns></returns>
        public static HttpListenerContext CorsAllowMethods(this HttpListenerContext listenerContext, string methods = "*")
        {
            listenerContext.Response.Headers.Add("Access-Control-Allow-Methods", methods);
            return listenerContext;
        }
        /// <summary>
        /// 跨域问题
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="headers">多个值请用，分割</param>
        /// <returns></returns>
        public static HttpListenerContext CorsAllowHeaders(this HttpListenerContext listenerContext, string headers = "*")
        {
            listenerContext.Response.Headers.Add("Access-Control-Allow-Headers", headers);
            return listenerContext;
        }

        /// <summary>
        /// 使用Http3
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <returns></returns>
        public static HttpListenerContext UseHttp3(this HttpListenerContext listenerContext)
        {
            listenerContext.Response.Headers.Add("AltSvc ", "h3=\":443\"");
            return listenerContext;
        }

        /// <summary>
        /// 使用 Sever-Sent-Event 服务
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="lastEventId">如果发生重连则返回 Last-Event-ID，否则为空</param>
        /// <returns></returns>
        public static HttpListenerContext UseSse(this HttpListenerContext listenerContext, out string? lastEventId)
        {
            lastEventId = listenerContext.Request.Headers["Last-Event-ID"]?.ToString();
            return listenerContext.UseSse();
        }

        /// <summary>
        /// 使用 Sever-Sent-Event 服务
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <returns></returns>
        public static HttpListenerContext UseSse(this HttpListenerContext listenerContext)
        {

            listenerContext.Response.Headers.Add("Connection", "keep-alive");
            listenerContext.Response.Headers.Add("Cache-Control", "no-cache");
            listenerContext.Response.ContentType = "text/event-stream";
            listenerContext.Response.StatusCode = 200;
            listenerContext.Response.KeepAlive = true;

            return listenerContext;
        }

        /// <summary>
        /// Server-sent-event发送数据
        /// </summary>
        /// <param name="listenerContext"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool SseSend(this HttpListenerContext listenerContext, HttpSSEModel data)
        {

            var bytes = Encoding.UTF8.GetBytes(data.ToString());

            try
            {
                listenerContext.Response.OutputStream.Write(bytes, 0, bytes.Length);
                listenerContext.Response.OutputStream.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }




        /// <summary>
        /// Url本地路径转换器
        /// </summary>
        /// <param name="url">Uri</param>
        /// <param name="baseLocalPath">本地路径基础地址</param>
        /// <param name="ignorePathSegment">忽略路径段，默认忽略第一阶级</param>
        /// <returns></returns>
        public static string LocalPathConverter(this Uri url, string baseLocalPath, int ignorePathSegment = 1)
        {
            return baseLocalPath+ string.Join(null, url.Segments.Skip(ignorePathSegment));
        }

        /// <summary>
        /// Url本地路径转换器，判断是否最后为‘/’，是则追加后缀
        /// </summary>
        /// <param name="url">Uri</param>
        /// <param name="baseLocalPath">本地路径基础地址</param>
        /// <param name="suffix">后缀</param>
        /// <param name="ignorePathSegment">忽略路径段，默认忽略第一阶级</param>
        /// <returns></returns>
        public static string LocalPathConverter(this Uri url, string baseLocalPath, string suffix, int ignorePathSegment = 1)
        {
            var newUrl = url.LocalPathConverter(baseLocalPath, ignorePathSegment);
            return newUrl.EndsWith("/") ? newUrl + suffix : newUrl;
        }

        /// <summary>
        /// 接收文件,协议接收。
        /// </summary>
        /// <param name="request">HttpListenerRequest</param>
        /// <param name="maxSize">最大尺寸</param>
        /// <returns>返回(文件大小,文件名称/消息传递)</returns>
        public static (byte[]? bytes, string? data) AgreementReviceFile(this HttpListenerRequest request, long maxSize = long.MaxValue)
        {
            try
            {
                if (request.ContentLength64 > maxSize)
                {
                    return (null, "超过最大限制");
                }

                using BufferedStream br = new BufferedStream(request.InputStream);
                using MemoryStream ms = new MemoryStream();
                byte[] buffer = new byte[4096];
                int len = 0;
                while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, len);
                    if (ms.Length > request.ContentLength64)
                    {
                        return (null, "文件过大");

                    }
                }
                var px = request.ContentType!.Split('=').Last();
                var startLength = request.ContentEncoding.GetBytes($"{px}\r\n").Length;
                byte[] reStart = ms.ToArray().Skip(startLength).ToArray();
                byte[] startEnd = request.ContentEncoding.GetBytes("\r\n\r\n");
                int startIndex = 0;
                for (int i = 0; i < reStart.Length; i++)
                {
                    if (reStart[i] == startEnd[0])
                    {
                        int pk = 1;
                        for (int j = 1; j < startEnd.Length; j++)
                        {
                            try
                            {
                                if (reStart[i + j] == startEnd[j])
                                {
                                    pk++;
                                }
                            }
                            catch
                            {
                            }
                        }
                        if (pk == startEnd.Length)
                        {
                            startIndex = i;
                            break;
                        }
                    }

                }
                var filename = "";
                var bc = new Regex(@"filename="".*""", RegexOptions.ECMAScript);
                var isMach = bc.Match(request.ContentEncoding.GetString(reStart.Take(startIndex).ToArray()));
                if (isMach.Success)
                {
                    filename = isMach.Value.TrimStart("filename=\"".ToCharArray()).TrimEnd('"');
                }
                var two = reStart.Skip(startIndex + startEnd.Length);
                var endLength = request.ContentEncoding.GetBytes($"\r\n\r\n{px}--\r\n").Length;
                var result = two.Take(two.Count() - endLength).ToArray();

                return (result, filename);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// 接收文件,纯文件接收
        /// </summary>
        /// <param name="request">HttpListenerRequest</param>
        /// <param name="maxSize">最大尺寸</param>
        /// <returns>返回(文件大小,消息传递)</returns>
        public static (byte[]? bytes, string? msg) ReviceFile(this HttpListenerRequest request, long maxSize = long.MaxValue)
        {
            try
            {
                if (request.ContentLength64 > maxSize)
                {
                    return (null, "超过最大限制");
                }
                using BufferedStream br = new BufferedStream(request.InputStream);
                using MemoryStream ms = new MemoryStream();
                byte[] buffer = new byte[4096];
                int len = 0;
                while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, len);
                    if (ms.Length > maxSize)
                    {
                        return (null, "文件过大");
                    }
                }
                return (ms.ToArray(), null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// 获取断点值
        /// </summary>
        /// <param name="listenerContext">HttpListenerContext</param>
        /// <param name="fileInfo">文件信息</param>
        /// <returns></returns>
        private static (long? startIndex, long? endIndex) GetRangeHeader(this HttpListenerContext listenerContext, FileInfo fileInfo)
        {
            try
            {
                var gtTime = LocalToGMT(fileInfo.LastWriteTime);
                //判断是否有缓存时间
                var If_Modified_Since = listenerContext.Request.Headers.GetValues("If-Range");
                if (If_Modified_Since != null && If_Modified_Since.Any() && If_Modified_Since.First() == gtTime)
                    return (null, null);

                //判断值
                var ranges = listenerContext.Request.Headers.GetValues("Range");
                if (ranges == null || !ranges.Any())
                    return (null, null);


                var range = ranges.First().Trim();

                if (!range.StartsWith("bytes="))
                    return (null, null);

                var timeRange = range.Skip(6).ToString().Split('-');
                if (timeRange.Length != 2)
                    return (null, null);

                //返回结果
                long? start = null, end = null;
                if (long.TryParse(timeRange[0], out long _start)) start = _start;
                if (long.TryParse(timeRange[1], out long _end)) end = _end;

                return (start, end);
            }
            catch
            {
                return (null, null);

            }
        }


        /// <summary>
        /// 读取请求String,默认utf-8编码
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string ReadString(this HttpListenerRequest request)
        {
            return ReadString(request, Encoding.UTF8);
        }


        /// <summary>
        /// 读取请求String
        /// </summary>
        /// <param name="request"></param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string ReadString(this HttpListenerRequest request, Encoding encoding)
        {
            using StreamReader reader = new StreamReader(request.InputStream, encoding);
            return reader.ReadToEnd();
        }


        /// <summary>
        /// 读取请求为Form数据
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static NameValueCollection ReadForm(this HttpListenerRequest request)
        {
            return request.ReadForm(Encoding.UTF8);
        }

        /// <summary>
        /// 读取请求为Form数据
        /// </summary>
        /// <param name="request"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static NameValueCollection ReadForm(this HttpListenerRequest request, Encoding encoding)
        {
            return HttpUtility.ParseQueryString(request.ReadString(encoding));
        }


        /// <summary>
        /// 读取为JsonNode
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static JsonNode? ReadJsonNode(this HttpListenerRequest request)
        {
            return request.ReadJsonNode(Encoding.UTF8);
        }

        /// <summary>
        /// 读取为JsonNode
        /// </summary>
        /// <param name="request"></param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static JsonNode? ReadJsonNode(this HttpListenerRequest request, Encoding encoding)
        {
            try
            {
                return JsonNode.Parse(ReadString(request, encoding));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 读取并序列为为T
        /// </summary>
        /// <typeparam name="T">Object</typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        public static T? ReadJsonDeserialize<T>(this HttpListenerRequest request)
        {
            return request.ReadJsonDeserialize<T>(JsonHelper.JSOpentions, Encoding.UTF8);
        }
        /// <summary>
        /// 读取并序列为为T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="options">序列化参数</param>
        /// <returns></returns>
        public static T? ReadJsonDeserialize<T>(this HttpListenerRequest request, JsonSerializerOptions options)
        {
            return request.ReadJsonDeserialize<T>(options, Encoding.UTF8);
        }
        /// <summary>
        /// 读取并序列为为T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="options">序列化参数</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static T? ReadJsonDeserialize<T>(this HttpListenerRequest request, JsonSerializerOptions options, Encoding encoding)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(ReadString(request, encoding), options);

            }
            catch
            {

                return default;
            }
        }

        #endregion


        #region OtherHelper

        /// <summary>
        /// Win系统开启Http3支持。需要系统版本大于等于 Windows Server 2022 or Win11。
        /// <para>注册表方式开启，第一次使用完此方法后，可能需要重启系统。</para>
        /// <para>https://techcommunity.microsoft.com/t5/networking-blog/enabling-http-3-support-on-windows-server-2022/ba-p/2676880</para>
        /// <para>开启后，仍然需要在httpBuilder开启支持http3的选项</para>
        /// </summary>
        public static void WinEnableHttp3()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var mreg = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\HTTP\Parameters", RegistryKeyPermissionCheck.ReadWriteSubTree);
                mreg.SetValue("EnableHttp3", 1);
                mreg.SetValue("EnableAltSvc", 1);
            }
        }
        /// <summary>
        /// 关闭Http.sys的将服务器标头追加到响应。
        /// <para>注册表方式开启，第一次使用完此方法后，可能需要重启系统。</para>
        /// </summary>
        public static void CloseHttpServerNameDefault()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var mreg = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\HTTP\Parameters", RegistryKeyPermissionCheck.ReadWriteSubTree);
                mreg.SetValue("DisableServerHeader", 2);
            }
        }

        /// <summary>
        /// 转换成GMT时间
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string LocalToGMT(DateTime dt)
        {
            return dt.ToUniversalTime().ToString("r");
        }

        /// <summary>
        /// GMT转换成DateTime
        /// </summary>
        /// <param name="gmt"></param>
        /// <returns></returns>
        public static DateTime GMTToLocal(string gmt)
        {
            DateTime dt = DateTime.MinValue;
            try
            {
                string pattern = "";
                if (gmt.IndexOf("+0") != -1)
                {
                    gmt = gmt.Replace("GMT", "");
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
                }
                if (gmt.ToUpper().IndexOf("GMT") != -1)
                {
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
                }
                if (pattern != "")
                {
                    dt = DateTime.ParseExact(gmt, pattern, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    dt = dt.ToLocalTime();
                }
                else
                {
                    dt = Convert.ToDateTime(gmt);
                }
            }
            catch
            {
            }
            return dt;
        }

        /// <summary>
        /// 是否为相对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsRelativePath(string path)
        {
            return Regex.IsMatch(path, @"\\\.{1,2}\\|\\\.{1,2}\/|\/\.{1,2}\/|\/\.{1,2}\\|^\.|^~");
        }

        #endregion

    }
}