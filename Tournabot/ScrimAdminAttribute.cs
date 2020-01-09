using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tournabot;

public class ScrimAdminAttribute : PreconditionAttribute
{
    public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var guild = await context.Client.GetGuildAsync(services.GetService<ConfigHandler>().GetGuild());
        var user = await guild.GetUserAsync(context.User.Id) as SocketGuildUser;
        foreach (IRole role in user.Roles)
        {
            if (role.Name == "Scrim Admin")
                return PreconditionResult.FromSuccess();
        }
        await Task.CompletedTask;
        return PreconditionResult.FromError("You must be a Scrim Admin to run this command.");
    }
}
