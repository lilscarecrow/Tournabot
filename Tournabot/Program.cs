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
            client.UserLeft += HandleLeaveGuild;
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
                if (reaction.Emote.Name == "🇺🇸")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var sqlmessage = await AddMemberRegion(user.Id, "NA");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as NA..." + sqlmessage;
                    await dmChannel.SendMessageAsync(dmMessage);
                }
                else if (reaction.Emote.Name == "🇪🇺")
                {
                    var user = await channel.GetUserAsync(reaction.UserId);
                    var sqlmessage = await AddMemberRegion(user.Id, "EU");
                    var dmChannel = await user.GetOrCreateDMChannelAsync();
                    var dmMessage = "Adding Region as EU..." + sqlmessage;
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
                    var total = db.Users.Where(u => u.CheckedIn).Count();
                    if (user != null && total < 100 && !user.IsDirector)
                    {
                        user.SignedUp = true;
                        db.Users.Update(user);
                        await db.SaveChangesAsync();
                        message = "Successfully signed up for the upcoming tournament!";
                    }
                    else if (user.IsDirector)
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
                    if (user != null && user.WaitList)
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
                        message = user.Name + " is not registered in the database.";
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
                        builder.Append("Successfully found member(s):```");
                        await users.ForEachAsync(u => builder.AppendLine("Id = " + u.Id + "\nDiscordTag = " + u.DiscordTag + "\nName = " + u.Name));
                        builder.Append(" ```");
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
                    builder.Append("Directors: ``` ");
                    await directors.ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.Append("```Signed Up: ``` ");
                    await signedUp.ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.Append("```Checked In: ``` ");
                    await checkedIn.ForEachAsync(u => builder.AppendLine(u.Name));
                    builder.Append("```");
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

        public async Task<string> CreateBrackets(List<IRole> roles)
        {
            string message = "Brackets";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
                    await db.Users.ForEachAsync(u => guild.GetUser(u.Id).RemoveRolesAsync(roles));//Remove all roles
                    var players = db.Users.Where(u => u.CheckedIn && !u.IsDirector).OrderBy(u => Guid.NewGuid());//Randomize players
                    var buckets = (int) Math.Ceiling(players.Count() / 10.0);
                    var directors = db.Users.Where(u => u.IsDirector).OrderBy(u => Guid.NewGuid()).Take(buckets);//Take number of directors needed randomly
                    await db.Database.ExecuteSqlCommandAsync("TRUNCATE TABLE public.\"Directors\"");
                    if (buckets == 1)//FINALS
                    {
                        var finalsDirector = guild.Users.FirstOrDefault(u => u.Roles.Any(x => x.Id == roles[10].Id));
                        if (finalsDirector == null)
                            throw new Exception("Can't find Finals Director.");
                        var finalsDirectorDb = await db.Users.SingleOrDefaultAsync(u => u.Id == finalsDirector.Id);
                        if (finalsDirectorDb == null)
                            throw new Exception("Can't find Finals Director in DB.");
                        var dir = new Directors
                        {
                            Id = finalsDirectorDb.Id,
                            DirectorName = finalsDirectorDb.Name,
                            MatchId = roles[10].Id,
                            Submitted = false
                        };
                        builder.Append("Finals Director:```" + dir.DirectorName + "```Finalists:```");
                        db.Directors.Add(dir);
                        await players.ForEachAsync(u => 
                        {
                            guild.GetUser(u.Id).AddRoleAsync(roles[10]);
                            builder.AppendLine(u.Name);
                        });
                        builder.Append(" ```");
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
                                user.AddRoleAsync(roles[count]);
                                var dir = new Directors
                                {
                                    Id = u.Id,
                                    DirectorName = u.Name,
                                    MatchId = roles[count].Id,
                                    Submitted = false
                                };
                                db.Directors.Add(dir);
                                directorList.Add(dir);
                                count++;
                            }
                        });
                        count = 0;
                        await players.ForEachAsync(u =>
                        {
                            guild.GetUser(u.Id).AddRoleAsync(roles[count]);
                            count++;
                            count %= buckets;
                        });
                        for(count = 0; count < buckets; count++)
                        {
                            builder.Append(roles[count].Name + " Director:```" + directorList[count] + "```Players:```");
                            var roledPlayers = guild.Users.Where(u => u.Roles.Any(x => x.Id == roles[count].Id));
                            var dbusers = db.Users.Where(u => roledPlayers.Any(x => x.Id == u.Id));
                            await dbusers.ForEachAsync(u => builder.AppendLine(u.Name));
                            builder.Append(" ```");
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
                    var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
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
                    var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    var users = guild.Users.Where(u => u.Roles.Any(x => x.Id == director.MatchId));
                    var dbusers = db.Users.Where(u => users.Any(x => x.Id == u.Id));
                    builder.Append("Message sent to:```");
                    await dbusers.ForEachAsync(async u =>
                    {
                        var dmChannel = await guild.GetUser(u.Id).GetOrCreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("Message from your director, " + director.DirectorName + ":\n```" + code + "```");
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
                    var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
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
                    var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
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

        public async Task<string> Score(ulong id, string scores)
        {
            string message = "Score";
            using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
            {
                try
                {
                    var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == id);
                    if (director.Submitted)
                    {
                        message = "You already submitted results. You can make a correction (if you think there is a mistake) by using the command```!correct *name* *newScore*``` ";
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder();
                        var scoreArray = scores.Split(',').Select(int.Parse).ToArray();
                        var guild = client.GetGuild(services.GetService<ConfigHandler>().GetGuild());
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
    }
}
