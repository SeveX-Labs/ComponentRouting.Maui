using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComponentRouting.Maui.Extension
{
    public static class ReflectionExtension
    {
        public static bool IsSubclassOfRawGeneric(this object toCheck, Type generic)
        {
            var typeToCheck = toCheck.GetType();
            while (typeToCheck != null && typeToCheck != typeof(object))
            {
                var cur = typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;
                if (generic == cur)
                {
                    return true;
                }
                typeToCheck = typeToCheck.BaseType;
            }
            return false;
        }


        private static Dictionary<string, Type> TypesDict = new Dictionary<string, Type>();
        private static Dictionary<string, PropertyInfo> PropertyInfosDict = new Dictionary<string, PropertyInfo>();

        private static Type GetTypeExtended(this object obj, string objName)
        {
            Type? type = null;
            if (TypesDict.ContainsKey(objName))
            {
                type = TypesDict[objName];
            }
            else
            {
                type = obj.GetType();
                TypesDict.Add(objName, type);
            }
            return type;
        }

        public static PropertyInfo? GetPropertyInfo(this object obj, string propertyName, string? objName = null)
        {
            PropertyInfo? propertyInfo = null;

            try
            {
                string? typeKey = objName is null ? obj.ToString() : objName;

                if (typeKey is not null)
                {
                    var extentedType = obj.GetTypeExtended(typeKey);

                    string propertyInfoKey = objName is null ? $"{extentedType.FullName}_{propertyName}" : $"{objName}_{extentedType.FullName}_{propertyName}";
                    if (PropertyInfosDict.ContainsKey(propertyInfoKey))
                    {
                        propertyInfo = PropertyInfosDict[propertyInfoKey];
                    }
                    else
                    {
                        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                        propertyInfo = extentedType.GetProperty(propertyName, bindingFlags);
                        if (propertyInfo is not null)
                        {
                            PropertyInfosDict.Add(propertyInfoKey, propertyInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return propertyInfo;
        }
    }
}
