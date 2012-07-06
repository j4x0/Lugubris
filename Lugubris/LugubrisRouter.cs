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
using SimpleJson;

namespace Lugubris
{
    public struct LugubrisRoute
    {
        public string from;
        public string to;

        public LugubrisRoute(string from, string to)
        {
            this.to = to;
            this.from = from;
        }
    }

    public class LugubrisRouter
    {
        public List<LugubrisRoute> Routes { get; private set; }

        public string TranslateUrl(string url)
        {
            string translated = url;
            foreach (var route in this.Routes)
                if (route.from == url)
                    translated = route.to;
            return translated;
        }

        public void DeleteRoute(string from)
        {
            LugubrisRoute target = default(LugubrisRoute);
            foreach (var route in this.Routes)
                if (route.from == from) target = route;
            if (target.to == null)
                throw new Exception("There is no route registered from " + from);
            else
                this.Routes.Remove(target);
        }

        public bool HasRoute(string from)
        {
            foreach (var route in this.Routes)
                if (route.from == from) return true;
            return false;
        }

        public void AddRoute(string from, string to)
        {
            if (this.HasRoute(from))
                throw new Exception("There is already a route registered from " + from);
            this.Routes.Add(new LugubrisRoute(from, to));
        }

        public void Save(string path)
        {
            var stream = File.OpenWrite(path);
            byte[] json = UTF8Encoding.UTF8.GetBytes(this.ToJson());
            stream.Write(json, 0, json.Length);
            stream.Close();
        }

        public string ToJson()
        {
            return SimpleJson.SimpleJson.SerializeObject(this.Routes.ToDictionary((route) => route.from, (route) => route.to));
        }

        public static LugubrisRouter Parse(string json)
        {
            var routesRaw = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(json);
            var routes = new List<LugubrisRoute>();
            var existingRoutes = new List<string>();
            foreach (var route in routesRaw)
            {
                if (existingRoutes.Contains(route.Key))
                    throw new Exception("There is already a route registered from " + route.Key);
                existingRoutes.Add(route.Key);
                routes.Add(new LugubrisRoute(route.Key, (string)route.Value));
            }
            return new LugubrisRouter
            {
                Routes = routes
            };
        }
    }
}
