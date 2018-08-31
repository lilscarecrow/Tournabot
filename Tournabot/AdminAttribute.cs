using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public class AdminAttribute : PreconditionAttribute
{
    public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var user = context.User as SocketGuildUser;
        foreach(IRole role in user.Roles)
        {
            if(role.Name == "Admin")
                return PreconditionResult.FromSuccess();
        }
        await Task.CompletedTask;
        return PreconditionResult.FromError("You must be the owner of the bot to run this command.");   
    }
}
