using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WildernessLabs.TalusDB
{
    /// <summary>
    /// A set of TalusDB Tables
    /// </summary>
    public class Database
    {
        private Dictionary<Type, object> _tableCache = new Dictionary<Type, object>();

        /// <summary>
        /// Gets the folder path that holds all of the TalusDB Tables
        /// </summary>
        public string RootFolder { get; }

        /// <summary>
        /// Creates a new TalusDB Database
        /// </summary>
        /// <param name="rootFolder">Optional root folder for teh Database</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public Database(string? rootFolder = null)
        {
            if (rootFolder == null)
            {
                rootFolder = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                if (!Directory.Exists(rootFolder))
                {
                    throw new DirectoryNotFoundException(rootFolder);
                }
            }

            var di = new DirectoryInfo(Path.Combine(rootFolder, ".talusdb"));

            if (!di.Exists)
            {
                di.Create();
            }

            RootFolder = di.FullName;
        }

        private void AddTableMeta<T>() where T : struct
        {
            var type = typeof(T);
            var path = Path.Combine(RootFolder, ".meta");
            var lines = File.ReadAllLines(path).ToList();
            if (!lines.Any(l => l.StartsWith($"{type.Name}|")))
            {
                lines.Add($"{type.Name}|{type.AssemblyQualifiedName}");
                File.Delete(path);
                File.WriteAllLines(path, lines);
            }
        }

        /// <summary>
        /// Creates a TalusDB Table for the give blittable type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maxElements"></param>
        /// <returns></returns>
        /// <exception cref="TalusException"></exception>
        public Table<T> CreateTable<T>(int maxElements) where T : struct
        {
            if (TableExists<T>())
            {
                throw new TalusException("Table already exists");
            }

            lock (_tableCache)
            {
                var table = new Table<T>(RootFolder, maxElements);
                AddTableMeta<T>();
                _tableCache.Add(typeof(T), table);
                return table;
            }
        }

        /// <summary>
        /// Drops (deletes) a Table by Table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <exception cref="TalusException"></exception>
        public void DropTable(string tableName)
        {
            if (!TableExists(tableName))
            {
                throw new TalusException("Table not found");
            }

            lock (_tableCache)
            {
                var existing = _tableCache.FirstOrDefault(t => t.Key.Name == tableName);
                if (!existing.Equals(default(KeyValuePair<Type, object>)))
                {
                    _tableCache.Remove(existing.Key);
                }
            }

            var path = Path.Combine(RootFolder, ".meta");
            var lines = File.ReadAllLines(path).Where(l => !l.StartsWith($"{tableName}|"));
            File.Delete(path);
            File.WriteAllLines(path, lines);

            var fi = new FileInfo(Path.Combine(RootFolder, tableName));
            fi.Delete();
        }

        /// <summary>
        /// Drops (deletes) a Table by the Type is stores
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="TalusException"></exception>
        public void DropTable<T>() where T : struct
        {
            if (!TableExists<T>())
            {
                throw new TalusException("Table not found");
            }

            lock (_tableCache)
            {
                if (_tableCache.ContainsKey(typeof(T)))
                {
                    _tableCache.Remove(typeof(T));
                }
            }

            var type = typeof(T);
            var path = Path.Combine(RootFolder, ".meta");
            var lines = File.ReadAllLines(path).Where(l => !l.StartsWith($"{type.Name}|"));
            File.Delete(path);
            File.WriteAllLines(path, lines);

            var fi = new FileInfo(Path.Combine(RootFolder, type.Name));
            fi.Delete();
        }

        /// <summary>
        /// Checks for the existance of a Table by name
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool TableExists(string tableName)
        {
            lock (_tableCache)
            {
                if (_tableCache.Any(t => t.Key.Name == tableName))
                {
                    return true;
                }
            }

            var path = Path.Combine(RootFolder, ".meta");
            var lines = File.ReadAllLines(path).ToList();
            return lines.Any(l => l.StartsWith($"{tableName}|"));
        }

        /// <summary>
        /// Checks for the existance of a Table by Element Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TableExists<T>() where T : struct
        {
            lock (_tableCache)
            {
                if (_tableCache.ContainsKey(typeof(T)))
                {
                    return true;
                }
            }

            var type = typeof(T);
            var path = Path.Combine(RootFolder, ".meta");
            var lines = File.ReadAllLines(path).ToList();
            return lines.Any(l => l.StartsWith($"{type.Name}|"));
        }

        /// <summary>
        /// Gets an existing Table for a specified Element Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="TalusException"></exception>
        public Table<T> GetTable<T>() where T : struct
        {
            if (!TableExists<T>())
            {
                throw new TalusException("Table not found");
            }

            lock (_tableCache)
            {
                if (_tableCache.ContainsKey(typeof(T)))
                {
                    var t = _tableCache[typeof(T)] as Table<T>;
                    if (t == null) throw new TalusException("Unable to cast table");
                    return t;
                }

                if (!TableExists<T>())
                {
                    throw new TalusException("Table not found");
                }

                var table = Table<T>.Open(RootFolder);

                _tableCache.Add(typeof(T), table);

                return table;
            }
        }

        /// <summary>
        /// Gets an array of all Table names
        /// </summary>
        /// <returns></returns>
        public string[] GetTableNames()
        {
            var path = Path.Combine(RootFolder, ".meta");
            if (!File.Exists(path))
            {
                File.CreateText(path).Close();
                return new string[0];
            }
            return File.ReadAllLines(path).Select(l => l.Substring(0, l.IndexOf('|'))).ToArray();
        }
    }
}
