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
using System.IO.Compression;
using System.IO;

namespace Lugubris.ContentEncoders
{
    public class DeflateEncoder : IContentEncoder
    {
        public byte[] Encode(byte[] data)
        {
            var output = new MemoryStream();
            var gzipstream = new DeflateStream(output, CompressionMode.Compress);
            gzipstream.Write(data, 0, data.Length);
            gzipstream.Close();
            byte[] compressed = output.ToArray();
            output.Close();
            return compressed;
        }

        public string GetName()
        {
            return "deflate";
        }
    }
}
