namespace BasicFleetServer.Utils
{
    public static class MSG_Translator
    {
        public static string ConstructNetworkMessage(string cmd, string[] args)
        {
            return cmd + "|" + string.Join("|", args);
        }

        public static (string cmd, string[] args) ParseNetworkMessage(string message)
        {
            string[] parts = message.Split('|');
            if (parts.Length == 0)
            {
                throw new ArgumentException("Invalid message format.");
            }
            string cmd = parts[0];
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            return (cmd, args);
        }
    }
}
