using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WildernessLabs.TalusDB
{
    /// <summary>
    /// Base class for Database Publishers
    /// </summary>
    public abstract class PublisherBase : IPublisher
    {
        private AutoResetEvent _dataReadyEvent = new AutoResetEvent(false);
        private Dictionary<ITable, TableMeta> _tables = new Dictionary<ITable, TableMeta>();

        /// <summary>
        /// Period on which the publisher will check the database for data to publish
        /// </summary>
        public TimeSpan PublicationPeriod { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// override this method with your publication logic
        /// </summary>
        /// <param name="item"></param>
        /// <returns><b>True</b> on successful publication, otehrwise <b>False</b></returns>
        public abstract Task<bool> PublishItem(object item);

        /// <summary>
        /// Base constructor for a Publisher
        /// </summary>
        /// <param name="database"></param>
        public PublisherBase(Database database)
        {
            foreach (var table in database.GetTables())
            {
                AddTable(table);
            }

            database.TableAdded += (e, t) =>
            {
                AddTable(t);
            };

            new Thread(() => _ = PublicationProc())
            {
                IsBackground = true,
                Name = "TalusDBPublisher"
            }
            .Start();
        }

        private void AddTable(ITable table)
        {
            table.ItemAdded += (sender, e) =>
            {
                _dataReadyEvent.Set();
            };

            var meta = new TableMeta
            {
                TableType = table.GetType().GenericTypeArguments[0],
                PeekMethod = table.GetType().GetMethod("Peek"),
                RemoveMethod = table.GetType().GetMethod("Remove")
            };

            _tables.Add(table, meta);
        }

        private async Task PublicationProc()
        {
            while (true)
            {
                // wait for either an item to get added *or* the publication period
                // this allows immediate attempts to publish, but periodic tries if we fail for any reason
                _dataReadyEvent.WaitOne(PublicationPeriod);

                foreach (var table in _tables)
                {
                    while (table.Key.PublicationEnabled && table.Key.Count > 0)
                    {
                        var row = table.Value.PeekMethod.Invoke(table.Key, null);

                        var published = false;

                        try
                        {
                            published = await PublishItem(row);
                        }
                        catch (Exception ex)
                        {
                            // TODO: raise an event maybe?
                            Debug.WriteLine(ex.Message);
                        }

                        if (published)
                        {
                            // publish was successful, remove item from the table
                            table.Value.RemoveMethod.Invoke(table.Key, null);
                        }
                    }
                }
            }
        }

        private class TableMeta
        {
            public Type TableType { get; set; } = default!;
            public MethodInfo PeekMethod { get; set; } = default!;
            public MethodInfo RemoveMethod { get; set; } = default!;
        }
    }
}
