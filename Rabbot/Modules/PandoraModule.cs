using Discord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PandoraSharp;
using Discord;
using Discord.Commands;
using PandoraSharp.Structures;
using System.IO;
using Rabbot.Types;
using NAudio.Wave;
using Discord.Audio;

namespace Rabbot.Modules
{
    class PandoraModule : IAudioModule, IModule
    {

        private DiscordClient discordBaseClient;
        private AudioController audioController;
        private PandoraClient pandora;
        private const string myUID = "pandora";

        public void Install(ModuleManager manager)
        {
            discordBaseClient = manager.Client;
            audioController = discordBaseClient.GetModule<AudioController>().Instance;
            audioController.register(this);
            pandora = new PandoraClient();
            pandora.doPartnerAuth();
            pandora.doUserAuth();
            registerCommands();
        }

        private void registerCommands()
        {
            CommandService cmd = discordBaseClient.GetService<CommandService>();

            cmd.CreateCommand("pandora-list-stations")
                .Description("Lists available pandora stations.")
                .Alias("pnd-ls")
                .Do(async (e) =>
                {
                    string response = "--- Available Stations ---\n";
                    int i = 1;
                    foreach (PStation s in pandora.doGetStationList())
                    {
                        response = response + i + " - " + s.stationName + "\n";
                        i++;
                    }
                    response = response + "--------------------------------";
                    await e.User.SendMessage(response);
                });

            cmd.CreateCommand("pandora-select-station")
                .Description("Select a Pandora station to play.")
                .Alias("pnd-select")
                .Parameter("stationIndex", ParameterType.Required)
                .Do(async (e) =>
                {
                    int idx = int.Parse(e.GetArg("stationIndex"));
                    pandora.CurrentStation = pandora.StationList[idx - 1];
                    await getMP3Files();
                    
                });
        }

        public void onAudioEndOfStream(object sender, EventArgs e)
        {
            //getMP3Files();
        }

        public void onAudioPaused(object sender, EventArgs e)
        {
        }

        public void onAudioStarted(object sender, EventArgs e)
        {
        }

        public void onAudioStopped(object sender, EventArgs e)
        {
        }

        public void onAudioUnpaused(object sender, EventArgs e)
        {
            
        }

        private async Task getMP3Files()
        {
            foreach (PSong song in pandora.doGetStationPlaylist(pandora.CurrentStation))
            {
                try
                {
                    song.getMP3("highQuality").Close();
                    var mp3 = song.FileName;
                    MemoryStream store = new MemoryStream();

                    var channelCount = discordBaseClient.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                    var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                    
                    using (var MP3Reader = new MediaFoundationReader(mp3)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                    using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                    {
                        resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                        int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                        byte[] buffer = new byte[blockSize];
                        int byteCount;

                        while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present, as long as we are playing.
                        {
                            if (byteCount < blockSize)
                            {
                                // Incomplete Frame
                                for (int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            }
                            store.Write(buffer, 0, blockSize);
                        }
                        MP3Reader.Close();
                    }
                    store.Position = 0;
                    AudioStream aStream = new AudioStream(store);
                    aStream.Meta = song.artistName + " - " + song.songName;
                    audioController.addAudio(this, aStream);
                    File.Delete(mp3);
                }catch(Exception ex)
                {
                    Console.WriteLine(ex.GetType().ToString());
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        public string getUID()
        {
            return myUID;
        }
    }
}
