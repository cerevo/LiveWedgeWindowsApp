using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cerevo.UB300_Win.Api {
    public partial class SwApiCommand {
        internal byte[] ToBytes() {
            var len = Marshal.SizeOf(this.GetType());
            var buffer = new byte[len];

            var ptr = Marshal.AllocHGlobal(len);
            try {
                Marshal.StructureToPtr(this, ptr, true);
                Marshal.Copy(ptr, buffer, 0, len);
            } finally {
                Marshal.FreeHGlobal(ptr);
            }
            return buffer;

        }

        internal static T FromBytes<T>(byte[] buffer) where T : SwApiCommand {
            return FromBytes<T>(buffer, 0);
        }

        internal static T FromBytes<T>(byte[] buffer, int offset) where T : SwApiCommand {
            if(!CheckSize<T>(buffer, offset)) {
                return null;
            }

            var len = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(len);
            try {
                Marshal.Copy(buffer, offset, ptr, len);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            } finally {
                Marshal.FreeHGlobal(ptr);
            }
        }

        internal static bool CheckSize<T>(byte[] buffer) where T : SwApiCommand {
            return CheckSize<T>(buffer, 0);
        }

        internal static bool CheckSize<T>(byte[] buffer, int offset) where T : SwApiCommand {
            Debug.Assert(buffer.Length >= (Marshal.SizeOf(typeof(T)) + offset));
            return buffer.Length >= (Marshal.SizeOf(typeof(T)) + offset);
        }

        internal static SwApiId GetApiId(byte[] buffer) {
            if(buffer.Length < 4) return SwApiId.Null;
            return (SwApiId)BitConverter.ToUInt32(buffer, 0);
        }
    }
}
