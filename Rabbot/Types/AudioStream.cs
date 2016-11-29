using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot.Types
{
    class AudioStream
    {
        public string Meta { get; set; } = "";
        public MemoryStream Buffer;

        public AudioStream(MemoryStream buffer)
        {
            Buffer = buffer;
        }
    }
}
