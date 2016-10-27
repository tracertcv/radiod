using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Rabbot.Types;

using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace Rabbot.Modules
{
    class MusicModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private static IAudioClient _voiceClient { get; set; } = null;

        //Audio Stuff

        // Bool, when playing a song, set it to true.
        private static bool playingSong = false;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            //Populate the audio service
            var audio = _client.AddService<AudioService>(new AudioService(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                Bitrate = 128,
            }));

            manager.CreateCommands("", cmd =>
            {
                cmd.CreateCommand("play")                           // The command text is `!play {file}`.
                    .Description("Play a given .mp3")               
                    .Parameter("filename", ParameterType.Required)
                    .Do(async (e) =>
                    {
                        string filename = e.GetArg("filename");
                        if (File.Exists(filename))                  // Make sure the file is real.
                        {
                            playingSong = true;                     // Set our playing bool.
                            await e.Channel.SendMessage("Now playing " + e.GetArg("filename"));
                            await SendAudio(e.GetArg("filename"), e.User.VoiceChannel, _client);
                        }
                        else
                        { await e.Channel.SendMessage("Unable to find file!"); }
                    });

                cmd.CreateCommand("stop")
                    .Do(async (e) =>
                    {
                        playingSong = false;
                        await e.Channel.SendMessage("Stopping!");
                    });
            });
        }

        public static async Task SendAudio(string filePath, Channel voiceChannel, DiscordClient client)
        {

            _voiceClient = await client.GetService<AudioService>().Join(voiceChannel);

            var channelCount = client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
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
                    _voiceClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                }
                await _voiceClient.Disconnect();
            }

        }
    }
}
