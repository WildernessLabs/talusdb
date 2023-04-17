using System;

namespace WildernessLabs.TalusDB
{
    public class TalusException : Exception
    {
        internal TalusException(string message)
            : base(message)
        {
        }
    }
}
