using System;
using System.Runtime.InteropServices;
using SharpDX;

namespace Cerevo.UB300_Win.Media {
    internal static class ComObjectUtils {
        public static IntPtr Detach(this ComObject comObject) {
            var pointer = comObject.NativePointer;
            comObject.NativePointer = IntPtr.Zero;
            return pointer;
        }

        public static ComObject Attach(IntPtr pointer) {
            if(pointer == IntPtr.Zero) {
                return new ComObject(IntPtr.Zero);
            }
            var comObject = new ComObject(pointer);
            Marshal.AddRef(pointer);
            return comObject;
        }

        public static T AttachAs<T>(IntPtr pointer) where T : ComObject {
            var comObject = ComObject.As<T>(pointer);
            Marshal.AddRef(pointer);
            return comObject;
        }
    }
}
