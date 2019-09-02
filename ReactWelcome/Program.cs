using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace ReactWelcome
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient socketClient;
        private DiscordRestClient restClient;
        private Config config;
        private ulong updateChannel = 0;

        private class RoleAddition
        {
            public ulong userId;
            public ulong roleId;
            public bool remove = false;
        }
        

        private async Task RunAsync()
        {
            socketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 100
            });
            socketClient.Log += Log;

            restClient = new DiscordRestClient(new DiscordRestConfig
            {
                LogLevel = LogSeverity.Verbose
            });
            restClient.Log += Log;

            if (File.Exists("./update"))
            {
                var temp = File.ReadAllText("./update");
                ulong.TryParse(temp, out updateChannel);
                File.Delete("./update");
                Console.WriteLine($"Found an update file! It contained [{temp}] and we got [{updateChannel}] from it!");
            }

            config = await Config.Load();

            await socketClient.LoginAsync(TokenType.Bot, config.Token);
            await socketClient.StartAsync();

            await restClient.LoginAsync(TokenType.Bot, config.Token);

            if (File.Exists("./deadlock"))
            {
                Console.WriteLine("We're recovering from a deadlock.");
                File.Delete("./deadlock");
                foreach (var u in config.OwnerIds)
                {
                    (await restClient.GetUserAsync(u))?
                        .SendMessageAsync($"I recovered from a deadlock.\n`{DateTime.Now.ToShortDateString()}` `{DateTime.Now.ToLongTimeString()}`");
                }
            }

            socketClient.GuildAvailable += Client_GuildAvailable;
            socketClient.Disconnected += SocketClient_Disconnected;

            try
            {
                socketClient.ReactionAdded += Client_ReactionAdded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Source}\n{ex.Message}\n{ex.StackTrace}");
            }

            await Task.Delay(-1);
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            if (updateChannel != 0 && guild.GetTextChannel(updateChannel) != null)
            {
                await Task.Delay(3000); // wait 3 seconds just to ensure we can actually send it. this might not do anything.
                await guild.GetTextChannel(updateChannel).SendMessageAsync("Successfully reconnected.");
                updateChannel = 0;
            }
        }

        private async Task SocketClient_Disconnected(Exception ex)
        {
            // If we disconnect, wait 3 minutes and see if we regained the connection.
            // If we did, great, exit out and continue. If not, check again 3 minutes later
            // just to be safe, and restart to exit a deadlock.
            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 2; i++)
                {
                    await Task.Delay(1000 * 60 * 3);

                    if (socketClient.ConnectionState == ConnectionState.Connected)
                        break;
                    else if (i == 1)
                    {
                        File.Create("./deadlock");
                        await config.Save();
                        Environment.Exit((int)ExitCodes.ExitCode.DeadlockEscape);
                    }
                }
            });
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // Only listen to reactions in a single channel
            if (channel.Id != config.GatewayChannelId)
                return;

            // Only look for the specified reaction
            if (reaction.Emote.Name != config.AccessReaction)
                return;

            var user = reaction.User.Value as SocketGuildUser;
            var guild = (channel as SocketGuildChannel).Guild;
            var role = guild.GetRole(config.AccessRoleId);
            var welcomeChannel = guild.GetChannel(config.WelcomeChannelId) as SocketTextChannel;

            await user.AddRoleAsync(role);

            await welcomeChannel.SendMessageAsync(config.WelcomeString.Replace("$user", user.Mention));
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
