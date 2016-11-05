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

namespace Rabbot.Extentions
{
    class AudioStream
    {
        public string _name { get; set; } = "";
        public MemoryStream _buffer;
        public User _addedBy;

        public AudioStream(MemoryStream buffer)
        {
            _buffer = buffer;
        }

        public AudioStream(MemoryStream buffer, string name)
        {
            _buffer = buffer;
            _name = name;
        }

        public AudioStream(MemoryStream buffer, string name, User addedBy)
        {
            _buffer = buffer;
            _name = name;
            _addedBy = addedBy;
        }

    }

    class AudioHost
    {
        //Broke this into it's own service so modules can access audio streaming independently.
        private DiscordClient _client;
        private IAudioClient _voiceClient = null;
        public AudioStream current;

        //Set if we're paused, stopped, or in use by something.
        public bool stop { get; set; } = false;
        public bool pause { get; set; } = false;

        List<AudioStream> queue = new List<AudioStream>();

        public AudioHost(DiscordClient client)
        {
            _client = client;
        }
        
        public AudioStream addQueue(MemoryStream _buffer)
        {
            AudioStream song = new AudioStream(_buffer);
            queue.Add(song);
            return song;
        }

        public void removeQueue(AudioStream _song)
        {
            if (current != _song)
            {
                try { queue.Remove(_song); }
                catch (Exception e) { throw; }
            }
            else
            {
                stop = true;
                try { queue.Remove(_song); }
                catch (Exception e) { throw; }
            }
        }

        public async Task advanceQueue()
        {
            AudioStream _next = queue[queue.IndexOf(current) + 1];
            stop = true;
            //await SendAudio(_next);
        }

        public void clearQueue()
        {
            stop = true;
            queue.Clear();
        }

        public async Task SendAudio(AudioStream store, Channel e)
        {
            MemoryStream _store = store._buffer;

            _voiceClient = await _client.GetService<AudioService>().Join(e);

            int blockSize = 1920 * _client.GetService<AudioService>()?.Config?.Channels ?? 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;

            current = store;
            while ((byteCount = _store.Read(buffer, 0, blockSize)) > 0 && !stop) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing.
            {
                _voiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                while (pause) { System.Threading.Thread.Sleep(500); } // SLEEP MY CHILD
            }
            current = null;
            await _voiceClient.Disconnect();
        }
    }
}
