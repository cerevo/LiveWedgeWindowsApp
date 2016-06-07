using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX.MediaFoundation;
using SharpDX.Win32;

namespace Cerevo.UB300_Win.Media {
    [ComImport]
    [Guid("2CD0BD52-BCD5-4B89-B62C-EADC0C031E7D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaEventGenerator {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetEvent([In] uint dwFlags, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void BeginGetEvent(/*IMFAsyncCallback*/[In] IntPtr pCallback, /*IUnknown*/[In] IntPtr punkState);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void EndGetEvent(/*IMFAsyncResult*/[In] IntPtr pResult, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void QueueEvent([MarshalAs(UnmanagedType.U4)][In] MediaEventTypes met, [In] ref Guid guidExtendedType, [MarshalAs(UnmanagedType.Error)][In] int hrStatus, /*ref Variant*/[In] IntPtr pvValue);
    }

    [ComImport]
    [Guid("279A808D-AEC7-40C8-9C6B-A6B492C78A66")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaSource : IMFMediaEventGenerator {
        [MethodImpl(MethodImplOptions.InternalCall)]
        new void GetEvent([In] uint dwFlags, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void BeginGetEvent(/*IMFAsyncCallback*/[In] IntPtr pCallback, /*IUnknown*/[In] IntPtr punkState);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void EndGetEvent(/*IMFAsyncResult*/[In] IntPtr pResult, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void QueueEvent([MarshalAs(UnmanagedType.U4)][In] MediaEventTypes met, [In] ref Guid guidExtendedType, [MarshalAs(UnmanagedType.Error)][In] int hrStatus, /*ref Variant*/[In] IntPtr pvValue);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetCharacteristics([MarshalAs(UnmanagedType.U4)] out MediaSourceCharacteristics pdwCharacteristics);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void CreatePresentationDescriptor(/*IMFPresentationDescriptor*/ out IntPtr ppPresentationDescriptor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Start(/*IMFPresentationDescriptor*/[In] IntPtr pPresentationDescriptor, [In] ref Guid pguidTimeFormat, [In] ref Variant pvarStartPosition);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Stop();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Pause();

        [MethodImpl(MethodImplOptions.InternalCall)]
        void Shutdown();
    }

    [ComImport]
    [Guid("D182108F-4EC6-443F-AA42-A71106EC825F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaStream : IMFMediaEventGenerator {
        [MethodImpl(MethodImplOptions.InternalCall)]
        new void GetEvent([In] uint dwFlags, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void BeginGetEvent(/*IMFAsyncCallback*/[In] IntPtr pCallback, /*IUnknown*/[In] IntPtr punkState);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void EndGetEvent(/*IMFAsyncResult*/[In] IntPtr pResult, /*IMFMediaEvent*/out IntPtr ppEvent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        new void QueueEvent([MarshalAs(UnmanagedType.U4)][In] MediaEventTypes met, [In] ref Guid guidExtendedType, [MarshalAs(UnmanagedType.Error)][In] int hrStatus, /*ref Variant*/[In] IntPtr pvValue);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetMediaSource([MarshalAs(UnmanagedType.Interface)] out IMFMediaSource ppMediaSource);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetStreamDescriptor(/*IMFStreamDescriptor*/ out IntPtr ppStreamDescriptor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        void RequestSample(/*IUnknown*/[In] IntPtr pToken);
    }
}
