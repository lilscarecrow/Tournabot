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

namespace Tournabot
{
    public class Program
    {
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            client.MessageReceived += HandleCommand;
            client.UserJoined += HandleJoinedGuild;
            client.ReactionAdded += HandleReaction;
            client.Log += Log;
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

            await Task.Delay(-1);
        }

        public CommandService GetCommands()
        {
            return commands;
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
                if (reaction.Emote.Name == "🇺🇸")
                {
                    var userTag = reaction.User.Value.Username + "#" + reaction.User.Value.DiscriminatorValue;
                    var sqlmessage = await AddMemberRegion(userTag, "NA");
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as NA..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇪🇺")
                {
                    var userTag = reaction.User.Value.Username + "#" + reaction.User.Value.DiscriminatorValue;
                    var sqlmessage = await AddMemberRegion(userTag, "EU");
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as EU..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetSignUpMessage())//SIGN UP
            {
                if (reaction.Emote.Name == "✅")
                {
                    var userTag = reaction.User.Value.Username + "#" + reaction.User.Value.DiscriminatorValue;
                    var dmMessage = await AddMemberSignUp(userTag);
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
            else if (message.Id == services.GetService<ConfigHandler>().GetCheckInMessage())//CHECK IN
            {
                if (reaction.Emote.Name == "✅")
                {
                    var userTag = reaction.User.Value.Username + "#" + reaction.User.Value.DiscriminatorValue;
                    var dmMessage = await AddMemberCheckIn(userTag);
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(dmMessage);
                }
            }
        }

        public async Task HandleJoinedGuild(SocketGuildUser user)
        {
            //var OwnerChannel = await user.Guild.Owner.GetOrCreateDMChannelAsync();
            var channel = await user.GetOrCreateDMChannelAsync();
            var message = "Hello! Welcome to The Darwin Elite. This is a hub for many Darwin Tournaments to come! " +
                "In order to keep members organized, please reply with the following information (with the `!join` command): \n" +
                "```In-game Name```\n" +
                "Example:\n" +
                "```!join lilscarerow```\n" +
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

        public async Task<string> AddMember(string discordTag, string name)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
                if (user != null)
                {
                    message = "You are already in the database with the following information:" +
                        "```Name: " + user.Name + "\nRegion: " + user.Region + "\nSigned Up: " + user.SignedUp + "\nChecked In: " + user.CheckedIn + "```";
                    return message;
                }
                user = new Users
                {
                    DiscordTag = discordTag,
                    Name = name,
                    Region = "XX",
                    SignedUp = false,
                    CheckedIn = false
                };
                try
                {
                    db.Users.Add(user);
                    var count = await db.SaveChangesAsync();
                    message = "Successfully entered into the database!";
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> GetMember(string discordTag)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
                    if(user != null)
                        message = "```Name: " + user.Name + "\nRegion: " + user.Region + "\nSigned Up: " + user.SignedUp + "\nChecked In: " + user.CheckedIn + "```";
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

        public async Task<string> AddMemberRegion(string discordTag, string region)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
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

        public async Task<string> AddMemberSignUp(string discordTag)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
                    if (user != null)
                    {
                        user.SignedUp = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Successfully signed up for the upcoming tournament!";
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

        public async Task<string> AddMemberCheckIn(string discordTag)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
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

        public async Task<string> ClearSignUp()
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var users = db.Users.Where(u => u.SignedUp);
                    await users.ForEachAsync(u => { u.SignedUp = false; u.CheckedIn = false; });
                    await db.SaveChangesAsync();
                    message = "Successfully reset " + users.Count() + " records!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }

        public async Task<string> Unregister(string discordTag)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
                    if (user != null && user.SignedUp)
                    {
                        user.SignedUp = false;
                        user.CheckedIn = false;
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

        public async Task<string> RemoveSignUp(string discordTag)
        {
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordTag == discordTag);
                    if (user != null)
                    {
                        db.Users.Remove(user);
                        await db.SaveChangesAsync();
                        message = "Successfully removed " + discordTag;
                    }
                    else
                        message = discordTag + " is not registered in the database.";
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
            string message = "";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var users = db.Users.Where(u => EF.Functions.Like(u.DiscordTag, "%" + name + "%") || EF.Functions.Like(u.Name, "%" + name + "%"));
                    if (users != null)
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append("Successfully found member(s): \n");
                        await users.ForEachAsync(u => builder.Append("```DiscordTag = " + u.DiscordTag + "\nName = " + u.Name + "```"));
                        message = builder.ToString();
                    }
                    else
                        message = "Could not find member " + name;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    message = "Error occurred while updating. Please DM lilscarecrow#5308 on Discord.";
                }
            }
            return message;
        }
    }
}
