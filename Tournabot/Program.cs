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
        private string EastScrimAdmin = null;
        private string WestScrimAdmin = null;
        private string EUScrimAdmin = null;
        private string SAScrimAdmin = null;
        private string SPScrimAdmin = null;
        private string AUScrimAdmin = null;
        private SocketGuild guild;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            client.MessageReceived += HandleCommand;
            client.UserJoined += HandleJoinedGuild;
            client.ReactionAdded += HandleReaction;
            client.UserLeft += HandleLeaveGuild;
            client.Log += Log;
            var timer = new Timer(4000);
            timer.Elapsed += AddReactions;

            timer.Enabled = true;
            commands = new CommandService();
            services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ConfigHandler>()
                .BuildServiceProvider();
            await services.GetService<ConfigHandler>().PopulateConfig();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, services.GetService<ConfigHandler>().GetToken());
            await client.StartAsync();
            guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
            await Task.Delay(-1);
        }

        public CommandService GetCommands()
        {
            return commands;
        }

        private async void AddReactions(object source, ElapsedEventArgs e)
        {
            for(int i = 0; i < 3; i++)
            {
                if(roleQueue.Count() > 0)
                {
                    var item = roleQueue.Dequeue();
                    if(item.Item3)
                        await guild.GetUser(item.Item1).AddRoleAsync(guild.GetRole(item.Item2));
                    else
                        await guild.GetUser(item.Item1).RemoveRoleAsync(guild.GetRole(item.Item2));
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

        private async Task handleReactionCheck(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (message.Id == services.GetService<ConfigHandler>().GetRegionMessage())//REGION
            {
                var user = await channel.GetUserAsync(reaction.UserId);
                if (reaction.Emote.Name == "🇺🇸")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "NA");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as East..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇪🇺")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "EU");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as EU..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "cali")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "WE");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as West..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇧🇷")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "SA");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as South America..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇸🇬")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "SP");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as Singapore..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇦🇺")
                {
                    var sqlmessage = await AddMemberRegion(user.Id, "AU");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as OCE..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSignUpMessage())//SIGN UP
            {
                if (reaction.Emote.Name == "✅")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberSignUp(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetCheckInMessage())//CHECK IN
            {
                if (reaction.Emote.Name == "✅")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberCheckIn(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWaitListMessage())//WAIT LIST
            {
                if (reaction.Emote.Name == "✅")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddMemberWaitList(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetScrimMessage())//SCRIM GENERIC
            {
                if (reaction.Emote.Name == "✅")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmMessage = await AddScrimSignUp(user.Id);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetDashboardMessage())//SCRIM DASHBOARD
            {
                var user = await channel.GetUserAsync(reaction.UserId);
                if (reaction.Emote.Name == "🇺🇸")
                {
                    if(EastScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + EastScrimAdmin);
                    }
                    else
                    {
                        EastScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetEastScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetEastScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
                else if (reaction.Emote.Name == "🇪🇺")
                {
                    if (EUScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + EUScrimAdmin);
                    }
                    else
                    {
                        EUScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetEUScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetEUScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
                else if (reaction.Emote.Name == "cali")
                {
                    if (WestScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + WestScrimAdmin);
                    }
                    else
                    {
                        WestScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetWestScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetWestScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
                else if (reaction.Emote.Name == "🇧🇷")
                {
                    if (SAScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + SAScrimAdmin);
                    }
                    else
                    {
                        SAScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetSAScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetSAScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
                else if (reaction.Emote.Name == "🇸🇬")
                {
                    if (SPScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + SPScrimAdmin);
                    }
                    else
                    {
                        SPScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetSPScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetSPScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
                else if (reaction.Emote.Name == "🇦🇺")
                {
                    if (AUScrimAdmin != null)
                    {
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        var dmChannel = await user.GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Scrim is already running with Scrim Admin: " + AUScrimAdmin);
                    }
                    else
                    {
                        AUScrimAdmin = user.Username;
                        var builder = new EmbedBuilder()
                            .WithTitle("Scrim Dashboard")
                            .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                            $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                            .WithColor(new Color(0xF5FF))
                            .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                        await message.Value.ModifyAsync(x => x.Embed = builder);
                        await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        builder = new EmbedBuilder()
                            .WithTitle("Scrim Signup")
                            .WithDescription("Click the :white_check_mark: to sign up for the next set!! \n\n\n For Scrim Organizers: click the <:start:663144594401132603> to start the scrim.")
                            .WithColor(new Color(0xD3FF))
                            .WithThumbnailUrl("https://i.imgur.com/A0VNXkg.png").Build();
                        var chan = client.GetChannel(services.GetService<ConfigHandler>().GetAUScrimChannel()) as SocketTextChannel;
                        var signUpMessage = await chan.SendMessageAsync(embed: builder);
                        services.GetService<ConfigHandler>().SetAUScrimMessage(signUpMessage.Id);
                        var emote = new Emoji("✅");
                        await signUpMessage.AddReactionAsync(emote);
                        await Task.Delay(10000);
                        var emote2 = Emote.Parse("<:start:663144594401132603>");
                        await signUpMessage.AddReactionAsync(emote2);
                    }
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEastScrimMessage())//EAST SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetEastScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetEastScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetEastScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetEastScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetEastScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetEastScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    EastScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetWestScrimMessage())//WEST SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetWestScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetWestScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetWestScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetWestScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetWestScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetWestScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    WestScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetEUScrimMessage())//EU SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetEUScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetEUScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetEUScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetEUScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetEUScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetEUScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    EUScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSAScrimMessage())//SA SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetSAScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetSAScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetSAScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetSAScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetSAScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetSAScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    SAScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSPScrimMessage())//SP SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetSPScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetSPScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetSPScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetSPScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetSPScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetSPScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    SPScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetAUScrimMessage())//AU SCRIM SIGN UP
            {
                if (reaction.Emote.Name == "start")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetAUScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetAUScrimMessage()) as IUserMessage;
                    var dmMessage = await StartScrim(user.Id, signUpMessage, services.GetService<ConfigHandler>().GetAUScrimActiveRole());
                    if (dmMessage != "")
                    {
                        var dmChannel = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminLogsChannel());
                        await dmChannel.SendMessageAsync(dmMessage);
                    }
                }
                else if (reaction.Emote.Name == "❌")
                {
                    var signUpMessage = await guild.GetTextChannel(services.GetService<ConfigHandler>().GetAUScrimChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetAUScrimMessage()) as IUserMessage;
                    await signUpMessage.DeleteAsync();
                    var activeRole = guild.GetRole(services.GetService<ConfigHandler>().GetAUScrimActiveRole());
                    foreach (var user in activeRole.Members)
                    {
                        roleQueue.Enqueue((user.Id, activeRole.Id, false));
                    }
                    AUScrimAdmin = null;
                    var builder = new EmbedBuilder()
                        .WithTitle("Scrim Dashboard")
                        .WithDescription($"Click the region you would like to start a scrim for.\n :flag_us: EAST: {EastScrimAdmin}\n <:cali:663097025033666560> WEST: {WestScrimAdmin}" +
                        $"\n :flag_eu: EU: {EUScrimAdmin}\n :flag_br: SA: {SAScrimAdmin}\n :flag_au: OCE: {AUScrimAdmin}\n :flag_sg: SP: {SPScrimAdmin}")
                        .WithColor(new Color(0xF5FF))
                        .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
                    var dashboardMessage = guild.GetTextChannel(services.GetService<ConfigHandler>().GetScrimAdminChannel()).GetMessageAsync(services.GetService<ConfigHandler>().GetDashboardMessage()) as IUserMessage;
                    await dashboardMessage.ModifyAsync(x => x.Embed = builder);
                }
            }
        }

        public async Task HandleJoinedGuild(SocketGuildUser user)
        {
            //var OwnerChannel = await user.Guild.Owner.GetOrCreateDMChannelAsync();
            var channel = await user.GetOrCreateDMChannelAsync();
            var message = "Hello! Welcome to The Darwin Elite. This is a hub for many Darwin Tournaments to come! " +
                "In order to keep members organized, please reply in THIS dm channel with the following information (with the `!join` command): \n" +
                "```In-game Name```\n" +
                "Example:\n" +
                "```!join lilscarecrow```\n" +
                "Other commands:\n" +
                "```!status\n" +
                "!unregister```\n" +
                "If you have any questions or encounter any problems, please DM lilscarecrow#5308 on Discord.";
            await channel.SendMessageAsync(message);
            // await OwnerChannel.SendMessageAsync(message);
            Console.WriteLine("User: " + user.Username + " got the join message.");
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

        public async Task<string> AddMember(ulong id, string discordTag, string name)
        {
            string message = "Add";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        message = "You are already in the database but I changed your name with the following information:" +
                            "```Name: " + name + "\nRegion: " + user.Region + "\nSigned Up: " + user.SignedUp + "\nChecked In: " + user.CheckedIn + "```";
                        user.Name = name;
                        db.Users.Update(user);
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
                        message = "Successfully entered into the database!";
                    }
                    await db.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> GetMember(ulong id)
        {
            string message = "Get";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if(user != null)
                        message = "```Name: " + user.Name + "\nRegion: " + user.Region + "\nSigned Up: " + user.SignedUp + "\nChecked In: " + user.CheckedIn + "\nWait List: " + user.WaitList + "\nDirector: " + user.IsDirector +  "```";
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> AddMemberRegion(ulong id, string region)
        {
            string message = "Region";
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
                        message = "Successfully changed your region!";
                    }
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> AddMemberSignUp(ulong id)
        {
            string message = "SignUp";
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
                        message = "Successfully signed up for the upcoming tournament!";
                    }
                    else if (user != null && user.IsDirector)
                        message = "You are a director, silly! You can't register!";
                    else if (total >= 100)
                    {
                        user.WaitList = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Registration is full. You are now added to the wait list.";
                    } 
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> AddMemberCheckIn(ulong id)
        {
            string message = "CheckIn";
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
                        message = "Successfully checked in for the upcoming tournament!";
                    }
                    else if (user != null && !user.SignedUp)
                        message = "You are not signed up for the tournament.";
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> AddMemberWaitList(ulong id)
        {
            string message = "WaitList";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    var usersCheckedIn = db.Users.Where(u => u.CheckedIn);
                    if(usersCheckedIn.Count() >= 100)
                        message = "Registration is full.";
                    else if (user != null && user.WaitList)
                    {
                        user.CheckedIn = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Successfully checked in for the upcoming tournament!";
                    }
                    else if (user != null && !user.WaitList)
                        message = "You are not on the wait list.";
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> ClearSignUps()
        {
            string message = "Clear";
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
                    message = "Successfully reset " + count + " records!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> RefreshRegionSelection(IEnumerable<IUser> east, IEnumerable<IUser> eu, IEnumerable<IUser> west)
        {
            string message = "RefreshRegion";
            var count = 0;
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
                    foreach (var regionUser in west)
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
                    foreach (var regionUser in west)
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
                    foreach (var regionUser in west)
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

        public async Task<string> Unregister(ulong id)
        {
            string message = "Unregister";
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
                        message = "Successfully unregistered for the upcoming tournament.";
                    }
                    else if (user != null && !user.SignedUp)
                        message = "You are not signed up for the tournament.";
                    else
                        message = "You are not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> RemovePlayer(ulong id)
        {
            string message = "Remove";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        db.Users.Remove(user);
                        await db.SaveChangesAsync();
                        message = "Successfully removed " + user.Name;
                    }
                    else
                        message = "That player is not registered in the database.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
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
                        await users.ForEachAsync(u => builder.AppendLine("Id = " + u.Id + "\nDiscordTag = " + u.DiscordTag + "\nName = " + u.Name));
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

        public async Task<string> AddDirector(ulong id)
        {
            string message = "AddDir";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        user.IsDirector = true;
                        user.SignedUp = false;
                        user.CheckedIn = false;
                        user.FirstGame = null;
                        user.SecondGame = null;
                        user.ThirdGame = null;
                        user.Total = null;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Successfully updated member " + user.Name + " to director.";
                    }
                    else
                        message = "Could not find member.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> RemoveDirector(ulong id)
        {
            string message = "RemoveDir";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                    {
                        user.IsDirector = false;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Successfully removed member " + user.Name + " from director.";
                    }
                    else
                        message = "Could not find member.";
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

        public async Task<string> CreateBrackets()
        {
            string message = "Brackets";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var directorMessage = "Hello! I'm here to help you in setting up your matches! Here is the list of commands you can use:\n" +
                        "Use this to send your players the matchcode and/or a message. It will dm them directly!\n Example:\n`!sendCode This is your match code: AHF4. " +
                        "Good Luck and have fun!` ```!sendCode *matchcode*``` This will give you a list of the players in your match.```!overview```" +
                        "Change a single score for a player. This should only be used to correct a score AFTER you entered the scores using the `!score *scores*` command." +
                        "```!correct *newScore* *name* ``` THIS IS THE MOST IMPORTANT ONE! DO NOT MESS THIS ONE UP!!! Scores will be entered on the google spreadsheet (Name, Placement, Kills, Score).\n" +
                        "Only enter the SCORE value for the player in the order they appear on this ->`!overview`<- screen. Enter it in the following way: ```!score 100,50,25,75,50,50,100,125,150,25```";
                    string[] matches = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "Finals" };
                    StringBuilder builder = new StringBuilder();
                    var players = db.Users.Where(u => u.CheckedIn && !u.IsDirector).OrderBy(u => Guid.NewGuid());//Randomize players
                    var buckets = (int) Math.Ceiling(players.Count() / 10.0);
                    var finalsDirector = guild.Users.FirstOrDefault(u => u.Roles.Any(x => x.Id == services.GetService<ConfigHandler>().GetFinalsDirectorRole()));
                    var directors = db.Users.Where(u => u.IsDirector && u.Id != finalsDirector.Id).OrderBy(u => Guid.NewGuid()).Take(buckets);//Take number of directors needed randomly
                    //await db.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE public.\"Directors\"");
                    if (buckets == 1)//FINALS
                    {
                        if (finalsDirector == null)
                            throw new Exception("Can't find Finals Director.");
                        var finalsDirectorDb = await db.Users.SingleOrDefaultAsync(u => u.Id == finalsDirector.Id);
                        if (finalsDirectorDb == null)
                            throw new Exception("Can't find Finals Director in DB.");
                        var dir = new Directors
                        {
                            Id = finalsDirectorDb.Id,
                            DirectorName = finalsDirectorDb.Name,
                            //MatchName = matches[10],
                            Submitted = false
                        };
                        builder.Append("Finals Director:```" + dir.DirectorName + "```Finalists:");
                        builder.AppendLine("```");
                        db.Directors.Add(dir);
                        foreach(var user in players)
                        {
                            builder.AppendLine(user.Name);
                        }
                        builder.AppendLine("```");
                        var dirDm = await finalsDirector.GetOrCreateDMChannelAsync();
                        await dirDm.SendMessageAsync(directorMessage);
                    }
                    else
                    {
                        var count = 0;
                        List<Directors> directorList = new List<Directors>();
                        await directors.ForEachAsync(u =>
                        {
                            if (count < buckets)
                            {
                                var user = guild.GetUser(u.Id);
                                var dir = new Directors
                                {
                                    Id = u.Id,
                                    DirectorName = u.Name,
                                    //MatchId = roles[count].Id,
                                    Submitted = false
                                };
                                db.Directors.Add(dir);
                                directorList.Add(dir);
                                var dirDm = guild.GetUser(u.Id).GetOrCreateDMChannelAsync().Result.SendMessageAsync(directorMessage);
                                playerList.Add(new List<string>());
                                count++;
                            }
                        });
                        count = 0;
                        foreach(var user in players)
                        {
                            //Console.WriteLine(user.Name + " ROLE: " + roles[count].Name);
                            //await guild.GetUser(user.Id).AddRoleAsync(roles[count]);
                            playerList[count].Add(user.Name);
                            count++;
                            count %= buckets;
                        }
                        for (count = 0; count < buckets; count++)
                        {
                            //builder.Append(roles[count].Name + " Director:```" + directorList[count].DirectorName + "```Players:");
                            builder.AppendLine("```");
                            playerList[count].Sort();
                            foreach(var user in playerList[count])
                            {
                                builder.AppendLine(user);
                            }
                            builder.AppendLine(" ```");
                        }
                    }
                    message = builder.Length == 0 ? "No data to return." : builder.ToString();
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> CalculateScores(int numPlayers)
        {
            string message = "Calculate";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    var players = db.Users.Where(u => u.CheckedIn && u.Total != null).OrderByDescending(u => u.Total);
                    var playersNotScored = db.Users.Where(u => u.CheckedIn && u.Total == null);
                    if (numPlayers > 0)
                    {
                        builder.Append("Top " + numPlayers + " player(s) potentially advancing:```");
                        await players.Take(numPlayers).ForEachAsync(u => builder.AppendLine(u.Name + " : " + u.Total));
                        builder.Append(" ```Other player scores:```");
                        await players.Skip(numPlayers).ForEachAsync(u => builder.AppendLine(u.Name + " : " + u.Total));
                    }
                    else
                    {
                        builder.Append("Scores in order:```");
                        await players.ForEachAsync(u => builder.AppendLine(u.Name + " : " + u.Total));
                    }
                    builder.Append(" ```Scores not recorded:``` ");
                    await playersNotScored.ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.Append("```Use the \n`!advance *numPlayers* *true* \n(Only use true to also delete scores)`\n command to advance the bracket when ready. " +
                        "If a score is incorrect, run the \n`!updateTotal *score* *name*`\n command and try this command again.");
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

        public async Task<string> UpdateTotal(int score, string name)
        {
            string message = "Total";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.FirstOrDefaultAsync(u =>u.Name == name);
                    user.Total = score;
                    db.Users.Update(user);
                    await db.SaveChangesAsync();
                    message = "Updated the score for player " + name + " to " + score + ".";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Advance(int numPlayers, bool resetScores)
        {
            string message = "Advance";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    var totalPlayers = db.Users.Where(u => u.CheckedIn);
                    var playersAdvancing = totalPlayers.OrderByDescending(u => u.Total).Take(numPlayers);
                    var playersOut = totalPlayers.OrderByDescending(u => u.Total).Skip(numPlayers);
                    await playersOut.ForEachAsync(u =>
                    {
                        u.CheckedIn = false;
                        u.SignedUp = false;
                        db.Users.Update(u);
                    });
                    if(resetScores)
                    {
                        await totalPlayers.ForEachAsync(u =>
                        {
                            u.FirstGame = null;
                            u.SecondGame = null;
                            u.ThirdGame = null;
                            u.Total = null;
                            db.Users.Update(u);
                        });
                    }
                    await db.SaveChangesAsync();
                    builder.Append("The bracket is ready to be created again with the following players:```");
                    await playersAdvancing.ForEachAsync(u => builder.AppendLine(u.Name + " : " + u.Total));
                    builder.Append(" ```Please use the `!createBrackets` command when you are ready to continue");
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

        public async Task<string> SendCode(ulong id, string code)
        {
            string message = "Send";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    await guild.DownloadUsersAsync();
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    var users = guild.Users.Where(u => u.Roles.Any(x => x.Id == director.MatchId));
                    var dbusers = db.Users.Where(u => users.Any(x => x.Id == u.Id));
                    builder.Append("Message sent to:```");
                    await dbusers.ForEachAsync(u =>
                    {
                        var dmChannel = guild.GetUser(u.Id).GetOrCreateDMChannelAsync().Result.SendMessageAsync("Message from your director, " + director.DirectorName + ":\n```" + code + "```");
                        builder.AppendLine(u.Name);
                    });
                    builder.Append(" ```");
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

        public async Task<string> Overview(ulong id)
        {
            string message = "Overview";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    await guild.DownloadUsersAsync();
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    var role = guild.Roles.SingleOrDefault(u => u.Id == director.MatchId);
                    var users = guild.Users.Where(u => u.Roles.Any(x => x.Id == director.MatchId));
                    var dbusers = db.Users.Where(u => users.Any(x => x.Id == u.Id));
                    builder.Append("Players in your match (" + role.Name + "):```");
                    await dbusers.ForEachAsync(u =>
                    {
                        var score = u.ThirdGame ?? u.SecondGame ?? u.FirstGame ?? 0;
                        builder.AppendLine(u.Name + ":" + score);
                    });
                    builder.Append(" ```");
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

        public async Task<string> Correct(ulong id, int score, string name)
        {
            string message = "Correct";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    await guild.DownloadUsersAsync();
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    var role = guild.Roles.SingleOrDefault(u => u.Id == director.MatchId);
                    var users = guild.Users.Where(u => u.Roles.Any(x => x.Id == director.MatchId));
                    var dbusers = db.Users.Where(u => users.Any(x => x.Id == u.Id));
                    var user = dbusers.FirstOrDefault(u => EF.Functions.Like(u.DiscordTag.ToLower(), "%" + name.ToLower() + "%") || EF.Functions.Like(u.Name.ToLower(), "%" + name.ToLower() + "%"));
                    if(user != null)
                    {
                        if (user.ThirdGame != null)
                            user.ThirdGame = score;
                        else if (user.SecondGame != null)
                            user.SecondGame = score;
                        else
                            user.FirstGame = score;
                        user.Total = user.FirstGame + user.SecondGame ?? 0 + user.ThirdGame ?? 0;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Updated the score for player " + user.Name + " to " + score + ". Feel free to check `!overview` to make sure everything looks correct.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Score(ulong id, string scores, bool overrided)
        {
            string message = "Score";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    if (director.Submitted && !overrided)
                    {
                        message = "You already submitted results. You can make a correction (if you think there is a mistake) by using the command```!correct *name* *newScore*``` ";
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        var scoreArray = scores.Split(',').Select(int.Parse).ToArray();
                        await guild.DownloadUsersAsync();
                        var role = guild.Roles.SingleOrDefault(u => u.Id == director.MatchId);
                        var users = guild.Users.Where(u => u.Roles.Any(x => x.Id == director.MatchId));
                        var dbusers = db.Users.Where(u => users.Any(x => x.Id == u.Id));
                        if (scoreArray.Length != dbusers.Count())
                            throw new Exception("Data doesn't match amount of records.");
                        builder.Append("Thanks for entering the scores! Below are the scores the way you entered them. " +
                            "Double check to make sure they match the spreadsheet scores. If they don't, use the command```!correct *name* *newScore*```Scores:```");
                        var count = 0;
                        await dbusers.ForEachAsync(u =>
                        {
                            if (u.FirstGame == null)
                            {
                                u.FirstGame = scoreArray[count];

                            }
                            else if (u.SecondGame == null)
                            {
                                u.SecondGame = scoreArray[count];
                            }
                            else
                            {
                                u.ThirdGame = scoreArray[count];
                            }
                            u.Total = u.FirstGame + u.SecondGame ?? 0 + u.ThirdGame ?? 0;
                            var score = u.ThirdGame ?? u.SecondGame ?? u.FirstGame ?? 0;
                            builder.AppendLine(u.Name + ":" + score);
                            db.Users.Update(u);
                            count++;
                        });
                        director.Submitted = true;
                        db.Directors.Update(director);
                        await db.SaveChangesAsync();
                        builder.Append(" ```");
                        message = builder.ToString();
                    }
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
            string message = "Verfiy";
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
                    var emote = new Emoji("✅");
                    signUps.GetReactionUsersAsync(emote, 200).ForEach(x =>
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

        public async Task<string> FindRegion(ulong id)
        {
            string message = "FindRegion";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
                    if (user != null)
                        message = user.Region;
                    else
                        message = "XX";
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

        public async Task<string> AddScrimSignUp(ulong id)
        {
            string message = "ScrimSignUp";
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
                        message = "You are not registered in the database or have not selected a region. Make sure to do the " +
                            "\n\"!join <IN GAME NAME>\"\ncommand in these DMs and select a region in DPL's `#region-selection` channel and try to react again.";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> StartScrim(ulong userId, IUserMessage signUpMessage, ulong roleId)
        {
            string message = "";
            if (guild.GetRole(services.GetService<ConfigHandler>().GetScrimAdminRole()).Members.Any(x => x.Id == userId))//Success finding the admin role for user
            {
                var emote = new Emoji("✅");
                var builder = new StringBuilder();
                builder.Append("```\nUSERS SIGNED UP:");
                var reactions = await signUpMessage.GetReactionUsersAsync(emote,21).FlattenAsync();//Max 20 for 2 sets
                reactions = reactions.TakeLast(reactions.Count() - 1);//Removes the bot
                if(reactions.Count() > 10 && reactions.Count() < 20)//Remove left over entries
                {
                    reactions = reactions.Take(10);
                }
                var counter = 1;
                foreach(var user in reactions)
                {
                    roleQueue.Enqueue((user.Id, roleId, true));
                    builder.Append(counter + " - " + user.Username);
                    counter++;
                }
                builder.Append("```");
                message = builder.ToString();
                var emote2 = new Emoji("❌");
                await signUpMessage.RemoveAllReactionsAsync();
                await signUpMessage.AddReactionAsync(emote2);
            }
            return message;
        }
    }
}
