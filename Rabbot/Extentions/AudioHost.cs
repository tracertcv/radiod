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

using Rabbot.Types;

namespace Rabbot.Extentions
{    
    class AudioHost
    {
        //Broke this into it's own service so modules can access audio streaming independently.
        private IAudioClient _voiceClient { get; set; } = null;
        private DiscordClient _client;

        public List<AudioStream> queue;
        public AudioStream playing { get; set; } = null;

        private Dictionary<string, AudioStream> moduleStreams;

        public AudioHost(DiscordClient client)
        {
            queue = new List<AudioStream>();

            moduleStreams = new Dictionary<string, AudioStream>();

            _client = client;
            CommandService cmd = _client.GetService<CommandService>();

            cmd.CreateCommand("pause")
                .Description("Paused or resume the current audio")
                .Alias("p")
                .Do(async (e) =>
                {
                    if (playing != null && !playing.pause)
                    {
                        playing.pause = true;
                        await e.Channel.SendMessage("Paused!");
                        return;
                    }
                    if (playing != null && playing.pause)
                    {
                        playing.pause = false;
                        await e.Channel.SendMessage("Resuming!");
                        return;
                    }
                });

            cmd.CreateCommand("stop")
                .Description("Stop the current song")
                .Alias("s")
                .Do(async (e) =>
                {
                    if (playing != null)
                    {
                        playing.pause = false;
                        playing.stop = true;
                        playing = null;
                        await e.Channel.SendMessage("Stopping!");
                    }
                });

            cmd.CreateCommand("next")
                .Description("Move to the next song")
                .Alias("nxt")
                .Do(async (e) =>
                {
                    if (playing != null)
                    {
                        //playing.stop = true;
                        //playing = null;
                        await e.Channel.SendMessage("Next!");
                    }
                });

            cmd.CreateCommand("prev")
                .Description("Moves to the previous song")
                .Alias("prv")
                .Do(async (e) =>
                {
                    if (playing != null)
                    {
                        //playing.stop = true;
                        //playing = null;
                        await e.Channel.SendMessage("Previous!");
                    }
                });

            cmd.CreateCommand("move")
                .Description("Moves to a new voice channel")
                .Alias("mv")
                .Do(async (e) =>
                {
                    if (_voiceClient.Channel != null)
                    {
                        try
                        {
                            Console.WriteLine("foo");
                            Console.WriteLine(client.GetService<AudioService>().ToString());
                            _voiceClient = await client.GetService<AudioService>().Join(e.User.VoiceChannel);
                            await e.Channel.SendMessage("Moving to " + e.User.VoiceChannel.Name);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.GetType().ToString());
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                });
        }

        public AudioStream addQueue(MemoryStream _store)
        {
            AudioStream song = new AudioStream(_store);
            return song;
        }

        public async Task SendAudio(AudioStream _store, Channel voiceChannel, DiscordClient client)
        {
            if (_voiceClient.Channel == null)
            {
                _voiceClient = await client.GetService<AudioService>().Join(voiceChannel);
            }

            MemoryStream store = _store._buffer;
            int blockSize = 1920 * client.GetService<AudioService>()?.Config?.Channels ?? 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;
            playing = _store;
            while ((byteCount = store.Read(buffer, 0, blockSize)) > 0 && !playing.stop) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing.
            {
                _voiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                while (playing.pause) { System.Threading.Thread.Sleep(500); } // SLEEP MY CHILD
            }
            playing = null;
            await _voiceClient.Disconnect();
        }
    }
}