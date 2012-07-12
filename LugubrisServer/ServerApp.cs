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
using Lugubris;

namespace Lugubris.Cli
{
    class ServerApp
    {
        public static readonly string VERSION = "0.1.1-a";
        public static readonly string GITHUB = "http://www.github.com/JacoRuit/Lugubris";

        static int Main(string[] args)
        {
            Console.WriteLine("Lugubris Server version {0}", ServerApp.VERSION);
            Console.WriteLine("Lugubris is licensed under GNU GPL version 3 <{0}>", "http://www.gnu.org/copyleft/gpl.html");
            Console.WriteLine("Github: {0}", ServerApp.GITHUB);
            string configPath = @"config.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine("config.json wasn't found");
                configPath = ServerApp.GetConfigPath();
            }
            var data = File.ReadAllText(configPath);
            LugubrisConfig config;
            try
            {
                config = LugubrisConfig.Parse(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
                return -1;
            }
            var server = new LugubrisServer(config);
            Console.WriteLine("Starting server....");
            try
            {
                server.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
                return -1;
            }
            Console.WriteLine("Server listening on port {0}", server.Configuration.Port);
            Console.WriteLine("Press enter to stop the server");
            Console.ReadLine();
            try
            {
                server.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return 0;
        }

        private static string GetConfigPath()
        {
            Console.WriteLine("Please provide a path to the config file");
            var path = Console.ReadLine();
            if (!File.Exists(path))
            {
                Console.WriteLine("The config file doesn't exist");
                return ServerApp.GetConfigPath();
            }
            else return path;
        }
    }
}
