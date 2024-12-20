﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace LeeTeke.HttpServerLite
{
    internal class HttpRoute
    {
        public MethodInfo Method { get; }

        public object Parent { get; }

        public Action<HttpListenerContext, Action<object[]?>> Next { get; }


        private int _parametersNum = 0;//参数
        public HttpRoute(MethodInfo method, object parent, Action<HttpListenerContext, Action<object[]?>> next)
        {
            Method = method;
            Parent = parent;
            Next = next;
            _parametersNum = method.GetParameters().Length;
        }

        public void Invoke(HttpListenerContext context)
        {
            Next(context, objs =>
            {
                if (objs != null)
                {
                    var parms = new object[1 + objs.Length];
                    parms[0] = context;
                    for (int i = 0; i < objs.Length; i++)
                    {
                        parms[i + 1] = objs[i];
                    }
                    MethodInvoke(context, parms);
                }
                else
                {

                    MethodInvoke(context, new object[] { context });
                }
            });
        }

        private void MethodInvoke(HttpListenerContext context, object[] @params)
        {
            if (_parametersNum == 0)
            {
                context.Close(HttpStatusCode.OK);
                MethodDo(context, null);
            }
            else if (_parametersNum == @params.Length)
            {
                MethodDo(context, @params);
            }
            else if (_parametersNum < @params.Length)
            {
                MethodDo(context, @params.Take(_parametersNum).ToArray());
            }
            else
            {
                if (Parent is HttpControllerBase hb)
                {
                    hb.RaiseException(context, new NotImplementedException());
                }
                else
                {
                    context.Close(HttpStatusCode.NotImplemented);
                }
            }

        }

        private void MethodDo(HttpListenerContext context, object?[]? @params)
        {

            if (Method.ReturnType.Name == nameof(Task))
            {
                ((Task)Method.Invoke(Parent, @params)!).ContinueWith(t =>
                  {
                      if (t.IsFaulted)
                      {
                          if (Parent is HttpControllerBase hb)
                          {
                              hb.RaiseException(context, t.Exception);
                          }
                          else
                          {
                              context.Abort();
                          }
                      }
                  }, TaskContinuationOptions.OnlyOnFaulted).ConfigureAwait(false);
            }
            else
            {
               
                    Method.Invoke(Parent, @params);
            }


        }
    }
}
