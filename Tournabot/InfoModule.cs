using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//TEST
using Tesseract;
using System.IO;
using System.Reflection;
using System.Net;

namespace Tournabot
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ConfigHandler config { get; set; }

        [Command("scan", RunMode = RunMode.Async)]
        [Summary("Scan a result page")]
        [RequireContext(ContextType.DM)]
        public async Task Scan()
        {
            var message = "No file attached!";
            if (!Context.Message.Attachments.Any())
            {
                await Context.Channel.SendMessageAsync(message);
                return;
            }
            IAttachment att = Context.Message.Attachments.First();
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), att.Filename).Replace(@"\", @"\\");
            WebClient webClient = new WebClient();
            Uri uri = new Uri(att.Url);
            await webClient.DownloadFileTaskAsync(uri, filePath);
            using (var engine = new TesseractEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(filePath))
                {
                    var page = engine.Process(img);
                    message = page.GetText();
                }
            }
            File.Delete(filePath);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Join the database fun")]
        [RequireContext(ContextType.DM)]
        public async Task Join([Remainder] string name)
        {
            var discordTag = Context.User.Username + "#" + Context.User.DiscriminatorValue;
            var message = await program.AddMember(Context.User.Id, discordTag, name);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Check status of member")]
        [RequireContext(ContextType.DM)]
        public async Task Status()
        {
            var message = await program.GetMember(Context.User.Id);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("unregister", RunMode = RunMode.Async)]
        [Summary("Unregister for the tourney")]
        [RequireContext(ContextType.DM)]
        public async Task Unregister()
        {
            var message = await program.Unregister(Context.User.Id);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("dm", RunMode = RunMode.Async)]
        [Summary("Create another DM channel")]
        [RequireContext(ContextType.Guild)]
        public async Task Dm()
        {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("Hello! Welcome to The Darwin Elite. This is a hub for many Darwin Tournaments to come! " +
                "In order to keep members organized, please reply with the following information (with the `!join` command): \n" +
                "```In-game Name```\n" +
                "Example:\n" +
                "```!join lilscarerow```\n" +
                "Other commands:\n" +
                "```!status\n" +
                "!unregister```\n" +
                "If you have any questions or encounter any problems, please DM lilscarecrow#5308 on Discord.");
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("All the info")]
        [RequireContext(ContextType.DM)]
        public async Task Help()
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                List<ModuleInfo> mods = program.GetCommands().Modules.ToList<ModuleInfo>();
                foreach (ModuleInfo mod in mods)
                {
                    if(mod.Name == "InfoModule")
                        builder.Append("```User Commands:``````");
                    else if (mod.Name == "AdminModule")
                        builder.Append("```Admin Commands:``````");
                    else if (mod.Name == "DirectorModule")
                        builder.Append("```Director Commands:``````");
                    foreach (CommandInfo command in mod.Commands)
                    {
                        builder.Append("!");
                        if (mod.Group == "admin")
                            builder.Append("admin ");
                        builder.Append(command.Name);
                        foreach (Discord.Commands.ParameterInfo param in command.Parameters)
                        {
                            builder.Append(" *" + param.Name + "*");
                        }
                        builder.Append("\n\tSummary:\n\t\t" + command.Summary + "\n");
                        //builder.Append("```\n");
                        if (builder.Length >= 1800)
                        {
                            builder.Append("```\n");
                            await Context.Channel.SendMessageAsync(builder.Length == 0 ? "N/A" : builder.ToString());
                            builder.Clear();
                        }
                    }
                    builder.Append("```");
                }
                await Context.Channel.SendMessageAsync(builder.Length == 0 ? "N/A" : builder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("scrim", RunMode = RunMode.Async)]
        [Summary("Start A Scrim")]
        [RequireContext(ContextType.Guild)]
        public async Task Scrim(string region = "0", string tolerance = "7")
        {
            string message;
            int realTolerance;
            IUserMessage announcement;
            IDMChannel dmChannel;
            try
            {
                if (region == "0")
                {
                    region = await program.FindRegion(Context.User.Id);
                }
                if(region != "NA" && region != "WE" && region != "EU")
                {
                    dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync("The region selected doesn't make sense. You chose (or defaulted to): " + region + " when \"NA\" \"WE\" or \"EU\" were expected.");
                    return;
                }

                if(!Int32.TryParse(tolerance, out realTolerance) || realTolerance < 2 || realTolerance > 10)
                {
                    dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                    await dmChannel.SendMessageAsync("The tolerance of " + realTolerance + " players doesn\'t work. Please set a tolerance between 2 and 10 or don't set one yourself. (The default will be a 7 player tolerance)");
                    return;
                }
                announcement = await Context.Guild.GetTextChannel(config.GetScrimChannel()).SendMessageAsync(Context.Guild.GetRole(529397215203164192).Mention + " A scrim is starting soon for " 
                    + region + "! React to the ✅ below to join as a player, or react to the 🤖 to be the director!");
                var emote1 = new Emoji("✅");
                var emote2 = new Emoji("🤖");
                message = await program.CreateScrim(Context.User.Id, region, realTolerance, announcement.Id);
                dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(message);
                await Context.Message.DeleteAsync();
                await announcement.AddReactionAsync(emote1);
                await announcement.AddReactionAsync(emote2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("start", RunMode = RunMode.Async)]
        [Summary("Begin A Scrim")]
        [RequireContext(ContextType.DM)]
        public async Task Start()
        {
            try
            {
                await program.StartScrim(Context.User.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("code", RunMode = RunMode.Async)]
        [Summary("Enter Scrim Code")]
        [RequireContext(ContextType.DM)]
        public async Task Code(string code)
        {
            try
            {
                var message = await program.ScrimCode(Context.User.Id, code);
                var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(message);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("end", RunMode = RunMode.Async)]
        [Summary("End A Scrim")]
        [RequireContext(ContextType.DM)]
        public async Task End()
        {
            try
            {
                await program.RemoveScrimInstance(Context.User.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    [Admin]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ConfigHandler config { get; set; }
        [Command("signUpMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for signups")]
        [RequireContext(ContextType.Guild)]
        public async Task SignUpMessage(string signupImage, string tournamentDateMonth, string tournamentDateDay, string tournamentDateTime, [Remainder] string text)
        {
            var signupBuilder = new EmbedBuilder()
                    .WithTitle("We are happy to annouce another tournament!")
                    .WithDescription(text)
                    .WithColor(new Color(0x97400))
                    .WithImageUrl(signupImage)
                    .WithAuthor(author => {
                        author
                            .WithName("Sign-ups are now OPEN!")
                            .WithIconUrl("https://i.imgur.com/YYatELp.png");
                    })
                    .AddField("Rules and Info", "All rules and info is located in the tournament-rules channel.")
                    .AddField("Tournament Date", tournamentDateMonth + " " + tournamentDateDay + " at " + tournamentDateTime + " EST")
                    .AddField("Click the ✅ to sign up.", "Remember to select your region.")
                    .AddField("If not already registered", "In a dm with the bot do !join <your name> like !join lilscarerow");
            var signupEmbed = signupBuilder.Build();

            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(embed: signupEmbed);
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            config.SaveSignUpMessage(message);
        }

        [Command("checkInMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for checkins")]
        [RequireContext(ContextType.Guild)]
        public async Task CheckInMessage([Remainder] string text)
        {
            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text);
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            config.SaveCheckInMessage(message);
        }

        [Command("waitListMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for the wait list")]
        [RequireContext(ContextType.Guild)]
        public async Task WaitListMessage([Remainder] string text)
        {
            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text);
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            config.SaveWaitListMessage(message);
        }

        [Command("regionMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for regions")]
        [RequireContext(ContextType.Guild)]
        public async Task RegionMessage([Remainder] string text)
        {
            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text);
            var emoteEast = new Emoji("🇺🇸");
            var emoteEU = new Emoji("🇪🇺");
            var emoteWest = new Emoji("🇼");
            await message.AddReactionAsync(emoteEast);
            await message.AddReactionAsync(emoteWest);
            await message.AddReactionAsync(emoteEU);
            config.SaveRegionMessage(message);
        }

        [Command("refreshRegions", RunMode = RunMode.Async)]
        [Summary("Refresh all region reactions")]
        [RequireContext(ContextType.Guild)]
        public async Task RefreshRegions()
        {
            var emoteEast = new Emoji("🇺🇸");
            var emoteEU = new Emoji("🇪🇺");
            var emoteWest = new Emoji("🇼");
            var regionMessage = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).GetMessageAsync(config.GetRegionMessage()) as RestUserMessage;
            if (regionMessage == null)
            {
                await Context.Channel.SendMessageAsync("Cannot find region message!");
                return;
            }
            var reactionsEast = await regionMessage.GetReactionUsersAsync(emoteEast, 1000).FlattenAsync();
            var reactionsEU = await regionMessage.GetReactionUsersAsync(emoteEU, 1000).FlattenAsync();
            var reactionsWest = await regionMessage.GetReactionUsersAsync(emoteWest, 1000).FlattenAsync();
            var message = await program.RefreshRegionSelection(reactionsEast, reactionsEU, reactionsWest);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("refreshSignUps", RunMode = RunMode.Async)]
        [Summary("Refresh all sign up reactions")]
        [RequireContext(ContextType.Guild)]
        public async Task RefreshSignUps()
        {
            var emote = new Emoji("✅");
            var signUpMessage = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).GetMessageAsync(config.GetSignUpMessage()) as RestUserMessage;
            if (signUpMessage == null)
            {
                await Context.Channel.SendMessageAsync("Cannot find sign up message!");
                return;
            }
            var reactions = await signUpMessage.GetReactionUsersAsync(emote, 1000).FlattenAsync();
            var message = await program.RefreshSignUpSelection(reactions);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("refreshCheckIns", RunMode = RunMode.Async)]
        [Summary("Refresh all check in reactions")]
        [RequireContext(ContextType.Guild)]
        public async Task RefreshCheckIns()
        {
            var emote = new Emoji("✅");
            var checkInMessage = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).GetMessageAsync(config.GetCheckInMessage()) as RestUserMessage;
            if (checkInMessage == null)
            {
                await Context.Channel.SendMessageAsync("Cannot find check in message!");
                return;
            }
            var reactions = await checkInMessage.GetReactionUsersAsync(emote, 1000).FlattenAsync();
            var message = await program.RefreshCheckInSelection(reactions);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("addSignUp", RunMode = RunMode.Async)]
        [Summary("Add Sign Up for a user")]
        [RequireContext(ContextType.Guild)]
        public async Task AddSignUp(ulong id)
        {
            var message = await program.AddMemberSignUp(id);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("addCheckIn", RunMode = RunMode.Async)]
        [Summary("Add Sign Up for a user")]
        [RequireContext(ContextType.Guild)]
        public async Task AddCheckIn(ulong id)
        {
            var message = await program.AddMemberCheckIn(id);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("checkStatus", RunMode = RunMode.Async)]
        [Summary("Check status of member")]
        [RequireContext(ContextType.Guild)]
        public async Task Status(ulong id)
        {
            var message = await program.GetMember(id);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("clearSignUps", RunMode = RunMode.Async)]
        [Summary("Clear all entries in DB")]
        [RequireContext(ContextType.Guild)]
        public async Task ClearSignUps()
        {
            var message = await program.ClearSignUps();
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("remove", RunMode = RunMode.Async)]
        [Summary("Remove Sign Up of a user")]
        [RequireContext(ContextType.Guild)]
        public async Task Remove(ulong id)
        {
            var message = await program.RemovePlayer(id);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("find", RunMode = RunMode.Async)]
        [Summary("Find a member in the database")]
        [RequireContext(ContextType.Guild)]
        public async Task Find(string name)
        {
            var message = await program.FindMember(name);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("addDirector", RunMode = RunMode.Async)]
        [Summary("Add a director")]
        [RequireContext(ContextType.Guild)]
        public async Task AddDirector(ulong id)
        {
            var message = await program.AddDirector(id);
            var user = Context.Guild.Users.SingleOrDefault(u => u.Id == id);
            if(user != null)
            {
                await user.AddRoleAsync(user.Guild.GetRole(config.GetDirectorRole()));
                message += " The member was also given the Director role.";
            }
            else
                message += " The member was NOT given the Director role. Something went wrong.";
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("addFinalsDirector", RunMode = RunMode.Async)]
        [Summary("Add a director")]
        [RequireContext(ContextType.Guild)]
        public async Task AddFinalsDirector(ulong id)
        {
            var message = await program.AddDirector(id);
            var user = Context.Guild.Users.SingleOrDefault(u => u.Id == id);
            if (user != null)
            {
                await user.AddRoleAsync(user.Guild.GetRole(config.GetFinalsDirectorRole()));
                message += " The member was also given the Finals Director role.";
            }
            else
                message += " The member was NOT given the Finals Director role. Something went wrong.";
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("removeDirector", RunMode = RunMode.Async)]
        [Summary("Remove a director")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveDirector(ulong id)
        {
            var message = await program.RemoveDirector(id);
            var user = Context.Guild.Users.SingleOrDefault(u => u.Id == id);
            if (user != null)
            {
                await user.RemoveRoleAsync(user.Guild.GetRole(config.GetDirectorRole()));
                message += " The member was also removed from the Director role.";
            }
            else
                message += " The member was NOT revoked of the Director role. Something went wrong.";
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("removeFinalsDirector", RunMode = RunMode.Async)]
        [Summary("Remove a director")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveFinalsDirector(ulong id)
        {
            var message = await program.RemoveDirector(id);
            var user = Context.Guild.Users.SingleOrDefault(u => u.Id == id);
            if (user != null)
            {
                await user.RemoveRoleAsync(user.Guild.GetRole(config.GetFinalsDirectorRole()));
                message += " The member was also removed from the Finals Director role.";
            }
            else
                message += " The member was NOT revoked of the Finals Director role. Something went wrong.";
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("roster", RunMode = RunMode.Async)]
        [Summary("Show the current roster")]
        [RequireContext(ContextType.Guild)]
        public async Task Roster()
        {
            var message = await program.GetRoster();
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("createBrackets", RunMode = RunMode.Async)]
        [Summary("Create the tournament brackets")]
        [RequireContext(ContextType.Guild)]
        public async Task CreateBrackets()
        {
            var message = await program.CreateBrackets();

            while (message.Length >= 1800)
            {
                var miniMessage = message.Substring(0, 1800);
                await Context.Channel.SendMessageAsync(miniMessage);//await Context.Guild.GetTextChannel(config.GetBracketInfoChannel()).SendMessageAsync(miniMessage);//UNCOMMENT
                message = message.Substring(1800);
            }

            await Context.Channel.SendMessageAsync(message);//await Context.Guild.GetTextChannel(config.GetBracketInfoChannel()).SendMessageAsync(message);//UNCOMMENT
        }

        [Command("calculateScores", RunMode = RunMode.Async)]
        [Summary("Top scores for the brackets")]
        [RequireContext(ContextType.Guild)]
        public async Task CalculateScores(int numPlayers = 0)
        {
            var message = await program.CalculateScores(numPlayers);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("updateTotal", RunMode = RunMode.Async)]
        [Summary("Update a total score")]
        [RequireContext(ContextType.Guild)]
        public async Task UpdateTotal(int score, [Remainder] string name)
        {
            var message = await program.UpdateTotal(score, name);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("advance", RunMode = RunMode.Async)]
        [Summary("Advance top players")]
        [RequireContext(ContextType.Guild)]
        public async Task Advance(int numPlayers, bool resetScores = false)
        {
            var message = await program.Advance(numPlayers, resetScores);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("crown", RunMode = RunMode.Async)]
        [Summary("Crown a winner")]
        [RequireContext(ContextType.Guild)]
        public async Task Crown(ulong id)
        {
            var message = await program.Crown(id);
            await Context.Guild.GetUser(id).AddRoleAsync(Context.Guild.GetRole(config.GetChampionRole()));
            await Context.Guild.GetTextChannel(config.GetBracketInfoChannel()).SendMessageAsync(message);
        }

        [Command("score", RunMode = RunMode.Async)]
        [Summary("Set the scores for a match")]
        [RequireContext(ContextType.Guild)]
        public async Task Score(ulong id, string scores)
        {
            var message = await program.Score(id, scores, true);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("nuke", RunMode = RunMode.Async)]
        [Summary("Delete channel's messages")]
        [RequireContext(ContextType.Guild)]
        public async Task Nuke(int num)
        {
            if (num < 0)
                num = 1;
            else if (num > 100)
                num = 100;
            var messages = Context.Channel.GetMessagesAsync(num);
            await messages.ForEachAsync(async x =>
             {
                 foreach (var y in x)
                 {
                     await Context.Channel.DeleteMessageAsync(y.Id);
                 }
             });
        }

        [Command("verify", RunMode = RunMode.Async)]
        [Summary("Find members who aren't registered")]
        [RequireContext(ContextType.DM)]
        public async Task Verify()
        {
            var message = await program.Verify();
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("rebuild", RunMode = RunMode.Async)]
        [Summary("Rebuild members who aren't registered")]
        [RequireContext(ContextType.DM)]
        public async Task Rebuild()
        {
            var message = await program.Rebuild();
            await Context.Channel.SendMessageAsync(message);
        }
    }

    [Director]
    public class DirectorModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ConfigHandler config { get; set; }

        [Command("sendCode", RunMode = RunMode.Async)]
        [Summary("Send the game code to your match")]
        [RequireContext(ContextType.DM)]
        public async Task SendCode([Remainder] string code)
        {
            var message = await program.SendCode(Context.User.Id, code);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("overview", RunMode = RunMode.Async)]
        [Summary("See the stats of your match")]
        [RequireContext(ContextType.DM)]
        public async Task Overview()
        {
            var message = await program.Overview(Context.User.Id);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("correct", RunMode = RunMode.Async)]
        [Summary("Change a score")]
        [RequireContext(ContextType.DM)]
        public async Task Correct(int score, [Remainder] string name)
        {
            var message = await program.Correct(Context.User.Id, score, name);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("score", RunMode = RunMode.Async)]
        [Summary("Set the scores for a match")]
        [RequireContext(ContextType.DM)]
        public async Task Score(string scores)
        {
            var message = await program.Score(Context.User.Id, scores, false);
            await Context.Channel.SendMessageAsync(message);
        }
    }
}
