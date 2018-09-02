using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tournabot
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ConfigHandler config { get; set; }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Join the database fun")]
        [RequireContext(ContextType.DM)]
        public async Task Join(string name)
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
            await dmChannel.SendMessageAsync("Hi, here is our DM channel! Feel free to use the following commands:\n" +
                "```!join *in-game name*\n" +
                "!status\n" +
                "!unregister```");
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("All the info")]
        [RequireContext(ContextType.Guild)]
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
                        foreach (ParameterInfo param in command.Parameters)
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
        public async Task SignUpMessage([Remainder] string text)
        {
            var message = await Context.Guild.GetTextChannel(config.GetSignUpChannel()).SendMessageAsync(text);
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
            var emoteUS = new Emoji("🇺🇸");
            var emoteEU = new Emoji("🇪🇺");
            await message.AddReactionAsync(emoteUS);
            await message.AddReactionAsync(emoteEU);
            config.SaveRegionMessage(message);
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
            var roles = new List<IRole>
            {
                Context.Guild.GetRole(config.GetMatchARole()),
                Context.Guild.GetRole(config.GetMatchBRole()),
                Context.Guild.GetRole(config.GetMatchCRole()),
                Context.Guild.GetRole(config.GetMatchDRole()),
                Context.Guild.GetRole(config.GetMatchERole()),
                Context.Guild.GetRole(config.GetMatchFRole()),
                Context.Guild.GetRole(config.GetMatchGRole()),
                Context.Guild.GetRole(config.GetMatchHRole()),
                Context.Guild.GetRole(config.GetMatchIRole()),
                Context.Guild.GetRole(config.GetMatchJRole()),
                Context.Guild.GetRole(config.GetFinalistRole())
            };
            var message = await program.CreateBrackets(roles);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
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
            var message = await program.Score(Context.User.Id, scores);
            await Context.Channel.SendMessageAsync(message);
        }
    }
}
