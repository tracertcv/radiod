using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rabbot
{
    //This isn't an interface because the reflections API is a whiny bitch.
    interface IAudioModule
    {
        void onAudioStarted(object sender, EventArgs e);
        void onAudioStopped(object sender, EventArgs e);
        void onAudioPaused(object sender, EventArgs e);
        void onAudioUnpaused(object sender, EventArgs e);
        void onAudioEndOfStream(object sender, EventArgs e);
        string getUID();
    }
}
