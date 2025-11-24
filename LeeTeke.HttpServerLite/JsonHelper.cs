using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace LeeTeke.HttpServerLite
{
    /// <summary>
    /// 小小的json帮助方法
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 反序列化忽略大小写
        /// </summary>
        public static readonly JsonSerializerOptions JSOpentions = new() { Property​Name​Case​Insensitive = true };//默认参数
        /// <summary>
        /// 序列化的时候首字母小写
        /// </summary>
        public static readonly JsonSerializerOptions JSOCamel = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        /// <summary>
        /// 序列化的时候首字母小写且不带空值
        /// </summary>
        public static readonly JsonSerializerOptions JSOCamelNotNull = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        /// <summary>
        /// 序列化的时候不带空值
        /// </summary>
        public static readonly JsonSerializerOptions JSONotNull = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        /// <summary>
        /// 序列化的时候编码类型未全部
        /// </summary>
        public static readonly JsonSerializerOptions JSOUnicodeRangesAll = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };


       
        /// <summary>
        /// /*默认忽略大小写*/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static T? Deserialize<T>(string? jsonString, JsonSerializerOptions? options = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString))
                    return default;
                return JsonSerializer.Deserialize<T>(jsonString!, options ?? JSOpentions);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 转成Byte
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static byte[]? SerializeToBytes(object data, JsonSerializerOptions? options = null)
        {
            try
            {
                return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, options));
            }
            catch (Exception)
            {
                return null;
            }

        }


        /// <summary>
        /// tojson
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string SerializeToStr(object data, JsonSerializerOptions? options = null)
        {
            try
            {
                return JsonSerializer.Serialize(data, options);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 检查字符串
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool StrIsNullOrWhiteSpace(params string?[] data)
        {
            foreach (var item in data)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    return true;
                }
            }
            return false;
        }


    }
}
