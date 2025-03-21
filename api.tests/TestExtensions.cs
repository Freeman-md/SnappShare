using System;

namespace api.tests;

public static class TestExtensions
{
    public static bool PropertiesAreEqual<T>(this T obj1, T obj2) {
        if (obj1 == null || obj2 == null) return false;

        var properties = typeof(T).GetProperties();
        return properties.All(p => Equals(p.GetValue(obj1), p.GetValue(obj2)));
    }
}
