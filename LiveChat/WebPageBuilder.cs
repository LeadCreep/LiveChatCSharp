namespace LiveChatC_.LiveChat
{
    public class WebPageBuilder
    {
        private string webPage = string.Empty;

        private static WebPageBuilder? instance = null;
        public static WebPageBuilder Instance
        {
            get
            {
                instance ??= new WebPageBuilder();
                return instance;
            }
        }

        public string WebPage
        {
            get
            {
                return "<html><head><title>Live Chat</title></head><body>" + SSECode + webPage + "</body></html>";
            }
        }

        public const string SSECode = @"
            <script>
                const es = new EventSource('/events');

                es.addEventListener('refresh', () => {
                  location.reload();
                });

                es.addEventListener('ping', () => { /* heartbeat */ });

                es.onerror = (e) => {
                  console.error('SSE onerror', e);
                };
            </script>";

        public WebPageBuilder()
        {
            webPage = string.Empty;
        }

        public void AddImage(string fileName, string filePath)
        {
            webPage += $"<div style=\"display:flex; justify-content:center; align-items:center; width:100%; height:100%;\">" +
                       $"<img src=\"{filePath}\" alt=\"{fileName}\" style=\"max-width:100%; max-height:100%;\" />" +
                       $"</div>\n";
        }

        public void AddVideo(string fileName, string filePath)
        {
            webPage += $"<div style=\"display:flex; justify-content:center; align-items:center; width:100%; height:100%;\">" +
                       $"<video controls autoplay style=\"max-width:100%; max-height:100%;\">" +
                       $"<source src=\"{filePath}\" alt=\"{fileName}\" type=\"video/mp4\">" +
                       "Your browser does not support the video tag." +
                       "</video></div>\n";
        }

        public void AddAudio(string fileName, string filePath)
        {
            webPage += $"<div style=\"display:flex; justify-content:center; align-items:center; width:100%; height:100%;\">" +
                       $"<audio controls autoplay>" +
                       $"<source src=\"{filePath}\" alt=\"{fileName}\" type=\"audio/mpeg\">" +
                       "Your browser does not support the audio tag." +
                       "</audio></div>\n";
        }

        public void AddText(string text, float size = 64, int posx = 0, int posy = 0, string font = "Impact", bool center = true, float strokeWidth = 3f)
        {
            if (center)
            {
                webPage += $"<div style=\"display:flex; justify-content:center; align-items:center; position:absolute; width:100%; top:{posy}px; font-size:{size}px; font-family:{font}; color: white; padding: 5px; -webkit-text-stroke-width:{strokeWidth}px; -webkit-text-stroke-color: black;\">{text}</div>\n";
            }
            else
            {
                webPage += $"<div style=\"display:flex; justify-content:center; align-items:center; position:absolute; left:{posx}px; top:{posy}px; font-size:{size}px; font-family:{font}; color: white; padding: 5px; -webkit-text-stroke-width:{strokeWidth}px; -webkit-text-stroke-color: black;\">{text}</div>\n";
            }
        }

        public void RemoveAll()
        {
            webPage = string.Empty;
        }
    }
}