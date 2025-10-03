using FFmpeg.NET;
using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace LiveChatC_.LiveChat
{
    public class WebPageHandler
    {
        private readonly FIFOQueue<Request> requestQueue = new();

        private static WebPageHandler? instance = null;
        private static Engine? ffmpeg;

        private sealed class SseClient
        {
            public Guid Id { get; } = Guid.NewGuid();
            public HttpListenerResponse Response { get; }
            public StreamWriter Writer { get; }
            public object Sync { get; } = new();
            public SseClient(HttpListenerResponse response, StreamWriter writer)
            {
                Response = response;
                Writer = writer;
            }
        }

        private static readonly ConcurrentDictionary<Guid, SseClient> sseClients = new();

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

            WebPageBuilder.Instance.AddImage(request.FileName, request.FilePath);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);

            SendRefreshEvent(); // Notify clients to refresh

            float time = Math.Min(request.RequestDurationSeconds, 30f);

            await Task.Delay((int)time * 1000); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            SendRefreshEvent(); // Notify clients to refresh

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

            WebPageBuilder.Instance.AddVideo(request.FileName, request.FilePath);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);

            SendRefreshEvent(); // Notify clients to refresh

            float time = Math.Min(request.RequestDurationSeconds, 60f);

            await Task.Delay((int)time * 1000); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            SendRefreshEvent(); // Notify clients to refresh

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

            WebPageBuilder.Instance.AddAudio(request.FileName, request.FilePath);
            WebPageBuilder.Instance.AddText(request.Text, posy: 900);
            WebPageBuilder.Instance.AddText(request.UserName, center: false, font: "Arial", size: 45, strokeWidth: 1);

            SendRefreshEvent(); // Notify clients to refresh

            float time = Math.Min(request.RequestDurationSeconds, 30f);

            await Task.Delay((int)time * 1000); // Simulate processing time

            WebPageBuilder.Instance.RemoveAll(); // Clear the webpage after processing

            SendRefreshEvent(); // Notify clients to refresh

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

        private static bool isFilePath(string path)
        {
            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task DeverseWeb(int port)
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

                    if (urlPath.Equals("/events", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleSse(context);
                        continue;
                    }

                    // Si la requête concerne un ficher
                    if (isFilePath(urlPath))
                    {
                        DeverseFile(response, urlPath);
                    }
                    else
                    {
                        DeverseWebPage(response);
                    }
                }
            }
            catch (HttpListenerException e)
            {
                DebugError(e.Message);
                return;
            }
        }

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

        public static void DebugError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + errorMessage);
            Console.WriteLine("Make sure you are running in administrator mode");
            Console.ResetColor();
        }

        private async static void DeverseWebPage(HttpListenerResponse response)
        {
            string responseString = WebPageBuilder.Instance.WebPage;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private async static void DeverseFile(HttpListenerResponse response, string urlPath)
        {
            string fileName = Path.GetFileName(urlPath);
            string filePath = Path.Combine(SlashCommands.AttachmentDirectory, fileName);

            if (File.Exists(filePath))
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                response.ContentLength64 = fileBytes.Length;
                response.ContentType = GetMime(fileName);
                try
                {
                    await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending file: {ex.Message}");
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                Console.WriteLine($"File not found: {filePath}");
            }
            response.Close();
        }

        private static void HandleSse(HttpListenerContext context)
        {
            var response = context.Response;
            response.ContentType = "text/event-stream";
            response.StatusCode = (int)HttpStatusCode.OK;
            response.SendChunked = true;
            response.KeepAlive = true;
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";

            var writer = new StreamWriter(response.OutputStream, Encoding.UTF8) { AutoFlush = true };
            var client = new SseClient(response, writer);
            sseClients[client.Id] = client;

            lock (client.Sync)
            {
                writer.WriteLine("retry: 1000\n");
                writer.WriteLine();
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15));
                        lock (client.Sync)
                        {
                            writer.WriteLine("event: ping");
                            writer.WriteLine("data: keep-alive");
                            writer.WriteLine();
                        }
                    }
                }
                catch
                {
                    // Ignorer
                }
                finally
                {
                    CleanupSseClient(client.Id);
                }
            });
        }

        private static void CleanupSseClient(Guid id)
        {
            if (sseClients.TryRemove(id, out var c))
            {
                try { c.Writer.Dispose(); } catch { }
                try { c.Response.Close(); } catch { }
            }
        }

        public static void SendRefreshEvent()
        {
            foreach (var kv in sseClients)
            {
                var client = kv.Value;
                try
                {
                    lock (client.Sync)
                    {
                        client.Writer.WriteLine("event: refresh");
                        client.Writer.WriteLine("data: now");
                        client.Writer.WriteLine();
                    }
                }
                catch
                {
                    CleanupSseClient(client.Id);
                }
            }
        }
    }
}
