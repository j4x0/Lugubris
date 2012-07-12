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
using System.Reflection;

namespace Lugubris.HtmlTemplates
{
    public class MessagePage
    {
        public enum MessageType
        {
            Error,
            Information,
            Warning
        };

        private static string template;

        public static string GetHtml(MessageType type, string title, string header, string inner)
        {
            if (MessagePage.template == null)
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Lugubris.HtmlTemplates.MessageTemplate.html");
                var reader = new StreamReader(stream);
                MessagePage.template = reader.ReadToEnd();
                reader.Close();
                stream.Close();
            }
            string headerColor = null;
            switch (type)
            {
                case MessageType.Error:
                    headerColor = "#d30c0c";
                    break;
                case MessageType.Information:
                    headerColor = "#42bbd7";
                    break;
                case MessageType.Warning:
                    headerColor = "#f6f242";
                    break;
            }
            return String.Format(MessagePage.template, title, header, inner, headerColor);
        }
    }
}
