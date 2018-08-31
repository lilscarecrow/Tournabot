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
            var message = await program.AddMember(discordTag, name);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("status", RunMode = RunMode.Async)]
        [Summary("Check status of member")]
        [RequireContext(ContextType.DM)]
        public async Task Status()
        {
            var discordTag = Context.User.Username + "#" + Context.User.DiscriminatorValue;
            var message = await program.GetMember(discordTag);
            await Context.Channel.SendMessageAsync(message);
        }

        [Command("unregister", RunMode = RunMode.Async)]
        [Summary("Unregister for the tourney")]
        [RequireContext(ContextType.DM)]
        public async Task Unregister()
        {
            var discordTag = Context.User.Username + "#" + Context.User.DiscriminatorValue;
            var message = await program.Unregister(discordTag);
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
                    builder.Append("```");
                    if (mod.Group == "admin")
                        builder.Append("```Admin Commands:``````");
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
                }
                builder.Append("```\n");
                await Context.Channel.SendMessageAsync(builder.Length == 0 ? "N/A" : builder.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }

    [Group("admin")]
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
        [Summary("Clear entries in DB")]
        [RequireContext(ContextType.Guild)]
        public async Task ClearSignUps()
        {
            var message = await program.ClearSignUp();
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("removeUserSignUp", RunMode = RunMode.Async)]
        [Summary("Clear entries in DB")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveUserSignUp(string discordTag)
        {
            var message = await program.RemoveSignUp(discordTag);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }

        [Command("findMember", RunMode = RunMode.Async)]
        [Summary("Clear entries in DB")]
        [RequireContext(ContextType.Guild)]
        public async Task FindMember(string name)
        {
            var message = await program.FindMember(name);
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync(message);
        }
    }
}
