using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;

namespace Rabbot.Modules
{
    class AudioModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;

        //Broke this into it's own service so modules can access audio streaming independently.
        private IAudioClient _voiceClient { get; set; } = null;

        private List<AudioStream> queue;

        //Set if we're paused, stopped, or in use by something.
        public AudioStream current { get; set; } = null;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            manager.CreateCommands("", cmd =>
            {
                cmd.CreateCommand("stop")
                    .Description("Stop current song")
                    .Alias("s")
                    .Do(async (e) =>
                    {
                        if (current != null)
                        {
                            current.stop = true;
                            current = null;
                            await e.Channel.SendMessage("Stopping!");
                        }
                    });

                cmd.CreateCommand("pause")
                    .Description("Pause current song")
                    .Alias("p")
                    .Do(async (e) =>
                    {
                        if ((current != null) && !current.pause)
                        {
                            current.pause = true;
                            await e.Channel.SendMessage("Paused!");
                        }
                    });

                cmd.CreateCommand("resume")
                    .Description("Resume current song")
                    .Alias("r")
                    .Do(async (e) =>
                    {
                        if ((current != null) && current.pause)
                        {
                            current.pause = false;
                            await e.Channel.SendMessage("Resuming!");
                        }
                    });
            });
        }

        public async Task SendAudio(AudioStream _store, Channel voiceChannel, DiscordClient client)
        {
            try
            {
                _voiceClient = await client.GetService<AudioService>().Join(voiceChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException.ToString());
            }

            MemoryStream store = _store._buffer;
            int blockSize = 1920 * client.GetService<AudioService>()?.Config?.Channels ?? 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;
            current = _store;
            while ((byteCount = store.Read(buffer, 0, blockSize)) > 0 && !current.stop) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing.
            {
                _voiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                while (current.pause) { System.Threading.Thread.Sleep(500); } // SLEEP MY CHILD
            }
            current = null;
            await _voiceClient.Disconnect();
        }
    }
}
