using System;

namespace RainLanguageServer
{
    public enum MessageType
    {
        /// <summary>
        /// Language server errors.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Language server warnings.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Language server internal information.
        /// </summary>
        Info = 3,

        /// <summary>
        /// Language server log-level diagnostic messages.
        /// </summary>
        Log = 4
    }
    [Serializable]
    public sealed class LogMessageParams
    {
        public MessageType type;
        public string message;
    }
}
