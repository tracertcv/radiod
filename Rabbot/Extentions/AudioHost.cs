using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace Rabbot.Extentions
{
    class AudioStream
    {

        public bool stop { get; set; } = false;
        public bool pause { get; set; } = false;

        public string _name { get; set; } = "";
        public byte[] _buffer;

        AudioStream(byte[] buffer)
        {
            _buffer = buffer;
        }

    }

    class AudioHost
    {
        //Broke this into it's own service so modules can access audio streaming independently.
        private IAudioClient _voiceClient { get; set; } = null;

        //Set if we're paused, stopped, or in use by something.
        public bool inUse { get; set; } = false;
        public bool stop { get; set; } = false;
        public bool pause { get; set; } = false;

        
        public string name { get; set; } = "";
        
        public AudioHost() { }

        public async Task SendAudio(MemoryStream _store, Channel voiceChannel, DiscordClient client)
        {
            _voiceClient = await client.GetService<AudioService>().Join(voiceChannel);

            MemoryStream store = _store;
            int blockSize = 1920 * client.GetService<AudioService>()?.Config?.Channels ?? 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;
            inUse = true;
            while ((byteCount = store.Read(buffer, 0, blockSize)) > 0 && !stop) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing.
            {
                _voiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                while (pause) { System.Threading.Thread.Sleep(500); } // SLEEP MY CHILD
            }
            inUse = false;
            await _voiceClient.Disconnect();
        }
    }
}
