using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rabbot
{
    class AudioController : IModule
    {
        protected bool connected { get; set; }
        protected bool playing { get; set; }
        protected bool paused { get; set; }
        protected string currentStream { get; set; }

        private List<AudioModule> registeredModules;
        private Dictionary<string, List<MemoryStream>> _streams;

        private DiscordClient discordBaseClient;
        private AudioService discordAudioService;
        private IAudioClient discordAudioClient;
        private Channel discordVoiceChannel;
        private ModuleManager discordModuleManager;

        public void Install(ModuleManager manager)
        {
            discordModuleManager = manager;
            discordBaseClient = manager.Client;

            //Populate the audio service
            discordAudioService = discordBaseClient.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                Bitrate = 128,
            }));

            this.registeredModules = new List<AudioModule>();
            this._streams = new Dictionary<string, List<MemoryStream>>();
            this.registerCommands();
        }

        public bool register(AudioModule module)
        {
            bool stat = registeredModules.Contains(module);
            if (!stat) {
                registeredModules.Add(module);
                _streams.Add(module.getUID(), new List<MemoryStream>());
            }
            return !stat;
        }

        public void addAudio(AudioModule module, MemoryStream data)
        {
            if (registeredModules.Contains(module))
            {
                lock (_streams[module.getUID()])
                {
                    _streams[module.getUID()].Add(data);
                }
            }else
            {
                Log("Module '" + module.getUID() + "' is not registered with the audio controller.");
            }
        }

        /*
         * Abstracted methods to be called by discord commands.
         */

        private async Task pause()
        {
            this.paused = true;
            broadcastAudioEvent("onAudioPaused");
        }
        private async Task unpause()
        {
            this.paused = true;
            broadcastAudioEvent("onAudioUnpaused");
        }
        private async Task play()
        {
            if (!playing)
            {
                broadcastAudioEvent("onAudioStarted");
                paused = false;
                while (playing)
                {
                    await AudioLoop();
                    if(!hasData(currentStream)) { Thread.Sleep(500); }
                }
            }
        }
        private async Task stop()
        {
            playing = false;
            broadcastAudioEvent("onAudioStopped");
        }
        private async Task<bool> switchStream(string newStream)
        {
            bool stat = this._streams.ContainsKey(newStream);
            if (stat)
            {
                this.currentStream = newStream;
            }
            else
            {
                Log("Couldn't switch to audio stream '" + newStream + "' - no such stream registered.");
            }
            return stat;
        }


        /* 
         * Internal methods for controlling things directly.
         */

        private void registerCommands() //Define all commands here.
        {
            CommandService cmd = discordBaseClient.GetService<CommandService>();
            cmd.CreateCommand("play")
                .Description("Begin playback from the selected stream.")
                .Alias("pl")
                .Do(async (e) =>
                {
                    await play();
                    await e.User.SendMessage("Playback started.");
                });
            cmd.CreateCommand("pause")
                .Description("Pause the current audio")
                .Alias("p")
                .Do(async (e) => 
                {
                    await pause();
                    await e.User.SendMessage("Playback paused.");
                });
            cmd.CreateCommand("unpause")
                .Description("Resume the current audio")
                .Alias("up")
                .Do(async (e) =>
                {
                    await unpause();
                    await e.User.SendMessage("Playback resumed.");
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
                        await e.User.SendMessage("Next!");
                    }
                });
            cmd.CreateCommand("move")
                .Description("Moves to a new voice channel")
                .Alias("mv")
                .Do(async (e) =>
                {
                    discordAudioClient = await discordBaseClient.GetService<AudioService>().Join(e.User.VoiceChannel);
                    discordVoiceChannel = e.User.VoiceChannel;
                    await e.User.SendMessage("Moving to " + e.User.VoiceChannel.Name);
                });
            cmd.CreateCommand("list-treams")
                .Description("Lists all available streams.")
                .Alias("ls-s")
                .Do(async (e) =>
                {
                    string response = "--- Available Audio Streams ---\n";
                    foreach (AudioModule module in registeredModules)
                    {
                        response = response + module.getUID() + "\n";
                    }
                    response = response + "--------------------------------";
                    await e.User.SendMessage(response);
                });
            cmd.CreateCommand("setstream")
                .Description("Sets the current audio stream.")
                .Alias("ss")
                .Parameter("newStream", ParameterType.Required)
                .Do(async (e) =>
                {
                    string msg;
                    bool result = await switchStream(e.GetArg("newStream"));
                    if (result)
                    {
                        msg = "Successfully switched stream to '" + e.GetArg("newStream") + "'.";
                    } else
                    {
                        msg = "Couldn't switch stream to '" + e.GetArg("newStream") + "'.";
                    }
                    await e.User.SendMessage(msg);
                });

        }

        private async Task AudioLoop()
        {
            MemoryStream _buffer = new MemoryStream();
            while (hasData(currentStream))
            {
                
                while (paused) { Thread.Sleep(500); }
                lock (_streams[currentStream])
                {
                    _streams[currentStream].First().CopyTo(_buffer);
                    _streams[currentStream].RemoveAt(0);
                }
                if (discordAudioClient.Channel == null)
                {
                    discordAudioClient = await discordAudioService.Join(discordVoiceChannel);
                }
                int blockSize = 1920 * discordAudioService?.Config?.Channels ?? 3840;
                byte[] buffer = new byte[blockSize];
                int byteCount;
                while ((byteCount = _buffer.Read(buffer, 0, blockSize)) > 0)  // Read audio into our buffer, and keep a loop open while data is present.
                {
                    discordAudioClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                }                
            }
            broadcastAudioEvent("onEndOfStream");
            //await discordAudioClient.Disconnect();
        }

        private void broadcastAudioEvent(string method)
        {
            foreach(AudioModule module in registeredModules)
            {
                try
                {
                    typeof(AudioModule).GetMethod(method).Invoke(module, null);
                }catch(Exception e)
                {
                    Log("Exception while broadcasting " + method + " to " + module.getUID() + ": " + e.StackTrace);
                }
            }
        }

        private void Log(string msg)
        {
            string _logprefix = "AudioController";
            Console.WriteLine("[" + _logprefix + "] - " + msg);
        }

        private bool hasData(string stream)
        {
            bool stat = false;
            lock (_streams[currentStream])
            {
                if (_streams[currentStream].Count() > 0)
                {
                    stat = true;
                }
            }
            return stat;
        }

    }
}
