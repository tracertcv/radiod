using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Rabbot.Types
{
    public class Configuration
    {
        public char Prefix { get; set; }

        public string Token { get; set; }

        public Configuration()
        {
            Prefix = '!';
            Token = "";
        }

        public void SaveFile(string loc)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            if (!File.Exists(loc))
                File.Create(loc).Close();

            File.WriteAllText(loc, json);
        }

        public static Configuration LoadFile(string loc)
        {
            string json = File.ReadAllText(loc);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }
    }
}
