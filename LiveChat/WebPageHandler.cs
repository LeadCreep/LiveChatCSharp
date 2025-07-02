using FFmpeg.NET;
using System.Net;

namespace LiveChatC_.LiveChat
{
    public class WebPageHandler
    {
        private readonly FIFOQueue<Request> requestQueue = new();

        private static WebPageHandler? instance = null;
        private static Engine? ffmpeg;

        public static WebPageHandler Instance
        {
            get
            {
                instance ??= new WebPageHandler();
                return instance;
            }
        }

        private WebPageHandler()
        {
            ffmpeg = new Engine();
        }

        public async Task WebPageLogic()
        {
            while (true)
            {
                if (requestQueue.Count > 0)
                {
                    await HandleNextRequest();
                }
                await Task.Delay(1000);
            }
        }

        public async Task WebPageLoop()
        {
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        public void AddRequest(RequestType type, string fileName, string filePath, float requestDurationSeconds, string text = "", string userName = "")
        {
            requestQueue.Enqueue(new Request(type, fileName, filePath, requestDurationSeconds, text, userName));
        }

        public async Task HandleNextRequest()
        {
            if (requestQueue.Count == 0)
            {
                Console.WriteLine("No requests in the queue.");
                return;
            }
            Request request = requestQueue.Dequeue();

            switch (request.Type)
            {
                case RequestType.Image:
                    await HandleImageRequest(request);
                    break;
                case RequestType.Video:
                    await HandleVideoRequest(request);
                    break;
                case RequestType.Audio:
                    await HandleAudioRequest(request);
                    break;
                default:
                    throw new InvalidOperationException("Unknown request type.");
            }
        }

        public async Task HandleImageRequest(Request request)
        {
            Console.WriteLine($"Handling image request: {request.FileName}, Duration: {request.RequestDurationSeconds} seconds, Text: {request.Text}, User : {request.UserName}");

            WebPageBuilder.Instance.AddImage(request.FileName, request.FilePath, request.RequestDurationSeconds);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);

            await Task.Delay((int)(request.RequestDurationSeconds * 500)); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            if (File.Exists(request.FilePath))
            {
                try
                {
                    File.Delete(request.FilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting image file: {ex.Message}");
                }
            }
        }

        public async Task HandleVideoRequest(Request request)
        {
            Console.WriteLine($"Handling video request: {request.FileName}, Duration: {request.RequestDurationSeconds} seconds, Text: {request.Text}, User : {request.UserName}");

            if (request.RequestDurationSeconds <= 0)
            {
                request.RequestDurationSeconds = await GetVideoLength(request.FilePath);
            }

            WebPageBuilder.Instance.AddVideo(request.FileName, request.FilePath, request.RequestDurationSeconds);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);


            await Task.Delay((int)(request.RequestDurationSeconds * 500)); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            if (File.Exists(request.FilePath))
            {
                try
                {
                    File.Delete(request.FilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting video file: {ex.Message}");
                }
            }
        }

        public async Task HandleAudioRequest(Request request)
        {
            Console.WriteLine($"Handling audio request: {request.FileName}, Duration: {request.RequestDurationSeconds} seconds, Text: {request.Text}, User : {request.UserName}");

            if (request.RequestDurationSeconds <= 0)
            {
                request.RequestDurationSeconds = await GetAudioLength(request.FilePath);
            }

            WebPageBuilder.Instance.AddAudio(request.FileName, request.FilePath, request.RequestDurationSeconds);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);


            await Task.Delay((int)(request.RequestDurationSeconds * 500)); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            if (File.Exists(request.FilePath))
            {
                try
                {
                    File.Delete(request.FilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting audio file: {ex.Message}");
                }
            }
        }

        public static async Task DeverseWebPage(int port)
        {
            try
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://*:" + port + "/");
                listener.Start();
                Console.WriteLine("Envoie de la page web sur le port " + port);

                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string urlPath = request.Url.AbsolutePath;

                    // Si la requête concerne un ficher
                    if (urlPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                        urlPath.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = Path.GetFileName(urlPath);
                        string filePath = Path.Combine(SlashCommands.AttachmentDirectory, fileName);

                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                            response.ContentLength64 = fileBytes.Length;
                            response.ContentType = GetMime(fileName);
                            await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            Console.WriteLine($"File not found: {filePath}");
                        }
                        response.Close();
                        continue;
                    }

                    // Sinon, servir la page HTML
                    string responseString = WebPageBuilder.Instance.WebPage;
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                }
            }
            catch (HttpListenerException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error starting HttpListener: " + e.Message);
                Console.WriteLine("Make sure you run the application with administrator privileges to use HttpListener.");
                Console.ResetColor();
                return;
            }
        }

        // Méthode utilitaire pour obtenir le type MIME
        private static string GetMime(string fileName)
        {
            return SlashCommands.AttachmentDirectory;
        }

        private async static Task<float> GetAudioLength(string filePath)
        {
            if (ffmpeg == null)
                throw new InvalidOperationException("FFmpeg engine not initialized.");

            var inputFile = new InputFile(filePath);
            var cancellationToken = CancellationToken.None; // Provide a default CancellationToken
            var metaData = await ffmpeg.GetMetaDataAsync(inputFile, cancellationToken);

            if (metaData?.Duration != null)
            {
                if ((float)metaData.Duration.TotalSeconds > 20f)
                {
                    return 20f; // Limit audio duration to 20 seconds
                }
                return (float)metaData.Duration.TotalSeconds + 1f;
            }

            throw new InvalidOperationException("Impossible de récupérer la durée du fichier audio.");
        }

        private async static Task<float> GetVideoLength(string filePath)
        {
            if (ffmpeg == null)
                throw new InvalidOperationException("FFmpeg engine not initialized.");

            var inputFile = new InputFile(filePath);
            var cancellationToken = CancellationToken.None; // Provide a default CancellationToken
            var metaData = await ffmpeg.GetMetaDataAsync(inputFile, cancellationToken);

            if (metaData?.Duration != null)
            {
                if ((float)metaData.Duration.TotalSeconds > 30f)
                {
                    return 30f; // Limit video duration to 30 seconds
                }
                return (float)metaData.Duration.TotalSeconds + 1f;
            }

            throw new InvalidOperationException("Impossible de récupérer la durée du fichier vidéo.");
        }
    }
}
