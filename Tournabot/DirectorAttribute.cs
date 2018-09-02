using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tournabot;
using Tournabot.Models;

public class DirectorAttribute : PreconditionAttribute
{
    public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        using (var db = new DarwinDBContext(services.GetService<ConfigHandler>().GetSql()))
        {
            try
            {
                var director = await db.Directors.SingleOrDefaultAsync(u => u.Id == context.User.Id);
                if (director != null)
                    return PreconditionResult.FromSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return PreconditionResult.FromError("You must be a Director to run this command.");
    }
}