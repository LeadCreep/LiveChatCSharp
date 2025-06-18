namespace LiveChatC_.LiveChat
{
    public enum RequestType { Image, Video, Audio }

    public class Request(RequestType type, string fileName, string filePath, float requestDurationSeconds, string text = "", string userName = "")
    {
        private readonly RequestType type = type;
        private readonly string fileName = fileName;
        private readonly string filePath = filePath;
        private float requestDurationSeconds = requestDurationSeconds;
        private readonly string text = text;
        private readonly string userName = userName;

        public RequestType Type { get => type; }
        public string FileName { get => fileName; }
        public string FilePath { get => filePath; }
        public float RequestDurationSeconds { get { return requestDurationSeconds; } set { requestDurationSeconds = value; } }
        public string Text { get => text; }
        public string UserName { get => userName; }
    }

    public class ImageRequest(string fileName, string filePath, float requestDurationSeconds, string text = "", string userName = "") : Request(RequestType.Image, fileName, filePath, requestDurationSeconds, text, userName)
    {
    }

    public class VideoRequest(string fileName, string filePath, float requestDurationSeconds, string text = "", string userName = "") : Request(RequestType.Video, fileName, filePath, requestDurationSeconds, text, userName)
    {
    }

    public class AudioRequest(string fileName, string filePath, float requestDurationSeconds, string text = "", string userName = "") : Request(RequestType.Audio, fileName, filePath, requestDurationSeconds, text, userName)
    {
    }

    public class FIFOQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (queue)
            {
                queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            lock (queue)
            {
                if (queue.Count == 0)
                    throw new InvalidOperationException("Queue is empty.");
                return queue.Dequeue();
            }
        }

        public int Count
        {
            get
            {
                lock (queue)
                {
                    return queue.Count;
                }
            }
        }
    }
}
