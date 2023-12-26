using System;

namespace WildernessLabs.TalusDB
{
    public interface ITable
    {
        /// <summary>
        /// Fires when an element is added to the Table
        /// </summary>
        public event EventHandler ItemAdded;
        /// <summary>
        /// Fires when an element is added to a Table when it is already full
        /// </summary>
        public event EventHandler Overrun;
        /// <summary>
        /// Fires when an attempt is made to remove an item from an empty Table
        /// </summary>
        public event EventHandler Underrun;
        /// <summary>
        /// Fires when the number of elements in a Table reaches a non-zero HighWaterLevel value on an Enqueue call.  This event fires only once when passing upward across the boundary.
        /// </summary>
        public event EventHandler HighWater;
        /// <summary>
        /// Fires when the number of elements in a table reaches a non-zero LowWaterLevel value on a Remove call.  This event fires only once when passing downward across the boundary.
        /// </summary>
        public event EventHandler LowWater;

        public int Count { get; }

        public bool PublicationEnabled { get; }
    }

    public interface ITable<T> : ITable
    {
        void Insert(T element);
        T? Remove();
    }
}
