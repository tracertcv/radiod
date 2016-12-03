using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using Rabbot.Types;
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
        //Some state things
        protected bool connected { get; set; }
        protected bool playing { get; set; } = false;
        protected bool paused { get; set; } = false;
        protected string currentStream { get; set; }

        //Data structures for managing registered module queues
        private List<IAudioModule> registeredModules;
        private Dictionary<string, List<AudioStream>> _streams;
        private Thread audioThread;

        //Things we need for the discord API
        private DiscordClient discordBaseClient;
        private AudioService discordAudioService;
        private IAudioClient discordAudioClient;
        private Channel discordVoiceChannel;
        private ModuleManager discordModuleManager;

        //Fucking event handlers
        public event EventHandler AudioStarted;
        public event EventHandler AudioStopped;
        public event EventHandler AudioPaused;
        public event EventHandler AudioUnpaused;
        public event EventHandler AudioEndOfStream;

        //Register with discord base API
        void IModule.Install(ModuleManager manager)
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

            this.registeredModules = new List<IAudioModule>();
            this._streams = new Dictionary<string, List<AudioStream>>();
            this.registerCommands();
        }

        //Register audio modules with us
        public bool register(IAudioModule module)
        {
            bool stat = registeredModules.Contains(module);
            if (!stat) {
                registeredModules.Add(module);
                _streams.Add(module.getUID(), new List<AudioStream>()); //This is now sort of unnecessary.  Just go with it.
                AudioStarted += module.onAudioStarted;
                AudioStopped += module.onAudioStopped;
                AudioPaused += module.onAudioPaused;
                AudioUnpaused += module.onAudioUnpaused;
                AudioEndOfStream += module.onAudioEndOfStream;
            }
            return !stat;
        }

        //Registered audio modules pass in audio data with this
        public void addAudio(IAudioModule module, AudioStream data)
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
            AudioPaused(this, new EventArgs());
        }

        private async Task unpause()
        {
            this.paused = false;
            AudioUnpaused(this, new EventArgs());
        }

        private void play()
        {
            if (!playing)
            {
                AudioStarted(this, new EventArgs());
                playing = true;
                paused = false;
                audioThread = new Thread(new ThreadStart(SendAudio));
                audioThread.Start();
            }
            
        }

        private async Task stop()
        {
            playing = false;
            AudioStopped(this, new EventArgs());
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
                    play();
                    await e.User.SendMessage("Playback started.");
                });
            cmd.CreateCommand("stop")
                .Description("Stop playback.")
                .Alias("st")
                .Do(async (e) =>
                {
                    await stop();
                    await e.User.SendMessage("Playback stopped.");
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
                    if (playing != false)
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
                    discordAudioClient = await discordBaseClient.GetService<AudioService>().Join(e.User.VoiceChannel);//e.User.VoiceChannel);
                    discordVoiceChannel = discordAudioClient.Channel;
                    await e.User.SendMessage("Moving to " + e.User.VoiceChannel);
                });
            cmd.CreateCommand("list-treams")
                .Description("Lists all available streams.")
                .Alias("ls-s")
                .Do(async (e) =>
                {
                    string response = "--- Available Audio Streams ---\n";
                    foreach (IAudioModule module in registeredModules)
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

        private void AudioLoop()
        {
            while (playing)
            {
                SendAudio();
                if (!hasData(currentStream)) { Thread.Sleep(500); }
            }
            audioThread.Abort();
        }

        private void SendAudio()
        {
            MemoryStream _buffer = new MemoryStream();
            while (hasData(currentStream) && playing)
            {
                string songName;
                lock (_streams[currentStream])
                {
                    _buffer = _streams[currentStream].First().Buffer;
                    songName = _streams[currentStream].First().Meta;
                    discordBaseClient.SetGame(songName);
                    _streams[currentStream].RemoveAt(0);

                }
                if (discordAudioClient == null)
                {
                    //discordAudioClient = await discordAudioService.Join(discordVoiceChannel);
                }
                int blockSize = 1920 * discordAudioService?.Config?.Channels ?? 3840;
                byte[] buffer = new byte[blockSize];
                int byteCount;
                while ((byteCount = _buffer.Read(buffer, 0, blockSize)) > 0)  // Read audio into our buffer, and keep a loop open while data is present.
                {
                    while (paused)
                    {
                        discordBaseClient.SetGame("Paused");
                        Thread.Sleep(500);
                        discordBaseClient.SetGame(songName);
                    }
                    if (byteCount < blockSize)
                    {
                        // Incomplete Frame
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    discordAudioClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    
                    
                }      
                if(!hasData(currentStream))
                {
                    AudioEndOfStream(this, new EventArgs());
                }          
            }
            
            discordAudioClient.Clear();
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