using System;

namespace WildernessLabs.TalusDB;

public static class TableFactory
{
    public static ITable<T> CreateTable<T>(string rootFolder, int maxElements) where T : new()
    {
        Type type = typeof(T);

        // If T is a struct, create a Table<T> for blittable types
        if (type.IsValueType)
        {
            return (ITable<T>)Activator.CreateInstance(typeof(Table<>).MakeGenericType(type),
                new object[] { rootFolder, maxElements });
        }
        // If T is a class, create a JsonTable<T> for reference types
        else
        {
            return (ITable<T>)Activator.CreateInstance(typeof(JsonTable<>).MakeGenericType(type),
                new object[] { rootFolder, maxElements });
        }
    }
}
