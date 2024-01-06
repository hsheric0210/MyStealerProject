using System;

namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class Platform
    {
        public static string GetTypeName(object obj) => GetTypeName(obj.GetType());

        public static string GetTypeName(Type t) => t.FullName;
    }
}
