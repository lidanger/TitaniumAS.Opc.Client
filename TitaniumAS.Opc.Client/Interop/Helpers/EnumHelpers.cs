using System;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using TitaniumAS.Opc.Client.Interop.Common;
using TitaniumAS.Opc.Client.Interop.Da;

namespace TitaniumAS.Opc.Client.Interop.Helpers
{
    internal static class EnumHelpers
    {
        public static List<object> EnumareateAllAndRelease(IEnumUnknown enumerator, int requestSize = 128)
        {
            try
            {
                return EnumerateAll(enumerator, requestSize);
            }
            finally
            {
                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }
        }

        public static List<string> EnumareateAllAndRelease(IEnumString enumerator, int requestSize = 128)
        {
            try
            {
                return EnumerateAll(enumerator, requestSize);
            }
            finally
            {
                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }
        }

        public static List<OPCITEMATTRIBUTES> EnumareateAllAndRelease(IEnumOPCItemAttributes enumerator,
            int requestSize = 128)
        {
            try
            {
                return EnumerateAll(enumerator, requestSize);
            }
            finally
            {
                if (enumerator != null)
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }
        }

        public static List<string> EnumerateAll(IEnumString enumerator, int requestSize = 128)
        {
            if (enumerator == null)
                return new List<string>();

            var result = new List<string>();
            var fetched = 0;
            var items = new string[requestSize];
            do
            {
                enumerator.RemoteNext(items.Length, items, out fetched);
                for (int i = 0; i < fetched; i++)
                {
                    result.Add(items[i]);
                }
            } while (fetched != 0);
            return result;
        }

        public static List<object> EnumerateAll(IEnumUnknown enumerator, int requestSize = 128)
        {
            if (enumerator == null)
                return new List<object>();

            var result = new List<object>();
            var fetched = 0;
            var items = new object[requestSize];
            do
            {
                enumerator.RemoteNext(items.Length, items, out fetched);
                for (int i = 0; i < fetched; i++)
                {
                    result.Add(items[i]);
                }
            } while (fetched != 0);
            return result;
        }

        public static List<OPCITEMATTRIBUTES> EnumerateAll(IEnumOPCItemAttributes enumerator, int requestSize = 128)
        {
            if (enumerator == null)
                return new List<OPCITEMATTRIBUTES>();

            var result = new List<OPCITEMATTRIBUTES>();
            var fetched = 0;
            do
            {
                IntPtr ptr;
                enumerator.Next(requestSize, out ptr, out fetched);
                for (var i = 0; i < fetched; i++)
                {
                    var current = new IntPtr((IntPtr.Size == sizeof(Int64) ? ptr.ToInt64() : ptr.ToInt32()) + i * Marshal.SizeOf(typeof(OPCITEMATTRIBUTES)));
                    result.Add((OPCITEMATTRIBUTES)Marshal.PtrToStructure(current, typeof(OPCITEMATTRIBUTES)));
                    Marshal.DestroyStructure(current, typeof(OPCITEMATTRIBUTES));
                }
                Marshal.FreeCoTaskMem(ptr);
            } while (fetched != 0);
            return result;
        }
    }
}