using System;
using System.Linq;
using System.Reflection;

namespace api.tests;

public static class TestExtensions
{
    public static bool PropertiesAreEqual<T>(this T obj1, T obj2, params string[] excludedProperties)
    {
        if (obj1 == null || obj2 == null) return false;

        PropertyInfo[] properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (excludedProperties.Contains(prop.Name))
                continue;

            var value1 = prop.GetValue(obj1);
            var value2 = prop.GetValue(obj2);

            if (!Equals(value1, value2))
                return false;
        }

        return true;
    }
}
