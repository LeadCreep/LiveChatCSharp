using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace LiveChatC_.LiveChat
{
    class Program
    {
        private static DiscordSocketClient? client;
        private static SlashCommands? slashCommands;

        private static ulong guildId = 0;

        public static async Task Main()
        {
            var root = Directory.GetCurrentDirectory();
            var dotenvPath = Path.Combine(root, "..\\..\\..\\LiveChat\\.env");
            DotEnv.Load(dotenvPath);

            client = new DiscordSocketClient();
            if (client == null)
            {
                Console.WriteLine("Failed to initialize DiscordSocketClient.");
                return;
            }
            client.Ready += Client_Ready;

            slashCommands = new SlashCommands(client);

            client.Log += Log;

            string? token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            string? guildIdString = Environment.GetEnvironmentVariable("GUILD_ID");
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(guildIdString))
            {
                guildId = ulong.Parse(guildIdString);
            }

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static async Task Client_Ready()
        {
            Console.WriteLine("Bot is ready!");

            var guildCommand = new SlashCommandBuilder().WithName("list-roles")
                .WithDescription("List all roles in the server")
                .AddOption("user", ApplicationCommandOptionType.User, "The user", isRequired: true);

            try
            {
                if (client == null)
                {
                    return;
                }
                await client.Rest.CreateGuildCommand(guildCommand.Build(), guildId);
            }
            catch (HttpException ex)
            {
                var json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
                Console.WriteLine($"Error creating global command: {json}");
            }
        }
    }
}
