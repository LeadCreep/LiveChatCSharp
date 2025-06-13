using Discord;
using Discord.WebSocket;

namespace LiveChatC_.LiveChat
{
    public class SlashCommands
    {
        public SlashCommands(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "ping":
                    await Ping(command);
                    break;
                case "list-roles":
                    await ListRoles(command);
                    break;
                default:
                    await command.RespondAsync("Should never run here !");
                    break;
            }
        }

        private static async Task ListRoles(SocketSlashCommand command)
        {
            var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

            var roleList = string.Join("\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
                .WithTitle("Roles")
                .WithDescription(roleList)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
        }

        private static async Task Ping(SocketSlashCommand command)
        {
            await command.RespondAsync("Pong!");
        }
    }
}
