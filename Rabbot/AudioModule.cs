using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot
{
    //This isn't an interface because the reflections API is a whiny bitch.
    class AudioModule
    {

        private string UID;
        
        public virtual void onAudioStarted()
        {

        }
        public virtual void onAudioStopped()
        {

        }
        public virtual void onAudioPaused()
        {

        }
        public virtual void onAudioUnpaused()
        {

        }
        public virtual void onEndOfStream()
        {

        }
        public virtual string getUID()
        {
            return this.UID;
        }
    }
}
