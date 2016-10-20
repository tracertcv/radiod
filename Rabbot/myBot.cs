using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using Discord.Net;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;


namespace Rabbot
{
    class myBot
    {
        //Client and commands
        public static DiscordClient discord;
        CommandService commands;

        //Audio Stuff
        public Channel VoiceChannel { get; set; } = null;
        public static IAudioClient VoiceClient { get; set; } = null;

        // Bool, when playing a song, set it to true.
        private static bool playingSong = false;


        public myBot()
        {
            //Set log levels
            discord = new DiscordClient(x =>
            {
                x.LogLevel = LogSeverity.Verbose;
                x.LogHandler = Log;
            });

            //Populate the audio service
            var audio = discord.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                //EnableMultiserver = true,
                Bitrate = 128,
            }));

            //Set command prefix
            discord.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;
            });

            commands = discord.GetService<CommandService>(); //Command service

            //Register our commands
            RegisterHello();
            RegisterVoice();
            RegisterPlay();

            //Actual execute for bot instance TODO use better/cleaner login.
            discord.ExecuteAndWait(async () =>
            {
                await discord.Connect("", TokenType.Bot); //TOKEN GO HERE
            });
        }
        
        private void RegisterHello()
        {
            commands.CreateCommand("hello")
                .Do(async (e) => 
                {
                    await e.Channel.SendMessage("Hi!");
                });
        }

        private void RegisterVoice()
        {
            commands.CreateCommand("joinVoice")
                .Do(async (e) =>
                {
                    try
                    {
                        if (VoiceClient == null)
                        {
                            VoiceChannel = e.User.VoiceChannel;
                            Console.WriteLine($"Joining voice channel " + VoiceChannel.Name +" ["+DateTime.Now.Second+"]");
                            VoiceClient = await Task.Run(async () => await VoiceChannel.JoinAudio());
                            Console.WriteLine($"Joined voicechannel [{DateTime.Now.Second}]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Starting failed: {ex}");
                    }
                });

            commands.CreateCommand("testVoice")
                            .Do(async (e) =>
                            {
                                try
                                {
                                    if (VoiceClient.Channel != null)
                                    {
                                        VoiceChannel = e.User.VoiceChannel;
                                        //Console.WriteLine($"Joining voice channel " + VoiceChannel.Name + " [{DateTime.Now.Second}]");
                                        VoiceClient = await Task.Run(async () => await VoiceChannel.JoinAudio());
                                        //Console.WriteLine($"Joined voicechannel [{DateTime.Now.Second}]");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Test failed: {ex}");
                                }
                            });

            commands.CreateCommand("leaveVoice")
                .Do(async (e) =>
                {
                    try
                    {
                        if (VoiceClient.Channel != null)
                        {
                            VoiceChannel = e.User.VoiceChannel;
                            Console.WriteLine($"Leaving voice channel " + VoiceChannel.Name + " [{DateTime.Now.Second}]");
                            await Task.Run(async () => await VoiceChannel.LeaveAudio());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Leave failed: {ex}");
                    }
                });
        }

        private void RegisterPlay()
        {
            commands.CreateCommand("play")
                .Do(async (e) =>
                {
                    playingSong = true;
                    await e.Channel.SendMessage("Playing!");
                    await SendAudio("test.mp3", e.User.VoiceChannel);
                });

            commands.CreateCommand("stop")
                .Do(async (e) =>
                {
                    playingSong = false;
                    await e.Channel.SendMessage("Stopping!");
                });
        }

        public static async Task SendAudio(string filePath, Channel voiceChannel)
        {

            VoiceClient = await discord.GetService<AudioService>().Join(voiceChannel);

            var channelCount = discord.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
            var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
            using (var MP3Reader = new Mp3FileReader(filePath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
            using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
            {
                resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                byte[] buffer = new byte[blockSize];
                int byteCount;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && playingSong) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing a song.

                {
                    if (byteCount < blockSize)
                    {
                        // Incomplete Frame
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    VoiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                }
                await VoiceClient.Disconnect();
            }

        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

    }
}
