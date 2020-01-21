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

        [Command("dm", RunMode = RunMode.Async)]
        [Summary("Create another DM channel")]
        [RequireContext(ContextType.Guild)]
        public async Task Dm()
        {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("Hello! Welcome to The Darwin Pro League. This is a hub for many Darwin Tournaments to come! " +
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
                    .AddField("Rules and Info", "All rules and info is located in the <#486912876162842645> channel.")
                    .AddField("Tournament Date", tournamentDateMonth + " " + tournamentDateDay + " at " + tournamentDateTime + " EST")
                    .AddField("Click the ✅ to sign up.", "Remember to select your region.")
                    .AddField("If not already registered", "In a dm with the bot do !join <your name> like !join lilscarerow");
            var signupEmbed = signupBuilder.Build();

            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text: Context.Guild.EveryoneRole.Mention, embed: signupEmbed);
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            config.SaveSignUpMessage(message);
        }

        [Command("checkInMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for checkins")]
        [RequireContext(ContextType.Guild)]
        public async Task CheckInMessage(string tournamentDateTime, [Remainder] string text)
        {

            var checkinBuilder = new EmbedBuilder()
                .WithDescription(text)
                .WithColor(new Color(0xFFB200))
                .WithAuthor(author =>
                {
                    author
                        .WithName("Check-Ins are now OPEN!")
                        .WithIconUrl("https://i.imgur.com/YYatELp.png");
                })
                .AddField("Rules and Info", "All rules and info is located in the <#486912876162842645> channel.")
                .AddField("Tournament Time", tournamentDateTime + " EST")
                .AddField("Click the ✅ to check in.", "⠀").Build();
            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text: Context.Guild.EveryoneRole.Mention, embed: checkinBuilder);
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
        public async Task RegionMessage()
        {
            var regionBuilder = new EmbedBuilder()
                .WithTitle("Region Assignment")
                .WithDescription("Click the rection corresponding to your region.\nThis will allow us to know what region you're\nfrom for tournaments " +
                "(Note: Not all tournaments\nwill use regions for creating brackets.)\n\n🇺🇸 East\n <:cali:663097025033666560> West\n🇪🇺 EU\n🇧🇷 SA\n🇦🇺 OCE" +
                "\n🇸🇬 SP\n\nIf the bot tells you that you aren't registered, you\n must do the !join name command in the dms with\n the bot first. If the" +
                " bot doesn't message you at all, \ndm lilscarecrow directly.")
                .WithThumbnailUrl("https://img.icons8.com/cotton/2x/globe.png")
                .WithColor(new Color(0x202225)).Build();
            var message = await Context.Guild.GetTextChannel(config.GetRegionChannel()).SendMessageAsync(embed:regionBuilder);
            var emoteEast = new Emoji("🇺🇸");
            var emoteEU = new Emoji("🇪🇺");
            var emoteSA = new Emoji("🇧🇷");
            var emoteSP = new Emoji("🇸🇬");
            var emoteAU = new Emoji("🇦🇺");
            var emoteWest = Emote.Parse("<:cali:663097025033666560>");
            await message.AddReactionAsync(emoteEast);
            await message.AddReactionAsync(emoteWest);
            await message.AddReactionAsync(emoteEU);
            await message.AddReactionAsync(emoteSA);
            await message.AddReactionAsync(emoteAU);
            await message.AddReactionAsync(emoteSP);
            config.SaveRegionMessage(message);
        }

        [Command("refreshRegions", RunMode = RunMode.Async)]
        [Summary("Refresh all region reactions")]
        [RequireContext(ContextType.Guild)]
        public async Task RefreshRegions()
        {
            var regionMessage = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).GetMessageAsync(config.GetRegionMessage()) as RestUserMessage;
            if (regionMessage == null)
            {
                await Context.Channel.SendMessageAsync("Cannot find region message!");
                return;
            }
            var message = await program.RefreshRegionSelection(regionMessage);
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
        [RequireContext(ContextType.Guild)]
        public async Task Verify()
        {
            var message = await program.Verify();
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("rebuild", RunMode = RunMode.Async)]
        [Summary("Rebuild members who aren't registered")]
        [RequireContext(ContextType.Guild)]
        public async Task Rebuild()
        {
            var message = await program.Rebuild();
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setEastRole", RunMode = RunMode.Async)]
        [Summary("Set East scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetEastRole(ulong id)
        {
            config.SetEastScrimRole(id);
            var message = "Id set for East scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setWestRole", RunMode = RunMode.Async)]
        [Summary("Set West scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetWestRole(ulong id)
        {
            config.SetWestScrimRole(id);
            var message = "Id set for West scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setEURole", RunMode = RunMode.Async)]
        [Summary("Set EU scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetEURole(ulong id)
        {
            config.SetEUScrimRole(id);
            var message = "Id set for EU scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setSARole", RunMode = RunMode.Async)]
        [Summary("Set SA scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetSARole(ulong id)
        {
            config.SetSAScrimRole(id);
            var message = "Id set for SA scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setSPRole", RunMode = RunMode.Async)]
        [Summary("Set SP scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetSPRole(ulong id)
        {
            config.SetSPScrimRole(id);
            var message = "Id set for SP scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setAURole", RunMode = RunMode.Async)]
        [Summary("Set AU scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetAURole(ulong id)
        {
            config.SetAUScrimRole(id);
            var message = "Id set for AU scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setEastActiveRole", RunMode = RunMode.Async)]
        [Summary("Set East Active scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetEastActiveRole(ulong id)
        {
            config.SetEastScrimActiveRole(id);
            var message = "Id set for East Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setWestActiveRole", RunMode = RunMode.Async)]
        [Summary("Set West Active scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetWestActiveRole(ulong id)
        {
            config.SetWestScrimActiveRole(id);
            var message = "Id set for West Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setEUActiveRole", RunMode = RunMode.Async)]
        [Summary("Set EU Active scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetEUActiveRole(ulong id)
        {
            config.SetEUScrimActiveRole(id);
            var message = "Id set for EU Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setSAActiveRole", RunMode = RunMode.Async)]
        [Summary("Set SA scrim Active role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetSAActiveRole(ulong id)
        {
            config.SetSAScrimActiveRole(id);
            var message = "Id set for SA Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setSPActiveRole", RunMode = RunMode.Async)]
        [Summary("Set SP Active scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetSPActiveRole(ulong id)
        {
            config.SetSPScrimActiveRole(id);
            var message = "Id set for SP Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("setAUActiveRole", RunMode = RunMode.Async)]
        [Summary("Set AU Active scrim role")]
        [RequireContext(ContextType.Guild)]
        public async Task SetAUActiveRole(ulong id)
        {
            config.SetAUScrimActiveRole(id);
            var message = "Id set for AU Active scrim role";
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("getGuildInfo", RunMode = RunMode.Async)]
        [Summary("Gets info about roles")]
        [RequireContext(ContextType.Guild)]
        public async Task GetGuildInfo()
        {
            var message = await program.GuildInfo();
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("scrimDashboard", RunMode = RunMode.Async)]
        [Summary("Set the message for the dashboard")]
        [RequireContext(ContextType.Guild)]
        public async Task ScrimDashboard()
        {
            var builder = new EmbedBuilder()
                .WithTitle("Scrim Dashboard")
                .WithDescription("Click the region you would like to start a scrim for.\n :flag_us: EAST: \n <:cali:663097025033666560> WEST: " +
                "\n :flag_eu: EU: \n :flag_br: SA: \n :flag_au: OCE: \n :flag_sg: SP: ")
                .WithColor(new Color(0xF5FF))
                .WithThumbnailUrl("http://cdn.onlinewebfonts.com/svg/img_205575.png").Build();
            var message = await Context.Guild.GetTextChannel(config.GetScrimAdminChannel()).SendMessageAsync(embed: builder);
            var emoteEast = new Emoji("🇺🇸");
            var emoteEU = new Emoji("🇪🇺");
            var emoteSA = new Emoji("🇧🇷");
            var emoteSP = new Emoji("🇸🇬");
            var emoteAU = new Emoji("🇦🇺");
            var emoteWest = Emote.Parse("<:cali:663097025033666560>");
            await message.AddReactionAsync(emoteEast);
            await message.AddReactionAsync(emoteWest);
            await message.AddReactionAsync(emoteEU);
            await message.AddReactionAsync(emoteSA);
            await message.AddReactionAsync(emoteAU);
            await message.AddReactionAsync(emoteSP);
            config.SaveDashboardMessage(message);
        }

        [Command("scrimSignUpMessage", RunMode = RunMode.Async)]
        [Summary("Set the message for the scrim sign ups")]
        [RequireContext(ContextType.Guild)]
        public async Task ScrimSignUpMessage()
        {
            var builder = new EmbedBuilder()
                .WithTitle("Scrim Role Assignment")
                .WithDescription("Click the :white_check_mark: to get access to scrims. Remove reaction to remove access.")
                .WithColor(new Color(0xB88E00))
                .WithThumbnailUrl("https://i.imgur.com/hJU393s.png").Build();
            var message = await Context.Guild.GetTextChannel(config.GetScrimChannel()).SendMessageAsync(embed: builder);
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            config.SaveScrimMessage(message);
        }

        [Command("removePlayer", RunMode = RunMode.Async)]
        [Summary("Remove player from the DB")]
        [RequireContext(ContextType.Guild)]
        public async Task RemovePlayer(ulong id)
        {
            var message = await program.RemovePlayer(id);
            await Context.Channel.SendMessageAsync(message);
        }
    }

    [ScrimAdmin]
    public class ScrimAdminModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ConfigHandler config { get; set; }

        [Command("scrimSize", RunMode = RunMode.Async)]
        [Summary("Change max scrim size")]
        [RequireContext(ContextType.Guild)]
        public async Task ScrimSize(int size)
        {
            var message = await program.ScrimSize(size);
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
