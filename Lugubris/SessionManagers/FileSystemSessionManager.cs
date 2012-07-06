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
using System.Security.Cryptography;

namespace Lugubris
{
    public class FileSystemSessionManager : ISessionManager
    {
        private string path;
        private SHA512 encrypter;

        public FileSystemSessionManager(string path)
        {
            this.path = path;
            this.encrypter = new SHA512Managed();
        }

        public LugubrisSession GetSession(string sessionId)
        {
            if (!this.SessionExists(sessionId))
                throw new Exception("Session doesn't exist");
            return LugubrisSession.Parse(File.ReadAllText(this.GetSessionPath(sessionId)));
        }

        public string CreateSaveSession(LugubrisSession session)
        {
            var id = this.GenerateId();
            this.SaveSession(id, session);
            return id;
        }

        public void SaveSession(string sessionId, LugubrisSession session)
        {
            var stream = File.OpenWrite(this.GetSessionPath(sessionId));
            byte[] json = UTF8Encoding.UTF8.GetBytes(session.ToJson());
            stream.Write(json, 0, json.Length);
            stream.Close();
        }

        public void DeleteSession(string sessionId)
        {
            if (!this.SessionExists(sessionId))
                throw new Exception("Session with id " + sessionId + " doesn't exist");
            File.Delete(this.GetSessionPath(sessionId));
        }

        public bool SessionExists(string sessionId)
        {
            return File.Exists(this.GetSessionPath(sessionId));
        }

        private string GetSessionPath(string sessionId)
        {
            return this.path + "\\ls_" + sessionId;
        }

        private string GenerateId()
        {
            var byteBuffer = new MemoryStream();
            var writer = new BinaryWriter(byteBuffer);
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                byte[] buffer = new byte[1024];
                random.NextBytes(buffer);
                writer.Write(buffer, 0, buffer.Length);
            }
            byte[] date = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
            writer.Write(date, 0, date.Length);
            byte[] hash = encrypter.ComputeHash(byteBuffer);
            var utf8hash = UTF8Encoding.UTF8.GetString(hash, 0, hash.Length);
            if (this.SessionExists(utf8hash))
                return this.GenerateId();
            else return utf8hash;
        }
    }
}
