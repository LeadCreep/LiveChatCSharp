using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace LiveChatC_.LiveChat
{
    public class SlashCommands
    {
        public static readonly string AttachmentDirectory = Path.Combine("..\\..\\..\\LiveChat\\Attachments\\");

        public SlashCommands(DiscordSocketClient client)
        {
            client.SlashCommandExecuted += SlashCommandHandler;
        }

        public async Task CreateSlashCommands(DiscordSocketClient client, ulong guildId)
        {
            List<ApplicationCommandProperties> applicationCommandProperties = [];

            var pingGuildCommand = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Ping le bot");
            applicationCommandProperties.Add(pingGuildCommand.Build());

            var imgGuildCommand = new SlashCommandBuilder()
                .WithName("img")
                .WithDescription("Afficher une image [.png / .jpg / .jpeg]")
                .AddOption("file", ApplicationCommandOptionType.Attachment, "Image to show", isRequired: true)
                .AddOption("text", ApplicationCommandOptionType.String, "Text to add", isRequired: false)
                .AddOption("duration", ApplicationCommandOptionType.Number, "Duration in seconds to display the image", isRequired: false, minValue: 1, maxValue: 60);
            applicationCommandProperties.Add(imgGuildCommand.Build());

            var vidGuildCommand = new SlashCommandBuilder()
                .WithName("vid")
                .WithDescription("Afficher une vidéo [.mp4 / .avi / .mov]")
                .AddOption("file", ApplicationCommandOptionType.Attachment, "Video to show", isRequired: true)
                .AddOption("text", ApplicationCommandOptionType.String, "Text to add", isRequired: false)
                .AddOption("duration", ApplicationCommandOptionType.Number, "Duration in seconds to display the video, if you want to cut", isRequired: false, minValue: 1, maxValue: 60);
            applicationCommandProperties.Add(vidGuildCommand.Build());

            var audioGuildCommand = new SlashCommandBuilder()
                .WithName("audio")
                .WithDescription("Jouer un fichier audio [.mp3 / .wav / .ogg]")
                .AddOption("file", ApplicationCommandOptionType.Attachment, "Audio to show", isRequired: true)
                .AddOption("text", ApplicationCommandOptionType.String, "Text to add", isRequired: false)
                .AddOption("duration", ApplicationCommandOptionType.Number, "Duration in seconds to play the audio, if you want to cut", isRequired: false, minValue: 1, maxValue: 60);
            applicationCommandProperties.Add(audioGuildCommand.Build());

            try
            {
                await client.Rest.BulkOverwriteGuildCommands(applicationCommandProperties.ToArray(), guildId);
            }
            catch (HttpException ex)
            {
                var json = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
                Console.WriteLine($"Error creating global command: {json}");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "ping":
                    await Ping(command);
                    break;
                case "img":
                    await Image(command);
                    break;
                case "vid":
                    await Video(command);
                    break;
                case "audio":
                    await Audio(command);
                    break;
                default:
                    await command.RespondAsync("Should never run here !");
                    break;
            }
        }

        private static async Task Ping(SocketSlashCommand command)
        {
            await command.RespondAsync("Pong!");
        }

        private static async Task Image(SocketSlashCommand command)
        {
            IAttachment attachement = (IAttachment)command.Data.Options.First().Value;

            if (!attachement.Filename.EndsWith(".png") && !attachement.Filename.EndsWith(".jpg") && !attachement.Filename.EndsWith(".jpeg"))
            {
                await command.RespondAsync("Le fichier n'est pas une image valide.", ephemeral: true);
                return;
            }

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : 5;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, attachement.Filename);

            try
            {
                await DownloadAttachment(attachement, filePath);
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"Erreur lors du téléchargement de l'image: {ex.Message}", ephemeral: true);
                return;
            }

            WebPageHandler.Instance.AddRequest(RequestType.Image, attachement.Filename, filePath, duration, text, userName);

            await command.RespondAsync($"Image reçue: {attachement.Filename}");
        }

        private static async Task Video(SocketSlashCommand command)
        {
            IAttachment attachement = (IAttachment)command.Data.Options.First().Value;

            if (!attachement.Filename.EndsWith(".mp4") && !attachement.Filename.EndsWith(".avi") && !attachement.Filename.EndsWith(".mov"))
            {
                await command.RespondAsync("Le fichier n'est pas une vidéo valide.", ephemeral: true);
                return;
            }

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : 0;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, attachement.Filename);

            try
            {
                await DownloadAttachment(attachement, filePath);
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"Erreur lors du téléchargement de la vidéo: {ex.Message}", ephemeral: true);
                return;
            }

            WebPageHandler.Instance.AddRequest(RequestType.Video, attachement.Filename, filePath, duration, text, userName);

            await command.RespondAsync($"Vidéo reçue: {attachement.Filename}");
        }

        private static async Task Audio(SocketSlashCommand command)
        {
            IAttachment attachement = (IAttachment)command.Data.Options.First().Value;

            if (!attachement.Filename.EndsWith(".mp3") && !attachement.Filename.EndsWith(".wav") && !attachement.Filename.EndsWith(".ogg"))
            {
                await command.RespondAsync("Le fichier n'est pas un audio valide.", ephemeral: true);
                return;
            }

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : 0;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, attachement.Filename);

            try
            {
                await DownloadAttachment(attachement, filePath);
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"Erreur lors du téléchargement de l'audio: {ex.Message}", ephemeral: true);
                return;
            }

            WebPageHandler.Instance.AddRequest(RequestType.Audio, attachement.Filename, filePath, duration, text, userName);

            await command.RespondAsync($"Audio reçu: {attachement.Filename}");
        }

        private static async Task DownloadAttachment(IAttachment attachment, string filePath)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(attachment.Url);
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, fileBytes);
                }
                else
                {
                    Console.WriteLine($"Failed to download attachment: {attachment.Filename}");
                }
            }
        }
    }
}