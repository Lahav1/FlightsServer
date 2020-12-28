using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlightsServer.Models
{
    class DatabaseUtils
    {
        public static string ReadFile(string path)
        {
            StreamReader sr = new StreamReader(path);
            return sr.ReadToEnd();
        }
    }
}
