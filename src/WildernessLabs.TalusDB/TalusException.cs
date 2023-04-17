using System;

namespace WildernessLabs.TalusDB
{
    /// <summary>
    /// An Exception thrown by talusDB
    /// </summary>
    public class TalusException : Exception
    {
        internal TalusException(string message)
            : base(message)
        {
        }
    }
}
