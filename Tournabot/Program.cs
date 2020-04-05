using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Tournabot.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Collections.Generic;
using Discord.Rest;
using System.Timers;
using Tesseract;
using System.IO;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using Image = System.Drawing.Image;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Tournabot
{
    /*
      REST:
          POST Message |  5/5s    | per-channel
          DELETE Message |  5/1s    | per-channel
          PUT/DELETE Reaction |  1/0.25s | per-channel
          PATCH Member |  10/10s  | per-guild
          PATCH Member Nick |  1/1s    | per-guild
          PATCH Username |  2/3600s | per-account
          |All Requests| |  50/1s   | per-account
      WS:
          Gateway Connect |   1/5s   | per-account
          Presence Update |   5/60s  | per-session
          |All Sent Messages| | 120/60s  | per-session
    */
    public class Program
    {
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;
        private Queue<(IUser, IRole, bool)> roleQueue = new Queue<(IUser, IRole, bool)>();
        private string[] ScrimAdmins = { "", "", "", "", "", ""};
        private SocketGuild guild;
        private int maxScrim;
        private List<IUser>[] tempLists = { new List<IUser>(), new List<IUser>(), new List<IUser>(), new List<IUser>(), new List<IUser>(), new List<IUser>() };
        private Emoji checkmark = new Emoji("✅");
        private Emoji emoteEast = new Emoji("🇺🇸");
        private Emoji emoteEU = new Emoji("🇪🇺");
        private Emoji emoteSA = new Emoji("🇧🇷");
        private Emoji emoteSP = new Emoji("🇸🇬");
        private Emoji emoteAU = new Emoji("🇦🇺");
        private Emoji emoteExit = new Emoji("❌");
        private Emote emoteWest = Emote.Parse("<:cali:663097025033666560>");
        private Emote start = Emote.Parse("<:start:663144594401132603>");
        private Emote manualStart = Emote.Parse("<:manual_start:663450072834375720>");

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            client.MessageReceived += HandleCommand;
            client.MessageDeleted += HandleMessageDeleted;
            client.UserJoined += HandleJoinedGuild;
            client.ReactionAdded += HandleReaction;
            client.ReactionRemoved += HandleReactionRemoved;
            client.UserLeft += HandleLeaveGuild;
            client.Log += Log;
            var timer = new Timer(4000);
            timer.Elapsed += RoleManager;

            timer.Enabled = true;
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ConfigHandler>()
                .BuildServiceProvider();
            await services.GetService<ConfigHandler>().PopulateConfig();

            maxScrim = services.GetService<ConfigHandler>().GetMaxScrimSize();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, services.GetService<ConfigHandler>().GetToken());
            await client.StartAsync();
            
            await Task.Delay(-1);
        }

        public CommandService GetCommands()
        {
            return commands;
        }

        private async Task HandleMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (message.Id == services.GetService<ConfigHandler>().GetEastScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetEastScrimActiveRole(), 0);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWestScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetWestScrimActiveRole(), 1);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEUScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetEastScrimActiveRole(), 2);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSAScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetSAScrimActiveRole(), 3);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSPScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetSPScrimActiveRole(), 4);
            }
            else if(message.Id == services.GetService<ConfigHandler>().GetAUScrimMessage())
            {
                await DoDeleteScrimMessage(services.GetService<ConfigHandler>().GetAUScrimActiveRole(), 5);
            }
        }

        private async void RoleManager(object source, ElapsedEventArgs e)
        {
            try
            {
                if (guild == null)
                {
                    guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
                }
                for (int i = 0; i < 3; i++)
                {
                    if (roleQueue.Count() > 0)
                    {
                        var item = roleQueue.Dequeue();
                        var user = item.Item1 as SocketGuildUser;
                        var role = item.Item2 as SocketRole;
                        Console.WriteLine("USER: " + user + " ROLE: " + item.Item2 + " ADDED: " + item.Item3 + " TIMESTAMP: " + DateTime.Now);
                        if (item.Item3)
                        {
                            await user.AddRoleAsync(role);
                        }
                        else
                        {
                            await user.RemoveRoleAsync(role);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task HandleLeaveGuild(SocketGuildUser arg)
        {
            var message = await RemovePlayer(arg.Id);
            var dmChannel = await arg.Guild.Owner.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("User left guild: " + arg.Username + "\n"+ message);
        }

        public async Task HandleReaction(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == client.CurrentUser.Id)
                return;
            Task.Run(() => handleReactionCheck(message, channel, reaction));
            await Task.CompletedTask;
        }

        public async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == client.CurrentUser.Id)
                return;
            Task.Run(() => handleReactionRemovedCheck(message, channel, reaction));
            await Task.CompletedTask;
        }

        private async Task handleReactionCheck(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id == services.GetService<ConfigHandler>().GetRegionMessage())//REGION
            {
                var user = reaction.User.Value;
                if (reaction.Emote.Name == emoteEast.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "NA", "EAST");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
                else if (reaction.Emote.Name == emoteWest.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "WE", "WEST");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
                else if (reaction.Emote.Name == emoteEU.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "EU", "EU");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
                else if (reaction.Emote.Name == emoteSA.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "SA", "SA");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
                else if (reaction.Emote.Name == emoteSP.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "SP", "SP");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
                else if (reaction.Emote.Name == emoteAU.Name)
                {
                    var dmMessage = await AddMemberRegion(user.Id, "AU", "OCE");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSignUpMessage())//SIGN UP
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var dmMessage = await AddMemberSignUp(user);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetCheckInMessage())//CHECK IN
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var dmMessage = await AddMemberCheckIn(user);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWaitListMessage())//WAIT LIST
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var dmMessage = await AddMemberWaitList(user);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetScrimMessage())//SCRIM GENERIC
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var dmMessage = await AddScrimSignUp(user);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetDashboardMessage())//SCRIM DASHBOARD
            {
                if (reaction.Emote.Name == emoteEast.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetEastScrimChannel(), 
                        services.GetService<ConfigHandler>().GetEastScrimRole(), 0);
                }
                else if (reaction.Emote.Name == emoteWest.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetWestScrimChannel(),
                        services.GetService<ConfigHandler>().GetWestScrimRole(), 1);
                }
                else if (reaction.Emote.Name == emoteEU.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetEUScrimChannel(),
                        services.GetService<ConfigHandler>().GetEUScrimRole(), 2);
                }
                else if (reaction.Emote.Name == emoteSA.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetSAScrimChannel(),
                        services.GetService<ConfigHandler>().GetSAScrimRole(), 3);
                }
                else if (reaction.Emote.Name == emoteSP.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetSPScrimChannel(),
                        services.GetService<ConfigHandler>().GetSPScrimRole(), 4);
                }
                else if (reaction.Emote.Name == emoteAU.Name)
                {
                    await DoScrimMessage(reaction, services.GetService<ConfigHandler>().GetAUScrimChannel(),
                        services.GetService<ConfigHandler>().GetAUScrimRole(), 5);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEastScrimMessage())//EAST SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetEastScrimChannel(), 
                    services.GetService<ConfigHandler>().GetEastScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetEastScrimActiveRole()), "EAST", 0);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWestScrimMessage())//WEST SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetWestScrimChannel(),
                    services.GetService<ConfigHandler>().GetWestScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetWestScrimActiveRole()), "WEST", 1);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEUScrimMessage())//EU SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetEUScrimChannel(),
                    services.GetService<ConfigHandler>().GetEUScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetEUScrimActiveRole()), "EU", 2);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSAScrimMessage())//SA SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetSAScrimChannel(),
                    services.GetService<ConfigHandler>().GetSAScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetSAScrimActiveRole()), "SA", 3);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSPScrimMessage())//SP SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetSPScrimChannel(),
                    services.GetService<ConfigHandler>().GetSPScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetSPScrimActiveRole()), "SP", 4);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetAUScrimMessage())//AU SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetAUScrimChannel(),
                    services.GetService<ConfigHandler>().GetAUScrimMessage(), guild.GetRole(services.GetService<ConfigHandler>().GetAUScrimActiveRole()), "AU", 5);
            }
        }

        private async Task DoScrimMessage(SocketReaction reaction, ulong scrimChannel, ulong scrimRole, int index)
        {
            var user = reaction.User.Value;
            if (ScrimAdmins[index] != "")
            {
                var messId = await reaction.Channel.GetMessageAsync(reaction.MessageId) as IUserMessage;
                await messId.RemoveReactionAsync(reaction.Emote, user);
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + ScrimAdmins[index]);
            }
            else
            {
                ScrimAdmins[index] = user.Username;
                var builder = new EmbedBuilder()
                    .WithTitle("Scrim Dashboard")
                    .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                    $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                    .AddField("Max Scrim Size: ", maxScrim)
                    .WithColor(new Discord.Color(0xF5FF))
                    .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
                var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                var messId = await reaction.Channel.GetMessageAsync(reaction.MessageId) as IUserMessage;
                await messId.RemoveReactionAsync(reaction.Emote, user);
                builder = new EmbedBuilder()
                    .WithTitle("Scrim Signup")
                    .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers:" +
                    "\nclick the  <:start:663144594401132603>  to start the scrim with FULL lobbies." +
                    "\nclick the  <:manual_start:663450072834375720>  to start the scrim with PARTIAL lobbies." +
                    "\nclick the  ❌  to end the scrim session.")
                    .WithColor(new Discord.Color(0xD3FF))
                    .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                var chan = client.GetChannel(scrimChannel) as SocketTextChannel;
                var signUpMessage = await chan.SendMessageAsync(text: guild.GetRole(scrimRole).Mention, embed: builder);
                if(index == 0)
                {
                    services.GetService<ConfigHandler>().SetEastScrimMessage(signUpMessage.Id);
                }
                else if (index == 1)
                {
                    services.GetService<ConfigHandler>().SetWestScrimMessage(signUpMessage.Id);
                }
                else if (index == 2)
                {
                    services.GetService<ConfigHandler>().SetEUScrimMessage(signUpMessage.Id);
                }
                else if (index == 3)
                {
                    services.GetService<ConfigHandler>().SetSAScrimMessage(signUpMessage.Id);
                }
                else if (index == 4)
                {
                    services.GetService<ConfigHandler>().SetSPScrimMessage(signUpMessage.Id);
                }
                else if (index == 5)
                {
                    services.GetService<ConfigHandler>().SetAUScrimMessage(signUpMessage.Id);
                }
                await Task.Delay(5000);
                await signUpMessage.AddReactionAsync(checkmark);
                await Task.Delay(10000);
                await signUpMessage.AddReactionAsync(start);
                await Task.Delay(500);
                await signUpMessage.AddReactionAsync(manualStart);
            }
        }

        private async Task DoScrimReaction(SocketReaction reaction, ISocketMessageChannel channel, ulong scrimChannel, ulong scrimMessage, IRole scrimRole, string region, int index)
        {
            if (reaction.Emote.Name == start.Name && guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == reaction.UserId))
            {
                var user = reaction.User.Value;
                var signUpMessage = await guild.GetTextChannel(scrimChannel).GetMessageAsync(scrimMessage) as IUserMessage;
                var dmMessage = await StartScrim(signUpMessage, scrimRole, tempLists[index], region, false);
                tempLists[index].Clear();
                if (dmMessage != "")
                {
                    var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (reaction.Emote.Name == manualStart.Name && guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == reaction.UserId))
            {
                var user = reaction.User.Value;
                var signUpMessage = await guild.GetTextChannel(scrimChannel).GetMessageAsync(scrimMessage) as IUserMessage;
                var dmMessage = await StartScrim(signUpMessage, scrimRole, tempLists[index], region, true);
                tempLists[index].Clear();
                if (dmMessage != "")
                {
                    var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (reaction.Emote.Name == emoteExit.Name && guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == reaction.UserId))
            {
                var signUpMessage = await guild.GetTextChannel(scrimChannel).GetMessageAsync(scrimMessage) as IUserMessage;
                await signUpMessage.DeleteAsync();
                var activeRole = scrimRole as SocketRole;
                foreach (var mem in tempLists[index])
                {
                    roleQueue.Enqueue((mem, activeRole, false));
                }
                tempLists[index].Clear();
                ScrimAdmins[index] = "";
                var builder = new EmbedBuilder()
                    .WithTitle("Scrim Dashboard")
                    .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                    $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                    .AddField("Max Scrim Size: ", maxScrim)
                    .WithColor(new Discord.Color(0xF5FF))
                    .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
                var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                await dashboardMessage.ModifyAsync(x => x.Embed = builder);
            }
            else if (reaction.Emote.Name == checkmark.Name)
            {
                if (!tempLists[index].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot && tempLists[index].Count() < maxScrim)
                {
                    tempLists[index].Add(reaction.User.Value);
                }
            }
        }

        private async Task DoDeleteScrimMessage(ulong active, int index)
        {
            var activeRole = guild.GetRole(active);
            foreach (var mem in tempLists[index])
            {
                roleQueue.Enqueue((mem, activeRole, false));
            }
            ScrimAdmins[index] = "";
            tempLists[index].Clear();
            var builder = new EmbedBuilder()
                .WithTitle("Scrim Dashboard")
                .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                .AddField("Max Scrim Size: ", maxScrim)
                .WithColor(new Discord.Color(0xF5FF))
                .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
            var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
            var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
            await dashboardMessage.ModifyAsync(x => x.Embed = builder);
        }

        private async Task handleReactionRemovedCheck(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id == services.GetService<ConfigHandler>().GetScrimMessage())//SCRIM GENERIC REMOVED
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var dmMessage = await RemoveScrimSignUp(user);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSignUpMessage())//SIGN UP REMOVED
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = reaction.User.Value;
                    var mess = await Unregister(user);
                    var dmChannel = await reaction.User.Value.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: mess);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEastScrimMessage() && reaction.Emote.Name == checkmark.Name)//EAST SCRIM SIGN UP
            {
                if (tempLists[0].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[0].Remove(reaction.User.Value);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWestScrimMessage() && reaction.Emote.Name == checkmark.Name)//WEST SCRIM SIGN UP
            {
                if (tempLists[1].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[1].Remove(reaction.User.Value);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEUScrimMessage() && reaction.Emote.Name == checkmark.Name)//EU SCRIM SIGN UP
            {
                if (tempLists[2].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[2].Remove(reaction.User.Value);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSAScrimMessage() && reaction.Emote.Name == checkmark.Name)//SA SCRIM SIGN UP
            {
                if (tempLists[3].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[3].Remove(reaction.User.Value);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSPScrimMessage() && reaction.Emote.Name == checkmark.Name)//SP SCRIM SIGN UP
            {
                if (tempLists[4].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[4].Remove(reaction.User.Value);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetAUScrimMessage() && reaction.Emote.Name == checkmark.Name)//AU SCRIM SIGN UP
            {
                if (tempLists[5].Any(x => x.Id == reaction.UserId) && !reaction.User.Value.IsBot)
                {
                    tempLists[5].Remove(reaction.User.Value);
                }
            }
        }

        public async Task HandleJoinedGuild(SocketGuildUser user)
        {
            var channel = await user.GetOrCreateDMChannelAsync();
            var message = "";
            var builder = new EmbedBuilder()
                .WithTitle("Welcome to Darwin Pro League!")
                .WithDescription("This is a hub for many Darwin Tournaments and scrims. In order to keep members organized, " +
                "please reply in **THIS** dm with the following information (with the !join command) : \n ```In-Game Name```")
                .WithColor(new Discord.Color(0x8169FB))
                .WithThumbnailUrl("https://i.imgur.com/TMiiPvl.png")
                .AddField("Example:", "!join lilscarecrow")
                .AddField("Other Command:", "!status")
                .AddField("Please choose a region by reacting to the first message in ", "[`#🌎region│scrim-selection`]")
                .AddField("After that you may react to the other message in ", "[`#🌎region│scrim-selection`] to get access to scrims.")
                .AddField("If you have any questions or encounter any problems", "DM lilscarecrow#5308 on Discord.");
            var embed = builder.Build();
            await channel.SendMessageAsync(embed: embed);
            Console.WriteLine("User: " + user.Username + " got the join message.");
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var dbUser = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Id);
                    if (dbUser != null)
                    {
                        message = "Member already in the database:" +
                            "```Name: " + dbUser.Name + "\nRegion: " + dbUser.Region + "\nSigned Up: " + dbUser.SignedUp + "\nChecked In: " + dbUser.CheckedIn + "```";
                    }
                    else
                    {
                        var discordTag =  user.Username + "#" + user.DiscriminatorValue;
                        dbUser = new Users
                        {
                            Id = user.Id,
                            DiscordTag = discordTag,
                            Name = user.Username,
                            Region = "XX",
                            SignedUp = false,
                            CheckedIn = false,
                            WaitList = false,
                            IsDirector = false
                        };
                        db.Users.Add(dbUser);
                        message = "User: " + user.Username + " was added to the database.";
                        await db.SaveChangesAsync();
                    }
                    Console.WriteLine(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            if (message.Channel.Id == 484517811159302148 && message.Attachments.Any())
            {
                await PerformOCR(message);
                return;
            }
            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            var context = new SocketCommandContext(client, message);
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
            {
                //await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        public async Task<Embed> AddMember(ulong id, string discordTag, string name)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        user.Name = name;
                        db.Users.Update(user);
                        em = new EmbedBuilder()
                            .WithTitle("You are already in the database")
                            .WithDescription("I changed your name with the following information:")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .AddField("Name: ", name)
                            .AddField("Region: ", user.Region)
                            .AddField("--TOURNAMENT INFO--", "⠀")
                            .AddField("Signed Up: ", user.SignedUp)
                            .AddField("Checked In: ", user.CheckedIn)
                            .AddField("Wait-Listed: ", user.WaitList)
                            .Build();
                    }
                    else
                    {
                        user = new Users
                        {
                            Id = id,
                            DiscordTag = discordTag,
                            Name = name,
                            Region = "XX",
                            SignedUp = false,
                            CheckedIn = false,
                            WaitList = false,
                            IsDirector = false
                        };
                        db.Users.Add(user);
                        em = new EmbedBuilder()
                            .WithTitle("Successfully entered into the database")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .AddField("Name: ", name)
                            .AddField("Region: ", user.Region)
                            .AddField("--TOURNAMENT INFO--", "⠀")
                            .AddField("Signed Up: ", user.SignedUp)
                            .AddField("Checked In: ", user.CheckedIn)
                            .AddField("Wait-Listed: ", user.WaitList)
                            .Build();
                    }
                    await db.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                            .WithTitle("An error has occured.")
                            .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> GetMember(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if(user != null)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("Successfully found your information")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .AddField("Name: ", user.Name)
                            .AddField("Region: ", user.Region)
                            .AddField("--TOURNAMENT INFO--", "⠀")
                            .AddField("Signed Up: ", user.SignedUp)
                            .AddField("Checked In: ", user.CheckedIn)
                            .AddField("Wait-Listed: ", user.WaitList)
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                             .WithTitle("An error has occured.")
                             .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                             .WithColor(new Discord.Color(0xFF0004))
                             .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                             .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberRegion(ulong id, string region, string regionName)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        user.Region = region;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully changed your region to " + regionName)
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                            .WithTitle("An error has occured.")
                            .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberSignUp(IUser iuser)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == iuser.Id);
                    var total = db.Users.Where(u => u.SignedUp).Count();
                    if (user != null && total < 100 && !user.IsDirector)
                    {
                        user.SignedUp = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully signed up for the upcoming tournament!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetSignUpRole()), true));
                    }
                    else if (total >= 100)
                    {
                        user.WaitList = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Registration is full.")
                            .WithDescription("But don't worry, you are now added to the wait list!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetWaitListRole()), true));
                    } 
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                            .WithTitle("An error has occured.")
                            .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberCheckIn(IUser iuser)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == iuser.Id);
                    if (user != null && user.SignedUp)
                    {
                        user.CheckedIn = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully checked in for the upcoming tournament!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetCheckInRole()), true));
                    }
                    else if (user != null && !user.SignedUp)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not signed up for the tournament.")
                            .WithDescription("Please DM an admin if you would like to play.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                            .WithTitle("An error has occured.")
                            .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberWaitList(IUser iuser)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == iuser.Id);
                    var usersCheckedIn = db.Users.Where(u => u.CheckedIn);
                    if (user != null && !user.WaitList)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You are not on the wait list.")
                            .WithDescription("")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else if (usersCheckedIn.Count() >= 100)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("Registration is full.")
                            .WithDescription("Please DM an admin if you would like to play.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else if (user != null && user.WaitList)
                    {
                        user.CheckedIn = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully checked in for the upcoming tournament!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetCheckInRole()), true));
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                           .WithTitle("An error has occured.")
                           .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                           .WithColor(new Discord.Color(0xFF0004))
                           .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                           .Build();
                }
            }
            return em;
        }

        public async Task<Embed> ClearSignUps()
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var users = db.Users.Where(u => u.SignedUp || u.WaitList || u.CheckedIn);
                    await users.ForEachAsync(u => 
                    {
                        u.SignedUp = false;
                        u.CheckedIn = false;
                        u.WaitList = false;
                        u.FirstGame = null;
                        u.SecondGame = null;
                        u.ThirdGame = null;
                        u.Total = null;
                        db.Users.Update(u);
                    });
                    var count = await db.SaveChangesAsync();
                    em = new EmbedBuilder()
                            .WithTitle("Successfully reset " + count + " records!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    var signedUpRole = guild.GetRole(services.GetService<ConfigHandler>().GetSignUpRole());
                    var checkInRole = guild.GetRole(services.GetService<ConfigHandler>().GetCheckInRole());
                    var waitListRole = guild.GetRole(services.GetService<ConfigHandler>().GetWaitListRole());
                    foreach (var user in signedUpRole.Members)
                    {
                        roleQueue.Enqueue((user, signedUpRole, false));
                    }
                    foreach (var user in checkInRole.Members)
                    {
                        roleQueue.Enqueue((user, checkInRole, false));
                    }
                    foreach (var user in waitListRole.Members)
                    {
                        roleQueue.Enqueue((user, waitListRole, false));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                           .WithTitle("An error has occured.")
                           .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                           .WithColor(new Discord.Color(0xFF0004))
                           .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                           .Build();
                }
            }
            return em;
        }

        public async Task<string> RefreshRegionSelection(RestUserMessage regionMessage)
        {
            string message = "RefreshRegion";
            var count = 0;
            var east = await regionMessage.GetReactionUsersAsync(emoteEast, 1000).FlattenAsync();
            var west = await regionMessage.GetReactionUsersAsync(emoteWest, 1000).FlattenAsync();
            var eu = await regionMessage.GetReactionUsersAsync(emoteEU, 1000).FlattenAsync();
            var sa = await regionMessage.GetReactionUsersAsync(emoteSA, 1000).FlattenAsync();
            var sp = await regionMessage.GetReactionUsersAsync(emoteSP, 1000).FlattenAsync();
            var au = await regionMessage.GetReactionUsersAsync(emoteAU, 1000).FlattenAsync();
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Successfully updated region for member(s):```");
                try
                {
                    builder.AppendLine("---NA---");
                    foreach (var regionUser in east)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if(user.Region != "NA")
                        {
                            user.Region = "NA";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    builder.AppendLine("---EU---");
                    foreach (var regionUser in eu)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.Region != "EU")
                        {
                            user.Region = "EU";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    builder.AppendLine("---WE---");
                    foreach (var regionUser in west)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.Region != "WE")
                        {
                            user.Region = "WE";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    builder.AppendLine("---SA---");
                    foreach (var regionUser in sa)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.Region != "SA")
                        {
                            user.Region = "SA";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    builder.AppendLine("---SP---");
                    foreach (var regionUser in sp)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.Region != "SP")
                        {
                            user.Region = "SP";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    builder.AppendLine("---AU---");
                    foreach (var regionUser in au)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == regionUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.Region != "AU")
                        {
                            user.Region = "AU";
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    await db.SaveChangesAsync();
                    builder.AppendLine("Total Updated: " + count + " ```");
                    message = builder.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> RefreshSignUpSelection(IEnumerable<IUser> signUpUsers)
        {
            string message = "RefreshSignUp";
            var count = 0;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Successfully updated sign ups for member(s):```");
                try
                {
                    foreach (var signUpUser in signUpUsers)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == signUpUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (!user.SignedUp)
                        {
                            user.SignedUp = true;
                            count++;
                            builder.AppendLine(user.Name);
                        }
                        db.Users.Update(user);
                    }
                    await db.SaveChangesAsync();
                    builder.AppendLine("Total Updated: " + count + " ```");
                    message = builder.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> RefreshCheckInSelection(IEnumerable<IUser> checkInUsers)
        {
            string message = "RefreshCheckIn";
            var count = 0;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Successfully updated check ins for member(s):```");
                try
                {
                    foreach (var checkInUser in checkInUsers)
                    {
                        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == checkInUser.Id);
                        if (user == null)
                        {
                            continue;
                        }
                        if (user.SignedUp && !user.CheckedIn)
                        {
                            user.CheckedIn = true;
                            count++;
                            db.Users.Update(user);
                            builder.AppendLine(user.Name);
                        }
                        else if (!user.SignedUp)
                        {
                            builder.AppendLine(user.Name + " --NOT SIGNED UP!");
                        }
                    }
                    await db.SaveChangesAsync();
                    builder.AppendLine("Total Updated: " + count + " ```");
                    message = builder.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<Embed> Unregister(IUser iuser)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == iuser.Id);
                    if (user != null && user.SignedUp)
                    {
                        user.SignedUp = false;
                        user.CheckedIn = false;
                        user.FirstGame = null;
                        user.SecondGame = null;
                        user.ThirdGame = null;
                        user.Total = null;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetSignUpRole()), false));
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetCheckInRole()), false));
                        roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetWaitListRole()), false));
                        em = new EmbedBuilder()
                            .WithTitle("Successfully unregistered for the upcoming tournament!")
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else if (user != null && (!user.SignedUp || !user.WaitList))
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not signed up for the upcoming tournament.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                           .WithTitle("An error has occured.")
                           .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                           .WithColor(new Discord.Color(0xFF0004))
                           .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                           .Build();
                }
            }
            return em;
        }

        public async Task<Embed> RemovePlayer(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        db.Users.Remove(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully removed " + user.Name)
                            .WithColor(new Discord.Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("That player is not registered in the database.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                           .WithTitle("An error has occured.")
                           .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                           .WithColor(new Discord.Color(0xFF0004))
                           .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                           .Build();
                }
            }
            return em;
        }

        public async Task<string> FindMember(string name)
        {
            string message = "Find";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var users = db.Users.Where(u => EF.Functions.Like(u.DiscordTag.ToLower(), "%" + name.ToLower() + "%") || EF.Functions.Like(u.Name.ToLower(), "%" + name.ToLower() + "%"));
                    if (users != null && users.Count() > 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine("Successfully found member(s):```");
                        await users.ForEachAsync(u => builder.AppendLine("Id = " + u.Id + "\nDiscordTag = " + u.DiscordTag + "\nName = " + u.Name + "\nRegion = " + u.Region));
                        builder.AppendLine(" ```");
                        message = builder.ToString();
                    }
                    else
                        message = "Could not find member: " + name + ".";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Crown(ulong id)
        {
            string message = "Crown";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    user.Champion +=1;
                    db.Users.Update(user);
                    await db.SaveChangesAsync();
                    message = "Congratulations to " + user.Name + " for becoming our new " + guild.GetRole(services.GetService<ConfigHandler>().GetChampionRole()).Mention + "!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Verify()
        {
            string message = "Verify";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    await guild.DownloadUsersAsync();
                    var missingUsers = guild.Users.Where(u => !db.Users.Any(x => x.Id == u.Id));
                    builder.AppendLine("```");
                    foreach (var user in missingUsers)
                    {
                        builder.AppendLine(user.Username + "#" + user.DiscriminatorValue);
                    }
                    builder.AppendLine("```");
                    message = builder.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Rebuild()
        {
            string message = "Rebuild";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    await guild.DownloadUsersAsync();
                    var signUps = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetSignUpChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetSignUpMessage()) as RestUserMessage;
                    signUps.GetReactionUsersAsync(checkmark, 200).ForEach(x =>
                    {
                        message += " " + x;
                        //db.Users.Where(u => u.Id == r.)
                    });
                    //var missingUsers = guild.Users.Where(u => !db.Users.Any(x => x.Id == u.Id));
                    //builder.AppendLine("```");
                    //foreach (var user in missingUsers)
                    //{
                    //    builder.AppendLine(user.Username + "#" + user.DiscriminatorValue);
                    //}
                    //builder.AppendLine("```");
                    //message = builder.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> GuildInfo()
        {
            string message = "Guild info could not be obtained";
            var builder = new StringBuilder();
            builder.Append("```");
            foreach (var role in guild.Roles)
            {
                builder.AppendLine(role.Name + " : " + role.Id);
            }
            builder.Append("```");
            message = builder.ToString();
            await Task.CompletedTask;
            return message;
        }

        public async Task<Embed> AddScrimSignUp(IUser iuser)
        {
            string message = "AddScrimSignUp";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == iuser.Id);
                    if (user != null && user.Region != "XX")
                    {
                        if(user.Region == "NA")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetEastScrimRole()), true));
                            message = "You may now join NA East Scrims in DPL!";
                        }
                        else if (user.Region == "WE")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetWestScrimRole()), true));
                            message = "You may now join NA West Scrims in DPL!";
                        }
                        else if (user.Region == "EU")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetEUScrimRole()), true));
                            message = "You may now join EU Scrims in DPL!";
                        }
                        else if (user.Region == "SA")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetSAScrimRole()), true));
                            message = "You may now join South America Scrims in DPL!";
                        }
                        else if (user.Region == "SP")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetSPScrimRole()), true));
                            message = "You may now join Singapore Scrims in DPL!";
                        }
                        else if (user.Region == "AU")
                        {
                            roleQueue.Enqueue((iuser, guild.GetRole(services.GetService<ConfigHandler>().GetAUScrimRole()), true));
                            message = "You may now join OCE Scrims in DPL!";
                        }
                    }
                    else
                    {
                        return new EmbedBuilder()
                            .WithTitle("You're not registered in the database or have not selected a region.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Discord.Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .AddField("select a region in DPL's [`#🌎region│scrim-selection`] channel and try to react again.", "⠀")
                            .Build();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EXCEPTION THROWN SCRIM FUNCTION: " + ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return new EmbedBuilder()
                    .WithTitle("Success!")
                    .WithDescription(message)
                    .WithColor(new Discord.Color(0x169400))
                    .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                    .Build();
        }

        public async Task<string> RemoveScrimSignUp(IUser iuser)
        {
            string message = "RemoveScrimSignUp";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = iuser as SocketGuildUser;
                    var roles = user.Roles.Where(x => x.Id == services.GetService<ConfigHandler>().GetEastScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetWestScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetEUScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetSAScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetSPScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetAUScrimRole());
                    foreach(var role in roles)
                    {
                        roleQueue.Enqueue((iuser, role, false));
                    }
                    message = "Scrim role removed. You can re-react to the checkmark to get the scrim role back at anytime.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            await Task.CompletedTask;
            return message;
        }

        public async Task<string> StartScrim(IUserMessage signUpMessage, IRole role, List<IUser> reactions, string region, bool partial)
        {
            string message = "";
            var builder = new StringBuilder();
            builder.AppendLine("```USERS SIGNED UP FOR " + region + ":");
            var counter = 1;
            if (!partial && reactions.Count() > 10)
            {
                reactions = reactions.GetRange(0, (reactions.Count() - reactions.Count() % 10));
            }
            foreach (var user in reactions)
            {
                builder.AppendLine(counter + " - " + user.Username);
                roleQueue.Enqueue((user, role, true));
                counter++;
            }
            builder.AppendLine("```");
            message = builder.ToString();
            await signUpMessage.RemoveAllReactionsAsync();
            await Task.Delay(5000);
            await signUpMessage.AddReactionAsync(emoteExit);
            return message;
        }

        public async Task<Embed> ScrimSize(int size)
        {
            Embed em;
            maxScrim = size;
            services.GetService<ConfigHandler>().SetMaxScrimSize(maxScrim);
            em = new EmbedBuilder()
                 .WithTitle("Max scrim size set to " + size + " players!")
                 .WithColor(new Discord.Color(0x169400))
                 .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                 .Build();
            var builder = new EmbedBuilder()
                    .WithTitle("Scrim Dashboard")
                    .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                    $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                    .AddField("Max Scrim Size: ", maxScrim)
                    .WithColor(new Discord.Color(0xF5FF))
                    .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
            var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
            var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
            await dashboardMessage.ModifyAsync(x => x.Embed = builder);
            return em;
        }

        public void RemoveActive()
        {
            var activeRoles = guild.Roles.Where(x => x.Id == services.GetService<ConfigHandler>().GetEastScrimActiveRole()
              || x.Id == services.GetService<ConfigHandler>().GetWestScrimActiveRole()
              || x.Id == services.GetService<ConfigHandler>().GetEUScrimActiveRole()
              || x.Id == services.GetService<ConfigHandler>().GetSAScrimActiveRole()
              || x.Id == services.GetService<ConfigHandler>().GetSPScrimActiveRole()
              || x.Id == services.GetService<ConfigHandler>().GetAUScrimActiveRole());
            foreach (var role in activeRoles)
            {
                foreach (var user in role.Members)
                {
                    roleQueue.Enqueue((user, role, false));
                }
            }
        }

        public async Task PerformOCR(SocketUserMessage message)
        {
            StringBuilder builder = new StringBuilder();
            var att = message.Attachments.First();
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), att.Filename).Replace(@"\", @"\\");
            WebClient webClient = new WebClient();
            Uri uri = new Uri(att.Url);
            await webClient.DownloadFileTaskAsync(uri, filePath);
            using (var loadedImage = new Bitmap(filePath))
            {
                var modImage = InvertImage(loadedImage);
                modImage = ResizeImage(modImage, modImage.Width * 6, modImage.Height * 6);
                using (var engine = new TesseractEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "eng", EngineMode.Default))
                {
                    Dictionary<string, Rect> foundWords = new Dictionary<string, Rect>();
                    using (var img = Pix.LoadFromMemory(ImageToByte(modImage)))
                    {
                        using (var page = engine.Process(img))
                        {
                            using (var iter = page.GetIterator())
                            {

                                iter.Begin();
                                do
                                {
                                    Rect symbolBounds;
                                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out symbolBounds))
                                    {
                                        var curText = iter.GetText(PageIteratorLevel.Word).Trim();
                                        Console.WriteLine("TEXT: " + curText + "  - POS: X1- " + symbolBounds.X1 + " X2- " + symbolBounds.X2 + " Y1- " + symbolBounds.Y1 + " Y2- " + symbolBounds.Y2);
                                        if (Regex.Matches(curText, @"[a-zA-Z]").Count() > 0)
                                        {
                                            if (foundWords.ContainsKey(curText))
                                            {
                                                foundWords[curText] = symbolBounds;
                                            }
                                            else
                                            {
                                                foundWords.Add(curText, symbolBounds);
                                            }
                                        }
                                    }
                                } while (iter.Next(PageIteratorLevel.Word));
                            }
                        }
                    }
                    if (!foundWords.ContainsKey("SURVIVED"))
                    {
                        Console.WriteLine("Could not find SURVIVED text.");
                    }
                    else if (!foundWords.ContainsKey("DAMAGE"))
                    {
                        Console.WriteLine("Could not find DAMAGE text.");
                    }
                    else if (!foundWords.ContainsKey("KILLS"))
                    {
                        Console.WriteLine("Could not find KILLS text.");
                    }
                    else
                    {
                        var survived = foundWords["SURVIVED"];//Get anchor elements needed for positioning
                        var damage = foundWords["DAMAGE"];
                        var kills = foundWords["KILLS"];
                        var survivedSize = survived.X2 - survived.X1;
                        var survivedDistance = survivedSize * 7.54;
                        var X1 = (int)Math.Floor(survived.X1 - survivedDistance);
                        var X2 = survived.X1 - (3 * survivedSize);
                        var Y1 = survived.Y2;
                        var Y2 = modImage.Height;
                        var namesCropped = CropImage(modImage, new Rectangle(X1, Y1, (X2 - X1), (Y2 - Y1)));
                        //namesCropped.Save(@"testNamesCropped.png", System.Drawing.Imaging.ImageFormat.Png);
                        var players = new List<string>();
                        var playerList = new List<Player>();
                        using (var subImg = Pix.LoadFromMemory(ImageToByte(namesCropped)))
                        {
                            using (var page = engine.Process(subImg))
                            {
                                players = page.GetText().Split().ToList();
                            }
                        }
                        engine.SetVariable("tessedit_char_whitelist", "0123456789:,-");
                        engine.SetVariable("load_system_dawg", "false");
                        engine.SetVariable("load_freq_dawg", "false");
                        foreach (var player in players)
                        {
                            if (foundWords.ContainsKey(player))//look for already saved bounding boxes
                            {
                                var word = foundWords[player];
                                var p = new Player();
                                p.name = player;
                                X1 = survived.X1;
                                X2 = survived.X2;
                                Y1 = word.Y1;
                                Y2 = word.Y2;
                                var tempCropped = CropImage(modImage, new Rectangle(X1, Y1, (X2 - X1), (Y2 - Y1)));
                                tempCropped.Save(@"testsurvCropped_" + player + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                using (var subImg = Pix.LoadFromMemory(ImageToByte(tempCropped)))
                                {
                                    using (var page = engine.Process(subImg))
                                    {
                                        p.survived = Regex.Replace(page.GetText(), @"\t|\n|\r", "");
                                    }
                                    if (p.survived.Trim() == "")
                                    {
                                        p.survived = "-";
                                    }
                                }

                                X1 = damage.X1;
                                X2 = damage.X2;
                                Y1 = word.Y1;
                                Y2 = word.Y2;
                                tempCropped = CropImage(modImage, new Rectangle(X1, Y1, (X2 - X1), (Y2 - Y1)));
                                tempCropped.Save(@"testdmgCropped_" + player + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                using (var subImg = Pix.LoadFromMemory(ImageToByte(tempCropped)))
                                {
                                    using (var page = engine.Process(subImg))
                                    {
                                        p.damage = Regex.Replace(page.GetText(), @"\t|\n|\r", "");
                                    }
                                    if (p.damage.Trim() == "")
                                    {
                                        using (var page = engine.Process(subImg, PageSegMode.SingleChar))
                                        {
                                            p.damage = Regex.Replace(page.GetText(), @"\t|\n|\r", "");
                                        }
                                    }
                                }

                                X1 = kills.X1;
                                X2 = kills.X2;
                                Y1 = word.Y1;
                                Y2 = word.Y2;
                                tempCropped = CropImage(modImage, new Rectangle(X1, Y1, (X2 - X1), (Y2 - Y1)));
                                tempCropped.Save(@"testkillsCropped_" + player + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                using (var subImg = Pix.LoadFromMemory(ImageToByte(tempCropped)))
                                {
                                    using (var page = engine.Process(subImg, PageSegMode.SingleChar))
                                    {
                                        p.kills = Regex.Replace(page.GetText(), @"\t|\n|\r", "");
                                        if (p.kills == "7")//manually set misread values to 1
                                        {
                                            p.kills = "1";
                                        }
                                    }
                                }
                                playerList.Add(p);
                            }
                        }
                        //Sanity Check
                        var killTotal = playerList.Sum(x => Int32.Parse(x.kills));
                        if (killTotal != playerList.Count() - 1)//Incorrect Kill Score (try and correct)
                        {
                            for (int i = 0; i < playerList.Count(); i++)
                            {
                                if (Int32.Parse(playerList[i].kills) == 1)
                                {
                                    playerList[i].kills = "7???";
                                    break;
                                }
                                if (i == 2)
                                {
                                    break;
                                }
                            }
                        }
                        builder.AppendLine("```");
                        foreach (var entry in playerList)
                        {
                            builder.AppendLine("NAME: " + entry.name);
                            builder.AppendLine("SURVIVED: " + entry.survived);
                            builder.AppendLine("DAMAGE: " + entry.damage);
                            builder.AppendLine("KILLS: " + entry.kills);
                            builder.AppendLine();
                        }
                        builder.AppendLine("```");
                        await message.Channel.SendMessageAsync(builder.ToString());
                    }
                }
            }
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static Bitmap InvertImage(Bitmap pic)
        {
            for (int y = 0; (y <= (pic.Height - 1)); y++)
            {
                for (int x = 0; (x <= (pic.Width - 1)); x++)
                {
                    System.Drawing.Color inv = pic.GetPixel(x, y);
                    inv = System.Drawing.Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    pic.SetPixel(x, y, inv);
                }
            }
            return pic;
        }

        public static Bitmap CropImage(Bitmap image, Rectangle region)
        {
            Bitmap target = new Bitmap(region.Width, region.Height);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(image, new Rectangle(0, 0, target.Width, target.Height), region, GraphicsUnit.Pixel);
            }
            return target;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
