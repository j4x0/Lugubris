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
using System.IO;
using Lugubris.ContentEncoders;
using Lugubris.SessionManagers;
using SimpleJson;

namespace Lugubris
{
    public class LugubrisConfig
    {
        public int Port { get; private set; }
        public string RootPath { get; private set; }
        public bool DebugMode { get; private set; }
        public string LibraryPath { get; private set; }
        public string RoutesPath { get; private set; }
        public IContentEncoder ContentEncoder { get; private set; }
        public LugubrisRouter Router { get; private set; }
        public ISessionManager SessionManager { get; private set; }
        public InternetMediaTypes MediaTypes { get; private set; }

        public static LugubrisConfig Parse(string json)
        {
            var data = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(json);
            var sessionManagerData = (IDictionary<string, object>)data["session_manager"];
            var smt = (string)sessionManagerData["type"];
            ISessionManager manager = null;
            switch (smt)
            {
                case "filesystem":
                    manager = new FileSystemSessionManager((string)sessionManagerData["save_path"]);
                    break;
                default:
                    throw new Exception("Unknown session manager type: " + smt);
            }
            var en = (string)data["content_encoder"];
            IContentEncoder encoder = null;
            switch (en)
            {
                case "gzip":
                    encoder = new GzipEncoder();
                    break;
                case "deflate":
                    encoder = new DeflateEncoder();
                    break;
                case "none":
                    break;
                default:
                    throw new Exception("Unknown content encoder: " + en);
            }
            var paths = (IDictionary<string, object>)data["paths"];
            var routesPath = (string)paths["routes"];
            var mediaTypesPath = (string)paths["media_types"];
            return new LugubrisConfig
            {
                Port = Convert.ToInt32(data["port"]),
                RootPath = (string)paths["htdocs"],
                LibraryPath = (string)paths["lib"],
                DebugMode = Convert.ToBoolean(data["debug_mode"]),
                SessionManager = manager,
                RoutesPath = routesPath,
                Router = LugubrisRouter.Parse(File.ReadAllText(routesPath)),
                MediaTypes = InternetMediaTypes.Parse(File.ReadAllText(mediaTypesPath)),
                ContentEncoder = encoder
            };
        }
    }
}
