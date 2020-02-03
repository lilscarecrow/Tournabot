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
        private List<List<string>> playerList = new List<List<string>>();
        private Queue<(ulong, ulong, bool)> roleQueue = new Queue<(ulong, ulong, bool)>();
        private string[] ScrimAdmins = { "", "", "", "", "", ""};
        private SocketGuild guild;
        private int maxScrim;
        private List<ulong>[] tempLists = { new List<ulong>(), new List<ulong>(), new List<ulong>(), new List<ulong>(), new List<ulong>(), new List<ulong>() };
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
            if(guild == null)
            {
                guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
            }
            for (int i = 0; i < 3; i++)
            {
                if(roleQueue.Count() > 0)
                {
                    var item = roleQueue.Dequeue();
                    Console.WriteLine("USER: " + item.Item1 + " ROLE: " + item.Item2 + " ADDED: " + item.Item3 + " TIMESTAMP: " + DateTime.Now);
                    if(guild.GetUser(item.Item1) == null)
                    {
                        Console.WriteLine("Cache Miss...Downloading Members..." + " TIMESTAMP: " + DateTime.Now);
                        await guild.DownloadUsersAsync();
                    }
                    if (item.Item3)
                    {
                        var user = guild.GetUser(item.Item1);
                        var role = guild.GetRole(item.Item2);
                        if (user != null && role != null)
                            await user.AddRoleAsync(role);
                        else
                            Console.WriteLine("User: " + user + " OR Role: " + role + " are NULL!" + " TIMESTAMP: " + DateTime.Now);
                    }
                    else
                    {
                        var user = guild.GetUser(item.Item1);
                        var role = guild.GetRole(item.Item2);
                        if (user != null && role != null)
                            await user.RemoveRoleAsync(role);
                        else
                            Console.WriteLine("User: " + user + " OR Role: " + role + " are NULL!" + " TIMESTAMP: " + DateTime.Now);
                    }
                }
                else
                {
                    break;
                }
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
                var user = await channel.GetUserAsync(reaction.UserId);
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
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberSignUp(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetCheckInMessage())//CHECK IN
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberCheckIn(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWaitListMessage())//WAIT LIST
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberWaitList(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetScrimMessage())//SCRIM GENERIC
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddScrimSignUp(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetDashboardMessage())//SCRIM DASHBOARD
            {
                if (reaction.Emote.Name == emoteEast.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetEastScrimChannel(), 
                        services.GetService<ConfigHandler>().GetEastScrimRole(), 0);
                }
                else if (reaction.Emote.Name == emoteWest.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetWestScrimChannel(),
                        services.GetService<ConfigHandler>().GetWestScrimRole(), 1);
                }
                else if (reaction.Emote.Name == emoteEU.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetEUScrimChannel(),
                        services.GetService<ConfigHandler>().GetEUScrimRole(), 2);
                }
                else if (reaction.Emote.Name == emoteSA.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetSAScrimChannel(),
                        services.GetService<ConfigHandler>().GetSAScrimRole(), 3);
                }
                else if (reaction.Emote.Name == emoteSP.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetSPScrimChannel(),
                        services.GetService<ConfigHandler>().GetSPScrimRole(), 4);
                }
                else if (reaction.Emote.Name == emoteAU.Name)
                {
                    await DoScrimMessage(reaction, channel, services.GetService<ConfigHandler>().GetAUScrimChannel(),
                        services.GetService<ConfigHandler>().GetAUScrimRole(), 5);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEastScrimMessage())//EAST SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetEastScrimChannel(), 
                    services.GetService<ConfigHandler>().GetEastScrimMessage(), services.GetService<ConfigHandler>().GetEastScrimActiveRole(), "EAST", 0);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWestScrimMessage())//WEST SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetWestScrimChannel(),
                    services.GetService<ConfigHandler>().GetWestScrimMessage(), services.GetService<ConfigHandler>().GetWestScrimActiveRole(), "WEST", 1);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEUScrimMessage())//EU SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetEUScrimChannel(),
                    services.GetService<ConfigHandler>().GetEUScrimMessage(), services.GetService<ConfigHandler>().GetEUScrimActiveRole(), "EU", 2);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSAScrimMessage())//SA SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetSAScrimChannel(),
                    services.GetService<ConfigHandler>().GetSAScrimMessage(), services.GetService<ConfigHandler>().GetSAScrimActiveRole(), "SA", 3);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSPScrimMessage())//SP SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetSPScrimChannel(),
                    services.GetService<ConfigHandler>().GetSPScrimMessage(), services.GetService<ConfigHandler>().GetSPScrimActiveRole(), "SP", 4);
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetAUScrimMessage())//AU SCRIM SIGN UP
            {
                await DoScrimReaction(reaction, channel, services.GetService<ConfigHandler>().GetAUScrimChannel(),
                    services.GetService<ConfigHandler>().GetAUScrimMessage(), services.GetService<ConfigHandler>().GetAUScrimActiveRole(), "AU", 5);
            }
        }

        private async Task DoScrimMessage(SocketReaction reaction, ISocketMessageChannel channel, ulong scrimChannel, ulong scrimRole, int index)
        {
            var user = await channel.GetUserAsync(reaction.UserId);
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
                    .WithColor(new Color(0xF5FF))
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
                    .WithColor(new Color(0xD3FF))
                    .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                var chan = client.GetChannel(scrimChannel) as SocketTextChannel;
                var signUpMessage = await chan.SendMessageAsync(text: guild.GetRole(scrimRole).Mention, embed: builder);
                services.GetService<ConfigHandler>().SetEastScrimMessage(signUpMessage.Id);
                await Task.Delay(5000);
                await signUpMessage.AddReactionAsync(checkmark);
                await Task.Delay(10000);
                await signUpMessage.AddReactionAsync(start);
                await Task.Delay(500);
                await signUpMessage.AddReactionAsync(manualStart);
            }
        }

        private async Task DoScrimReaction(SocketReaction reaction, ISocketMessageChannel channel, ulong scrimChannel, ulong scrimMessage, ulong scrimRole, string region, int index)
        {
            if (reaction.Emote.Name == start.Name && guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == reaction.UserId))
            {
                var user = await channel.GetUserAsync(reaction.UserId);
                var signUpMessage = await guild.GetTextChannel(scrimChannel).GetMessageAsync(scrimMessage) as IUserMessage;
                var dmMessage = await StartScrim(user.Id, signUpMessage, scrimRole, tempLists[index], region, false);
                tempLists[index].Clear();
                if (dmMessage != "")
                {
                    var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (reaction.Emote.Name == manualStart.Name && guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == reaction.UserId))
            {
                var user = await channel.GetUserAsync(reaction.UserId);
                var signUpMessage = await guild.GetTextChannel(scrimChannel).GetMessageAsync(scrimMessage) as IUserMessage;
                var dmMessage = await StartScrim(user.Id, signUpMessage, scrimRole, tempLists[index], region, true);
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
                var activeRole = guild.GetRole(scrimRole);
                foreach (var user in activeRole.Members)
                {
                    roleQueue.Enqueue((user.Id, activeRole.Id, false));
                }
                ScrimAdmins[index] = "";
                var builder = new EmbedBuilder()
                    .WithTitle("Scrim Dashboard")
                    .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                    $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                    .AddField("Max Scrim Size: ", maxScrim)
                    .WithColor(new Color(0xF5FF))
                    .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
                var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                await dashboardMessage.ModifyAsync(x => x.Embed = builder);
            }
            else if (reaction.Emote.Name == checkmark.Name)
            {
                if (!tempLists[index].Contains(reaction.UserId) && !reaction.User.Value.IsBot && tempLists[index].Count() < maxScrim)
                {
                    tempLists[index].Add(reaction.UserId);
                }
            }
        }

        private async Task DoDeleteScrimMessage(ulong active, int index)
        {
            var activeRole = guild.GetRole(active);
            foreach (var user in activeRole.Members)
            {
                roleQueue.Enqueue((user.Id, activeRole.Id, false));
            }
            ScrimAdmins[index] = "";
            var builder = new EmbedBuilder()
                .WithTitle("Scrim Dashboard")
                .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                .AddField("Max Scrim Size: ", maxScrim)
                .WithColor(new Color(0xF5FF))
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
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await RemoveScrimSignUp(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSignUpMessage())//SIGN UP REMOVED
            {
                if (reaction.Emote.Name == checkmark.Name)
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var mess = await Unregister(user.Id);
                    var dmChannel = await reaction.User.Value.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: mess);
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
                .WithColor(new Color(0x8169FB))
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
                            .WithColor(new Color(0x169400))
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
                            .WithColor(new Color(0x169400))
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
                            .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0x169400))
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
                            .WithColor(new Color(0xFF0004))
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
                             .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberSignUp(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    var total = db.Users.Where(u => u.SignedUp).Count();
                    if (user != null && total < 100 && !user.IsDirector)
                    {
                        user.SignedUp = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully signed up for the upcoming tournament!")
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetSignUpRole(), true));
                    }
                    else if (total >= 100)
                    {
                        user.WaitList = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Registration is full.")
                            .WithDescription("But don't worry, you are now added to the wait list!")
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetWaitListRole(), true));
                    } 
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberCheckIn(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null && user.SignedUp)
                    {
                        user.CheckedIn = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        em = new EmbedBuilder()
                            .WithTitle("Successfully checked in for the upcoming tournament!")
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetCheckInRole(), true));
                    }
                    else if (user != null && !user.SignedUp)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not signed up for the tournament.")
                            .WithDescription("Please DM an admin if you would like to play.")
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                }
            }
            return em;
        }

        public async Task<Embed> AddMemberWaitList(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    var usersCheckedIn = db.Users.Where(u => u.CheckedIn);
                    if (usersCheckedIn.Count() >= 100)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("Registration is full.")
                            .WithDescription("Please DM an admin if you would like to play.")
                            .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                        roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetCheckInRole(), true));
                    }
                    else if (user != null && !user.WaitList)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You are not on the wait list.")
                            .WithDescription("Please DM an admin if you would like to play.")
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                           .WithColor(new Color(0xFF0004))
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
                    var users = db.Users.Where(u => u.SignedUp);
                    await users.ForEachAsync(u => 
                    {
                        u.SignedUp = false;
                        u.CheckedIn = false;
                        u.FirstGame = null;
                        u.SecondGame = null;
                        u.ThirdGame = null;
                        u.Total = null;
                        db.Users.Update(u);
                    });
                    var count = await db.SaveChangesAsync();
                    em = new EmbedBuilder()
                            .WithTitle("Successfully reset " + count + " records!")
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    var signedUpRole = guild.GetRole(services.GetService<ConfigHandler>().GetSignUpRole());
                    var checkInRole = guild.GetRole(services.GetService<ConfigHandler>().GetCheckInRole());
                    var waitListRole = guild.GetRole(services.GetService<ConfigHandler>().GetWaitListRole());
                    foreach (var user in signedUpRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, signedUpRole.Id, false));
                    }
                    foreach (var user in checkInRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, checkInRole.Id, false));
                    }
                    foreach (var user in waitListRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, waitListRole.Id, false));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    em = new EmbedBuilder()
                           .WithTitle("An error has occured.")
                           .WithDescription("Please DM lilscarecrow#5308 on Discord.")
                           .WithColor(new Color(0xFF0004))
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

        public async Task<Embed> Unregister(ulong id)
        {
            Embed em;
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
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
                        em = new EmbedBuilder()
                            .WithTitle("Successfully unregistered for the upcoming tournament!")
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else if (user != null && !user.SignedUp)
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not signed up for the upcoming tournament.")
                            .WithColor(new Color(0xFF0004))
                            .WithThumbnailUrl("https://www.freeiconspng.com/uploads/hd-error-photo-transparent-background-19.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("You're not registered in the database.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                           .WithColor(new Color(0xFF0004))
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
                            .WithColor(new Color(0x169400))
                            .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                            .Build();
                    }
                    else
                    {
                        em = new EmbedBuilder()
                            .WithTitle("That player is not registered in the database.")
                            .WithColor(new Color(0xFF0004))
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
                           .WithColor(new Color(0xFF0004))
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

        public async Task<string> GetRoster()
        {
            string message = "Roster";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    var directors = db.Users.Where(u => u.IsDirector);
                    var signedUp = db.Users.Where(u => u.SignedUp);
                    var checkedIn = db.Users.Where(u => u.CheckedIn);
                    builder.AppendLine("Directors: ``` ");
                    await directors.ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: NA```");
                    await signedUp.Where(u => u.Region == "NA").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: EU``` ");
                    await signedUp.Where(u => u.Region == "EU").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: WE``` ");
                    await signedUp.Where(u => u.Region == "WE").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: SA```");
                    await signedUp.Where(u => u.Region == "SA").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: SP```");
                    await signedUp.Where(u => u.Region == "SP").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: AU```");
                    await signedUp.Where(u => u.Region == "AU").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Signed Up: No Region``` ");
                    await signedUp.Where(u => u.Region == "XX").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Checked In: NA``` ");
                    await checkedIn.Where(u => u.Region == "NA").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Checked In: EU``` ");
                    await checkedIn.Where(u => u.Region == "EU").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Checked In: WE``` ");
                    await checkedIn.Where(u => u.Region == "WE").ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.AppendLine("```Checked In: No Region``` ");
                    await checkedIn.Where(u => u.Region == "XX").ForEachAsync(u => builder.AppendLine(u.Name));
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

        public async Task<Embed> AddScrimSignUp(ulong id)
        {
            string message = "AddScrimSignUp";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null && user.Region != "XX")
                    {
                        if(user.Region == "NA")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetEastScrimRole(), true));
                            message = "You may now join NA East Scrims in DPL!";
                        }
                        else if (user.Region == "WE")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetWestScrimRole(), true));
                            message = "You may now join NA West Scrims in DPL!";
                        }
                        else if (user.Region == "EU")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetEUScrimRole(), true));
                            message = "You may now join EU Scrims in DPL!";
                        }
                        else if (user.Region == "SA")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetSAScrimRole(), true));
                            message = "You may now join South America Scrims in DPL!";
                        }
                        else if (user.Region == "SP")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetSPScrimRole(), true));
                            message = "You may now join Singapore Scrims in DPL!";
                        }
                        else if (user.Region == "AU")
                        {
                            roleQueue.Enqueue((id, services.GetService<ConfigHandler>().GetAUScrimRole(), true));
                            message = "You may now join OCE Scrims in DPL!";
                        }
                    }
                    else
                    {
                        return new EmbedBuilder()
                            .WithTitle("You're not registered in the database or have not selected a region.")
                            .WithDescription("Make sure to do the `!join <IN GAME NAME>` command in this DM.")
                            .WithColor(new Color(0xFF0004))
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
                    .WithColor(new Color(0x169400))
                    .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                    .Build();
        }

        public async Task<string> RemoveScrimSignUp(ulong id)
        {
            string message = "RemoveScrimSignUp";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    if(guild.GetUser(id) == null)
                    {
                        await guild.DownloadUsersAsync();
                    }
                    var roles = guild.GetUser(id)?.Roles.Where(x => x.Id == services.GetService<ConfigHandler>().GetEastScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetWestScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetEUScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetSAScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetSPScrimRole()
                        || x.Id == services.GetService<ConfigHandler>().GetAUScrimRole());
                    foreach(var role in roles)
                    {
                        roleQueue.Enqueue((id, role.Id, false));
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

        public async Task<string> StartScrim(ulong userId, IUserMessage signUpMessage, ulong roleId, List<ulong> reactions, string region, bool partial)
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
                if (guild.GetUser(user) == null)
                {
                    await guild.DownloadUsersAsync();
                }
                roleQueue.Enqueue((user, roleId, true));
                builder.AppendLine(counter + " - " + guild.GetUser(user)?.Username);
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
                 .WithColor(new Color(0x169400))
                 .WithThumbnailUrl("https://cdn1.iconfinder.com/data/icons/interface-elements/32/accept-circle-512.png")
                 .Build();
            var builder = new EmbedBuilder()
                    .WithTitle("Scrim Dashboard")
                    .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {ScrimAdmins[0]}\n <:cali:663097025033666560> WEST: {ScrimAdmins[1]}" +
                    $"\n :flag_eu: EU: {ScrimAdmins[2]}\n :flag_br: SA: {ScrimAdmins[3]}\n :flag_au: OCE: {ScrimAdmins[4]}\n :flag_sg: SP: {ScrimAdmins[5]}")
                    .AddField("Max Scrim Size: ", maxScrim)
                    .WithColor(new Color(0xF5FF))
                    .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
            var dashChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel());
            var dashboardMessage = await dashChannel.GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
            await dashboardMessage.ModifyAsync(x => x.Embed = builder);
            return em;
        }
    }
}
