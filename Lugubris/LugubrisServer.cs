/**
 * Lugubris
 * Copyright (C) 2012  Jaco Ruit
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting; 
using IronPython.Hosting;
using IronPython.Runtime;
using System.IO;

namespace Lugubris
{
    public class LugubrisServer
    {
        public LugubrisConfig Configuration { get; private set; }
        private HttpListener listener;

        public LugubrisServer(LugubrisConfig config)
        {
            this.Configuration = config;
            this.listener = new HttpListener();
            listener.Prefixes.Add("http://+:" + config.Port + "/");
        }

        public void Start()
        {
            this.listener.Start();
            this.Listen();
        }

        private void Listen()
        {
            if (!listener.IsListening)
                throw new Exception("Server isn't listening!");
            var result = listener.BeginGetContext(new AsyncCallback(this.HandleRequest), listener);
            result.AsyncWaitHandle.WaitOne();
        }

        public void Stop()
        {
            try
            {
                this.listener.Stop();
            }
            catch { }
        }

        private void PrepareScope(ScriptScope scope, HttpListenerRequest request, HttpListenerResponse response, StringBuilder responseBuffer)
        {
            scope.SetVariable("echo", new Action<string>((string str) => responseBuffer.Append(str)));
            var requestData = new Dictionary<string,string>();
            requestData.Add("contenttype", request.ContentType);
            requestData.Add("useragent", request.UserAgent);
            scope.SetVariable("__request__", requestData);
            scope.SetVariable("add_route", new Action<string, string>((from, to) => 
                {
                    this.Configuration.Router.AddRoute(from,to);
                }
                ));
            scope.SetVariable("delete_route", new Action<string>((from) =>
                {
                    this.Configuration.Router.DeleteRoute(from);
                }
                ));
            scope.SetVariable("save_routes", new Action(() => this.Configuration.Router.Save(this.Configuration.RoutesPath)));
            scope.SetVariable("set_header", new Action<string, object>((string field, object value) => 
                {
                    switch(field)
                    {
                        default:
                            break;
                    }
                }
                ));
            scope.SetVariable("get_session", new Func<LugubrisSession>(() =>
                {
                    var cookie = request.Cookies["LUGUBRISSESS"];
                    if (cookie == null)
                        throw new Exception("No session found");
                    return this.Configuration.SessionManager.GetSession(cookie.Value);
                }
            ));
        }

        private void HandleRequest(IAsyncResult result)
        {
            var listener = (HttpListener)result.AsyncState;
            var context = listener.EndGetContext(result);
            var request = context.Request;
            var response = context.Response;
            response.Headers.Add("Server: Lugubris");
            var translated = this.Configuration.Router.TranslateUrl(request.Url.AbsolutePath);
            var path = this.Configuration.RootPath +  translated;
            byte[] buffer = new byte[0];
            if (!File.Exists(path))
            {
                response.StatusCode = 404;
                if (this.Configuration.DebugMode)
                {
                }
            }    
            else if (path.EndsWith(".py"))
            {
                StringBuilder responseBuffer = new StringBuilder();
                var engine = Python.CreateEngine();
                var scope = engine.CreateScope();
                var searchPaths = engine.GetSearchPaths();
                searchPaths.Add(this.Configuration.LibraryPath);
                engine.SetSearchPaths(searchPaths);
                this.PrepareScope(scope, request, response, responseBuffer);
                try
                {
                    engine.ExecuteFile(path, scope);
                }
                catch (UnboundNameException e)
                {
                    response.StatusCode = 503;
                    ExceptionOperations eo = engine.GetService<ExceptionOperations>();
                    if (this.Configuration.DebugMode)
                    {
                    }
                    Console.WriteLine(eo.FormatException(e));
                }
                catch (SyntaxErrorException e)
                {
                    response.StatusCode = 503;
                    ExceptionOperations eo = engine.GetService<ExceptionOperations>();
                    if (this.Configuration.DebugMode)
                    {
                    }
                    Console.WriteLine(eo.FormatException(e));
                }
                catch (Exception e)
                {
                    response.StatusCode = 503;
                    if (this.Configuration.DebugMode)
                    {
                    }
                    Console.WriteLine(e.Message);
                }
                buffer = UTF8Encoding.UTF8.GetBytes(responseBuffer.ToString());
            }
            else
            {
                buffer = File.ReadAllBytes(path);
            }
            if (buffer.Length > 0)
                response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
            this.Listen();
        }

    }
}
