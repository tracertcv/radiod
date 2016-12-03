﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Modules;
using Discord.Net;
using Discord.Commands.Permissions.Levels;

using Rabbot.Modules;
using Rabbot.Types;

namespace Rabbot
{
    class myBot
    {
        //Client and commands
        public static DiscordClient discord;

        //Token/config file
        private Configuration config;


        public myBot()
        {
            const string configFile = "configuration.json";

            try
            {
                config = Configuration.LoadFile(configFile);           // Load the configuration from a saved file.
            }

            catch
            {
                config = new Configuration();                          // Create a new configuration file if it doesn't exist.

                Console.WriteLine("The example bot's configuration file has been created. Please enter a valid token.");
                Console.Write("Token: ");
                config.Token = Console.ReadLine();                     // Read the user's token from the console.

                Console.WriteLine("Please enter the owner ID.");
                Console.Write("ID: ");
                config.Owners.Add(UInt64.Parse(Console.ReadLine()));

                config.SaveFile(configFile);
            }


            discord = new DiscordClient(x =>                           //Set log levels
            {
                x.LogLevel = LogSeverity.Verbose;
                x.LogHandler = Log;
            })
            .UsingCommands(x =>
            {
                x.PrefixChar = config.Prefix;
                x.AllowMentionPrefix = true;
            })
            .UsingPermissionLevels((u, c) => (int)GetPermission(u, c))
            .UsingModules();

            //Register our modules
            discord.AddModule<AdminModule>("Admin", ModuleFilter.None);
            discord.AddModule<AudioController>("Audio", ModuleFilter.None);
            discord.AddModule<PandoraModule>("Pandora", ModuleFilter.None);
            mycommands();

            discord.ExecuteAndWait(async () =>                         //Log in to Discord with token as a bot.
            {
                while (true)
                {
                    try
                    {
                        await discord.Connect(config.Token, TokenType.Bot);
                        break;
                    }
                    catch
                    {
                        await Task.Delay(3000);
                    }
                }
            });
        }

        private Permissions GetPermission(User user, Channel channel)
        {
            if (user.IsBot)                                     // NO BEEPS OR BOOPS.
                return Permissions.Ignored;

            if (config.Owners.Contains(user.Id))               // IRULEU.
                return Permissions.BotOwner;

            if (!channel.IsPrivate)                             // NO WHISPERS.
            {
                if (user.GetPermissions(channel).ManageChannel) // WHAT'S A MOB TO A KING.
                    return Permissions.ChannelAdmin;

                if (user.ServerPermissions.Administrator)       // WHAT'S A KING TO A GOD.
                    return Permissions.ServerAdmin;

                if (user == channel.Server.Owner)               // WHAT'S A GOD TO A NON BELIEVER.
                    return Permissions.ServerOwner;

            }

            return Permissions.User;                            // HUMAN BEINGS IN A MOB.
        }

        private void Log(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        private void mycommands()
        {
            CommandService cmd = discord.GetService<CommandService>();
            cmd.CreateCommand("me")
                .Description("Find out what the bot knows about you.")
                .Do(async (e) =>
                {
                    try
                    {
                        User user = e.User;
                        await user.SendMessage("Your name is " + user.Name + "\nYour server is " + user.Server.Name + "\nYour channels are " + user.Channels.ToArray() + "\nYour voice channel is ");
                    }catch(Exception ex)
                    {
                        Console.WriteLine(ex.GetType().ToString());
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                });
        }

    }
}
