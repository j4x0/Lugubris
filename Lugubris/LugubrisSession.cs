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
using SimpleJson;

namespace Lugubris
{
    public class LugubrisSession 
    {
        public IDictionary<string, object> Data { get; private set; }

        public LugubrisSession(IDictionary<string, object> data)
        {
            this.Data = data;
        }

        public string ToJson()
        {
            return SimpleJson.SimpleJson.SerializeObject(Data);
        }

        public static LugubrisSession Parse(string json)
        {
            return new LugubrisSession((IDictionary<string,object>)SimpleJson.SimpleJson.DeserializeObject(json));
        }
    }
}
