using System;

namespace WildernessLabs.TalusDB
{
    public interface ITable
    {
        /// <summary>
        /// Fires when an element is added to the Table
        /// </summary>
        event EventHandler ItemAdded;
        /// <summary>
        /// Fires when an element is added to a Table when it is already full
        /// </summary>
        event EventHandler Overrun;
        /// <summary>
        /// Fires when an attempt is made to remove an item from an empty Table
        /// </summary>
        event EventHandler Underrun;
        /// <summary>
        /// Fires when the number of elements in a Table reaches a non-zero HighWaterLevel value on an Enqueue call.  This event fires only once when passing upward across the boundary.
        /// </summary>
        event EventHandler HighWater;
        /// <summary>
        /// Fires when the number of elements in a table reaches a non-zero LowWaterLevel value on a Remove call.  This event fires only once when passing downward across the boundary.
        /// </summary>
        event EventHandler LowWater;

        /// <summary>
        /// Closes any open streams associated with this table
        /// </summary>
        void Close();

        /// <summary>
        /// Get the current count of rows in the Table
        /// </summary>
        int Count { get; }

        bool PublicationEnabled { get; }
    }

    public interface ITable<T> : ITable
    {
        void Insert(T element);
        T? Remove();
    }
}
