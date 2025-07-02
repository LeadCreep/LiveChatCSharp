using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace LiveChatC_.LiveChat
{
    public class SlashCommands
    {
        public static readonly string AttachmentDirectory = Path.Combine("..\\..\\..\\LiveChat\\Attachments\\");

        private static readonly int defaultImageDuration = 5;
        private static readonly int maxVideoDuration = 30;
        private static readonly int maxAudioDuration = 20;

        private static int lastRequestId = 0;

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
                .WithDescription("Afficher une image [.png / .jpg / .jpeg / .gif]")
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

            var linkGuildCommand = new SlashCommandBuilder()
                .WithName("link")
                .WithDescription("Afficher un lien")
                .AddOption("url", ApplicationCommandOptionType.String, "URL to show", isRequired: true)
                .AddOption("text", ApplicationCommandOptionType.String, "Text to add", isRequired: false)
                .AddOption("duration", ApplicationCommandOptionType.Number, "Duration in seconds to display the link", isRequired: false, minValue: 1, maxValue: 60);
            applicationCommandProperties.Add(linkGuildCommand.Build());

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
                //case "link":
                //    await Link(command);
                //    break;
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

            if (!attachement.Filename.EndsWith(".png") && !attachement.Filename.EndsWith(".jpg") && !attachement.Filename.EndsWith(".jpeg") && !attachement.Filename.EndsWith(".gif"))
            {
                await command.RespondAsync("Le fichier n'est pas une image valide.", ephemeral: true);
                return;
            }

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : defaultImageDuration;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, lastRequestId++.ToString(), attachement.Filename);

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

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : maxVideoDuration;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, lastRequestId++.ToString(), attachement.Filename);

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

            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : maxAudioDuration;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            string userName = command.User.Username;

            string filePath = Path.Combine(AttachmentDirectory, lastRequestId++.ToString(), attachement.Filename);

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

        private static async Task Link(SocketSlashCommand command)
        {
            string url = command.Data.Options.FirstOrDefault(x => x.Name == "url")?.Value?.ToString() ?? string.Empty;
            string text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString() ?? string.Empty;
            float duration = command.Data.Options.FirstOrDefault(x => x.Name == "duration")?.Value is double d ? (float)d : defaultImageDuration;
            string userName = command.User.Username;

            if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                await command.RespondAsync("URL invalide.", ephemeral: true);
                return;
            }

            try
            {
                await DownloadFromLink(command, url, Path.Combine(AttachmentDirectory, "link_content.txt"), duration, text, userName);

                string fileName = url.Split('/').LastOrDefault() ?? "downloaded_file";
                string filePath = Path.Combine(AttachmentDirectory, fileName);

                switch (fileName.Substring(fileName.Length - 4))
                {
                    case ".png":
                    case ".jpg":
                    case "jpeg":
                    case ".gif":
                        WebPageHandler.Instance.AddRequest(RequestType.Image, fileName, filePath, duration, text, userName);
                        break;
                    case ".mp4":
                    case ".avi":
                    case ".mov":
                        WebPageHandler.Instance.AddRequest(RequestType.Video, fileName, filePath, duration, text, userName);
                        break;
                    case ".mp3":
                    case ".wav":
                    case ".ogg":
                        WebPageHandler.Instance.AddRequest(RequestType.Audio, fileName, filePath, duration, text, userName);
                        break;
                    default:
                        await command.RespondAsync($"Le type de fichier {fileName.Substring(fileName.Length - 4)} n'est pas supporté. from {fileName}", ephemeral: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                await command.RespondAsync($"Erreur lors de la récupération du lien: {ex.Message}", ephemeral: true);
                return;
            }
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

        private static async Task DownloadFromLink(SocketSlashCommand command, string url, string destinationPath, float duration, string text = "", string userName = "")
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await command.RespondAsync("Erreur lors de la récupération du lien.", ephemeral: true);
                    return;
                }

                // Save the linked content
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                string fileName = url.Split('/').LastOrDefault() ?? "downloaded_file";
                string filePath = Path.Combine(AttachmentDirectory, fileName);
                await File.WriteAllBytesAsync(filePath, fileBytes);

                Console.WriteLine($"File downloaded: {fileName} to {filePath}");

                await command.RespondAsync($"Lien reçu: {url}");
                return;
            }
        }
    }
}