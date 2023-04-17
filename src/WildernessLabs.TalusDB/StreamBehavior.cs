namespace WildernessLabs.TalusDB
{
    /// <summary>
    /// The stream behavior used by a Table when accessing a file
    /// </summary>
    public enum StreamBehavior
    {
        /// <summary>
        /// Always create a new Stream for every action
        /// </summary>
        AlwaysNew,
        /// <summary>
        /// Maintain a single. alway-open stream to the file
        /// </summary>
        KeepOpen
    }
}
