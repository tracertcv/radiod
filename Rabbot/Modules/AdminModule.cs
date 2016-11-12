using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rabbot.Types;

using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

namespace Rabbot.Modules
{
    class AdminModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;

        public string test = "hello";

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            manager.CreateCommands("", cmd =>                           // Create commands with the manager
            {
                cmd.CreateCommand("say")                                // Usage -> `!say {string}`
                    .Description("Make the bot say something")          
                    .Alias("s")                                         
                    .MinPermissions((int)Permissions.ServerAdmin)       
                    .Parameter("text", ParameterType.Unparsed)          
                    .Do(async (e) =>                                    
                    {
                        string text = e.Args[0];                        // Copy the first paramter into a variable
                        await e.Channel.SendMessage(text);              // Send the text to the channel the command was executed in
                    });

                cmd.CreateCommand("disconnect")                         // The command text is `!disconnect'
                    .Description("Force bot to disconnet")
                    .Alias("dc")
                    .MinPermissions((int)Permissions.ServerAdmin)
                    .Do(async (e) =>
                    {
                        await e.Channel.SendMessage("Disconnecting!");  // Announce disconnect
                        await _client.Disconnect();                     // LEAVE FOREVER!
                    });

                cmd.CreateGroup("set", (b) =>                           // Create a group of sub-commands `!set`
                {
                    b.CreateCommand("nick")                             // Usage -> `!set nick {name}`
                        .Description("Change your nickname.")
                        .Parameter("name", ParameterType.Unparsed)
                        .Do(async (e) =>
                        {
                            string name = e.Args[0];                    // Copy the first parameter into a variable
                            var user = e.User;                          
                            await user.Edit(nickname: name);            // Edit the user's nickname.
                            await e.Channel.SendMessage($"{user.Mention} I changed your name to **{name}**");
                        });

                    b.CreateCommand("botnick")                          // Usage -> `!set botnick {name}`
                        .Description("Change the bot's nickname.")
                        .MinPermissions((int)Permissions.ServerOwner)   // Limit this command to server owner
                        .Parameter("name", ParameterType.Unparsed)
                        .Do(async (e) =>
                        {
                            string name = e.Args[0];                    // Copy the first parameter into a variable
                            var bot = e.Server.CurrentUser;             // Get the bot's user object for this server.
                            await bot.Edit(nickname: name);             // Edit the user's nickname.
                            await e.Channel.SendMessage(                // Let the user know the command executed successfully.
                            $"I changed my name to **{name}**");
                        });
                });
            });
        }
    }
}
