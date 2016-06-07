using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Win32;

namespace Cerevo.UB300_Win.Media {
    internal class MediaEventGeneratorImpl : IMFMediaEventGenerator, IDisposable {
        private static readonly Variant VariantNull = new Variant() { Value = null };
        private readonly MediaEventQueue _eventQueue;
        private readonly MethodInfo _eventQueueBeginGetEvent;
        private bool _shutdowned = false;
        private bool _disposed = false;

        public MediaEventGeneratorImpl() {
            MediaFactory.CreateEventQueue(out _eventQueue);
            _eventQueueBeginGetEvent = _eventQueue.GetType().GetTypeInfo().GetMethod("BeginGetEvent_", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        ~MediaEventGeneratorImpl() {
            Dispose(false);
        }

        #region IDisposable members
        public void Dispose() {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
        #endregion

        protected virtual void Dispose(bool disposing) {
            if(_disposed) return;

            if(disposing) {
                // Dispose managed resources.
                Shutdown();
                _eventQueue.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        #region IMFMediaEventGenerator members
        public void GetEvent(uint dwFlags, /*IMFMediaEvent*/out IntPtr ppEvent) {
            MediaEvent ev = null;
            try {
                _eventQueue.GetEvent((int)dwFlags, out ev);
                ppEvent = ev.Detach();
            } finally {
                ev?.Dispose();
            }
        }

        public void BeginGetEvent(/*IMFAsyncCallback*/IntPtr pCallback, /*IUnknown*/IntPtr punkState) {
            if(punkState == IntPtr.Zero) {
                _eventQueueBeginGetEvent.Invoke(_eventQueue, new object[] { pCallback, null });
                return;
            }
            var co = new ComObject(punkState);
            try {
                _eventQueueBeginGetEvent.Invoke(_eventQueue, new object[] { pCallback, co });
            } finally {
                co.NativePointer = IntPtr.Zero;
                co.Dispose();
            }
        }

        public void EndGetEvent(/*IMFAsyncResult*/IntPtr pResult, /*IMFMediaEvent*/out IntPtr ppEvent) {
            if(pResult == IntPtr.Zero) {
                throw new ArgumentNullException();
            }

            var ar = new AsyncResult(pResult);
            MediaEvent ev = null;
            try {
                _eventQueue.EndGetEvent(ar, out ev);
                ppEvent = ev.Detach();
            } finally {
                ev?.Dispose();
                ar.NativePointer = IntPtr.Zero;
                ar.Dispose();
            }
        }

        public void QueueEvent(MediaEventTypes met, ref Guid guidExtendedType, int hrStatus, /*ref Variant*/IntPtr pvValue) {
            _eventQueue.QueueEventParamVar((int)met, guidExtendedType, hrStatus,
                pvValue == IntPtr.Zero ? VariantNull : Marshal.PtrToStructure<Variant>(pvValue));
        }
        #endregion

        public void QueueEventParamVar(MediaEventTypes met, Guid guidExtendedType, Result hrStatus, Variant vValueRef) {
            _eventQueue.QueueEventParamVar((int)met, guidExtendedType, hrStatus, vValueRef);
        }

        public void QueueEventParamUnk(MediaEventTypes met, Guid guidExtendedType, Result hrStatus, ComObject unkRef) {
            _eventQueue.QueueEventParamUnk((int)met, guidExtendedType, hrStatus, unkRef);
        }

        public void QueueEventParamNone(MediaEventTypes met) {
            _eventQueue.QueueEventParamVar((int)met, Guid.Empty, Result.Ok, VariantNull);
        }

        public void QueueEventParamErr(Result hrStatus) {
            _eventQueue.QueueEventParamVar((int)MediaEventTypes.Error, Guid.Empty, hrStatus, VariantNull);
        }

        public void Shutdown() {
            if(_shutdowned) return;
            _eventQueue.Shutdown();
            _shutdowned = true;
        }
    }
}
