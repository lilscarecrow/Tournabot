using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tournabot
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping", RunMode = RunMode.Async)]
        [Summary("A simple test")]
        [RequireContext(ContextType.Guild)]
        public async Task Ping()
        {
            await Context.Channel.SendMessageAsync("pong!");
        }
    }
}
