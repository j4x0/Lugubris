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
using System.IO;
using Lugubris.HtmlTemplates;
using Lugubris.RequestHandlers;

namespace Lugubris
{
    public class LugubrisServer
    {
        public static readonly string VERSION = "1.0.3-a";

        public LugubrisConfig Configuration { get; private set; }
        public ServerInfo Info { get; private set; }
        private List<IRequestHandler> requestHandlers;
        private HttpListener listener;

        public LugubrisServer(LugubrisConfig config)
        {
            this.Configuration = config;
            this.listener = new HttpListener();
            listener.Prefixes.Add("http://+:" + config.Port + "/");
            this.requestHandlers = new List<IRequestHandler>();
            this.Info = new ServerInfo();
            this.AddRequestHandler(new PythonScriptHandler(this.Configuration, this.Info));
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

        public void AddRequestHandler(IRequestHandler handler)
        {
            if (this.requestHandlers.Contains(handler))
                throw new Exception("This handler is already registered");
            this.requestHandlers.Add(handler);
        }

        private IRequestHandler GetRequestHandler(string requestUrl)
        {
            foreach (var handler in this.requestHandlers)
                if (handler.DoesHandle(requestUrl)) return handler;
            return null;
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
            if (File.Exists(path))
            {
                var handler = this.GetRequestHandler(path);
                if (handler == null)
                {
                    var extension = path.Reverse().Split('.').First().Reverse();
                    var ctype = this.Configuration.MediaTypes.GetMediaType(extension);
                    if (ctype != null)
                        response.ContentType = ctype;
                    buffer = File.ReadAllBytes(path);
                }
                else
                    buffer = handler.Handle(path, context);
            }
            else
            {
                response.StatusCode = 404;
                if (this.Configuration.DebugMode)
                {
                    var html = MessagePage.GetHtml(
                        MessagePage.MessageType.Warning,
                        "Lugubris - Not Found",
                        "404 Not Found",
                        "The server has not found anything matching the Request-URI"
                        );
                    buffer = UTF8Encoding.UTF8.GetBytes(html);
                }
            }
            var encoderName = this.Configuration.ContentEncoder.GetName();
            if (this.Configuration.ContentEncoder != null && request.Headers["Accept-Encoding"].Contains(encoderName))
            {
                buffer = this.Configuration.ContentEncoder.Encode(buffer);
                response.AddHeader("Content-Encoding", encoderName);
            }
            response.ContentLength64 = buffer.Length;
            this.Info.BytesOut += buffer.Length;
            response.AddHeader("Date", DateTime.Now.ToUniversalTime().ToString());
            if (buffer.Length > 0)
                response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
            this.Info.RequestsHandled++;
            this.Listen();
        }

    }
}
