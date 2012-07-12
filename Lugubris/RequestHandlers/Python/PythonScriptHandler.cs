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
using System.Reflection;
using System.IO;
using Lugubris;
using Lugubris.HtmlTemplates;

namespace Lugubris.RequestHandlers
{
    public class PythonScriptHandler : IRequestHandler
    {
        private LugubrisConfig serverConfiguration;
        private ServerInfo serverInfo;

        public PythonScriptHandler(LugubrisConfig serverConfiguration, ServerInfo serverInfo)
        {
            this.serverConfiguration = serverConfiguration;
            this.serverInfo = serverInfo;
        }

        private void PrepareScope(ScriptScope scope, HttpListenerContext context, StringBuilder responseBuffer)
        {
            scope.SetVariable("echo", new Action<object>((object str) => 
                responseBuffer.Append(str == null ? "null" : str.ToString())
                ));
            var requestData = new Dictionary<string, string>();
            foreach (var headerName in context.Request.Headers.AllKeys)
            {
                foreach (var headerValue in context.Request.Headers.GetValues(headerName))
                {
                    if (requestData.ContainsKey(headerName.ToUpper()))
                        requestData[headerName.ToUpper()] += headerValue;
                    else
                        requestData[headerName.ToUpper()] = headerValue;
                }
            }
            var serverinf = new Dictionary<string, object>();
            serverinf.Add("NAME", "Lugubris");
            serverinf.Add("VERSION", LugubrisServer.VERSION);
            serverinf.Add("BYTES_OUT", this.serverInfo.BytesOut);
            serverinf.Add("BYTES_IN", this.serverInfo.BytesOut);
            serverinf.Add("REQUESTS_HANDLED", this.serverInfo.RequestsHandled);
            scope.SetVariable("SERVER", serverinf);
            scope.SetVariable("REQUEST", requestData);
            scope.SetVariable("add_route", new Action<string, string>((from, to) =>
                {
                    this.serverConfiguration.Router.AddRoute(from, to);
                }
                ));
            scope.SetVariable("delete_route", new Action<string>((from) =>
                {
                    this.serverConfiguration.Router.DeleteRoute(from);
                }
                ));
            scope.SetVariable("save_routes", new Action(() => this.serverConfiguration.Router.Save(this.serverConfiguration.RoutesPath)));
            scope.SetVariable("set_header", new Action<string, string>((string field, string value) =>
                    context.Response.AddHeader(field, value)
                ));
            scope.SetVariable("get_session", new Func<string, object>((string field) =>
                {
                    var cookie = context.Request.Cookies["LUGUBRISSESS"];
                    LugubrisSession session;
                    if (cookie != null && !this.serverConfiguration.SessionManager.SessionExists(cookie.Value))
                        cookie = null;
                    if (cookie == null)
                    {
                        session = new LugubrisSession();
                        var newCookie = new Cookie(
                            "LUGUBRISSESS",
                            this.serverConfiguration.SessionManager.CreateSaveSession(session)
                            );
                        context.Response.Cookies.Add(newCookie);
                        return null;
                    }

                    return this.serverConfiguration.SessionManager.GetSession(cookie.Value).Data.GetOr(field,() => "");
                }
            ));
            scope.SetVariable("set_session", new Action<string, object>((string field, object value) =>
                {
                    var cookie = context.Request.Cookies["LUGUBRISSESS"];
                    LugubrisSession session;
                    if (cookie == null)
                    {
                        var data = new Dictionary<string, object>();
                        data[field] = value;
                        session = new LugubrisSession(data);
                        var newCookie = new Cookie(
                            "LUGUBRISSESS",
                            this.serverConfiguration.SessionManager.CreateSaveSession(session)
                            );
                        context.Response.Cookies.Add(newCookie);
                        return;
                    }
                    session = this.serverConfiguration.SessionManager.GetSession(cookie.Value);
                    session.Data[field] = value;
                    this.serverConfiguration.SessionManager.SaveSession(cookie.Value, session);
                }
            ));
        }

        public byte[] Handle(string requestUri, HttpListenerContext context)
        {
            StringBuilder responseBuffer = new StringBuilder();
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            var searchPaths = engine.GetSearchPaths();
            searchPaths.Add(this.serverConfiguration.LibraryPath);
            engine.SetSearchPaths(searchPaths);
            this.PrepareScope(scope, context, responseBuffer);
            var lastfew = "";
            var fileContents = File.ReadAllText(requestUri).ToCharArray();
            var pythonBuffer = new StringBuilder();
            bool inPython = false;
            try
            {
                foreach (var chr in fileContents)
                {
                    lastfew += chr;
                    if (lastfew.EndsWith("<?py") && !inPython)
                    {
                        inPython = true;
                        pythonBuffer.Clear();
                        responseBuffer = responseBuffer.Remove(responseBuffer.Length - 3, 3);
                    }
                    else if (lastfew.EndsWith("?>") && inPython)
                    {
                        inPython = false;
                        pythonBuffer = pythonBuffer.Remove(pythonBuffer.Length - 2, 2);
                        engine.Execute(pythonBuffer.ToString().Trim(), scope);
                    }
                    else if (inPython)
                    {
                        pythonBuffer.Append(chr);
                    }
                    else
                    {
                        responseBuffer.Append(chr);
                    }
                    if (lastfew.Length == 5)
                        lastfew = lastfew.Substring(2, 3);
                }
            }
            catch (UnboundNameException e)
            {
                context.Response.StatusCode = 503;
                ExceptionOperations eo = engine.GetService<ExceptionOperations>();
                if (this.serverConfiguration.DebugMode)
                {
                    responseBuffer.Clear();
                    responseBuffer.Append(
                        MessagePage.GetHtml(
                        MessagePage.MessageType.Error,
                        "Lugubris - Error",
                        "UnboundNamException",
                        "<pre>" + eo.FormatException(e).Replace("File \"\"", "File \"" + requestUri + "\"") + "</pre>"
                        ));
                }
                Console.WriteLine(eo.FormatException(e));
            }
            catch (SyntaxErrorException e)
            {
                context.Response.StatusCode = 503;
                ExceptionOperations eo = engine.GetService<ExceptionOperations>();
                if (this.serverConfiguration.DebugMode)
                {
                    responseBuffer.Clear();
                    responseBuffer.Append(
                        MessagePage.GetHtml(
                        MessagePage.MessageType.Error,
                        "Lugubris - Error",
                        "SyntaxErrorException",
                        "<pre>" + eo.FormatException(e).Replace("File \"\"", "File \"" + requestUri + "\"") + "</pre>"
                        ));
                }
                Console.WriteLine(eo.FormatException(e));
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 503;
                if (this.serverConfiguration.DebugMode)
                {
                    responseBuffer.Clear();
                    responseBuffer.Append(
                        MessagePage.GetHtml(
                        MessagePage.MessageType.Error,
                        "Lugubris - Error",
                        e.GetType().Name,
                        "Message:<br/><pre>" + e.Message + "</pre>"
                        ));
                }
                Console.WriteLine(e.Message);
            }
            return UTF8Encoding.UTF8.GetBytes(responseBuffer.ToString());
        }

        public bool DoesHandle(string requestUri)
        {
            return requestUri.EndsWith(".py");
        }
    }
}
