using Discord;
using Discord.WebSocket;

namespace LiveChatC_.LiveChat
{
    class Program
    {
        private const string DotEnvFileLocation = "..\\..\\..\\LiveChat\\.env";

        private static DiscordSocketClient? client;
        private static SlashCommands? slashCommands;

        private static ulong guildId = 0;
        private static int port = 3000; // Default port for the web server

        public static async Task Main()
        {
            var root = AppDomain.CurrentDomain.BaseDirectory;
            var dotenvPath = Path.Combine(root, DotEnvFileLocation);
            DotEnv.Load(dotenvPath);

            DiscordSocketConfig config = new DiscordSocketConfig()
            {
                //GatewayIntents = GatewayIntents.None
            };

            client = new DiscordSocketClient(config);
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
            string? portString = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(portString) && int.TryParse(portString, out int parsedPort))
            {
                port = parsedPort;
            }

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            Task httpServerDeverse = WebPageHandler.DeverseWebPage(port);
            Task webPageLogic = WebPageHandler.Instance.WebPageLogic();

            await Task.WhenAll(httpServerDeverse, webPageLogic);

            await Task.Delay(-1);
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static async Task Client_Ready()
        {
            Console.WriteLine("Le Bot est près !");

            Console.WriteLine("Inviter le bot avec :");

            if (slashCommands == null || client == null)
            {
                Console.WriteLine("SlashCommands or DiscordSocketClient is not initialized.");
                return;
            }

            await slashCommands.CreateSlashCommands(client, guildId);

            Console.WriteLine("https://discord.com/oauth2/authorize?client_id=" + Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") + "&permissions=2147552320&integration_type=0&scope=bot");
        }
    }
}
