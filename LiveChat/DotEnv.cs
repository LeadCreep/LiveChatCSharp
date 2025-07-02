namespace LiveChatC_.LiveChat
{
    public static class DotEnv
    {
        public static void Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Le fichier .env n'as pas été trouvé à : {path}");
            }
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue; // Skip empty lines and comments
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue; // Skip malformed lines
                // Remove comment after line if present
                parts[1] = parts[1].Split('#')[0].Trim(); // Remove comment part
                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('"'); // Remove surrounding quotes if present
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
