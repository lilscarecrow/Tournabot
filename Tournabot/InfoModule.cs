using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tournabot
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public Program program { get; set; }
        public ToornamentService toornamentService { get; set; }
        public ConfigHandler config { get; set; }

        IRole checkedInRole, signedUpRole;

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("A simple test")]
        [RequireContext(ContextType.Guild)]
        public async Task Ping()
        {
            await Context.Channel.SendMessageAsync("pong!");
        }

        [Command("register", RunMode = RunMode.Async)]
        [Summary("A simple test")]
        [RequireContext(ContextType.Guild)]
        public async Task Register()
        {
            await Context.Channel.SendMessageAsync("Register Here:\n" + $"https://www.toornament.com/tournaments/{config.GetTournamentSessionId()}/registration/");
        }

        [Command("checkRole", RunMode = RunMode.Async)]
        [Summary("Checks roles")]
        [RequireContext(ContextType.Guild)]
        public async Task CheckRole()
        {
            checkedInRole = Context.Guild.GetRole(config.GetTournamentCheckedInRole());
            signedUpRole = Context.Guild.GetRole(config.GetTournamentRegisteredRole());

            var ids = await toornamentService.RequestIds();

            StringBuilder checkedInBuilder = new StringBuilder();
            StringBuilder signedUpBuilder = new StringBuilder();

            foreach (KeyValuePair<string, bool> id in ids)
            {
                foreach (IGuildUser user in Context.Guild.Users)
                {
                    if (user.Username + "#" + user.DiscriminatorValue == id.Key)
                    {
                        if (!FindRole(user, signedUpRole.Id))
                        {
                            await user.AddRoleAsync(signedUpRole);
                            signedUpBuilder.AppendLine(user.Nickname ?? user.Username);
                        }
                        if (id.Value)
                        {
                            if(!FindRole(user, checkedInRole.Id))
                            {
                                await user.AddRoleAsync(checkedInRole);
                                checkedInBuilder.AppendLine(user.Nickname ?? user.Username);
                            }
                        }
                    }
                }
            }
            await Context.Channel.SendMessageAsync("", embed: buildEmbed(signedUpBuilder.ToString(),checkedInBuilder.ToString()));
        }

        [Command("clearRole", RunMode = RunMode.Async)]
        [Summary("Clear roles")]
        [RequireContext(ContextType.Guild)]
        public async Task ClearRole()
        {
            var count = 0;
            checkedInRole = Context.Guild.GetRole(config.GetTournamentCheckedInRole());
            signedUpRole = Context.Guild.GetRole(config.GetTournamentRegisteredRole());
            foreach (IGuildUser user in Context.Guild.Users)
            {
                if (FindRole(user, signedUpRole.Id))
                {
                    await user.RemoveRoleAsync(signedUpRole);
                    count++;
                    if (FindRole(user, checkedInRole.Id))
                    {
                        await user.RemoveRoleAsync(checkedInRole);
                    }
                }
            }
            await Context.Channel.SendMessageAsync("Removed roles from " + count + " members!");
        }

        private Embed buildEmbed(string signedUp, string checkedIn)
        {
            var builder = new EmbedBuilder()
                .WithTitle("***TOORNAMENT UPDATES***")
                .WithDescription("")
                .WithColor(new Color(0x00f9ff))
                .AddField("Added Signed-Up role for the following members:", "```\n" + signedUp + "\n```")
                .AddField("Added Checked-In role for the following members:", "```\n" + checkedIn + "\n```")
                .WithFooter(footer =>
                {
                    footer.WithText("Powered by Toornament");
                });
            return builder.Build();
        }

        private bool FindRole(IGuildUser user, ulong roleId)
        {
            foreach (var role in user.RoleIds)
            {
                if (role == roleId)
                    return true;
            }
            return false;
        }
    }
}
