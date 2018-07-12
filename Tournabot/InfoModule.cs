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

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("A simple test")]
        [RequireContext(ContextType.Guild)]
        public async Task Ping()
        {
            await Context.Channel.SendMessageAsync("pong!");
        }

        [Command("checkRole", RunMode = RunMode.Async)]
        [Summary("Checks roles")]
        [RequireContext(ContextType.Guild)]
        public async Task CheckRole()
        {
            SocketGuildUser user = Context.User as SocketGuildUser;

            //await Context.Channel.SendMessageAsync("pong!");
        }
    }
}
