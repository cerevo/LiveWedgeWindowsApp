using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Cerevo.UB300_Win.Api {
    internal static class InternalConfiguration {
        public const string DiscoveryIp = "224.0.0.250";
        public const int DiscoveryPort = 8888;
        public const int NetworkTimeoutMsec = 1000;
        public const int SwPathMax = 1024;
        public const int DeviceIdLength = 23;
        public const int WifiEssidLength = 33;
        public const int WifiPassphraseLength = 65;
        public const int DisplayNameLength = WifiEssidLength;
        public const string PreviewUriPath = "/live";
        public const int SessionIdMax = 65536;
        public const int CmdIdMax = 65536;
        public const int Value1 = 65536;
    }

    public static class Configuration {
        public static readonly int VideoFrameRate = 30;
        public static readonly int RegisterClientInterval = 57000;
    }

    public enum SwApiId : uint {
        // SwApi_ID
        Null = 0,
        FindSw,
        FindSwAck,
        SwBasicInfo,
        ChangeProgramOutSetting,
        ChangePreviewOutSetting,
        ChangePreviewSetting,
        ChangeRecordSetting,
        GetFileList,
        FileList,
        GetFile,
        File,
        UploadFile,
        ChangeWiFiNetworkSetting,
        WiFiNetworkSettingResult,
        ClearLiveBroadcastSetting,
        SelectInputForPreview,
        DoFirmwareUpdate,
        ChangeLiveBroadcastState,
        LiveBroadcastStateResult,
        ChangeRecordingState,
        ChangeExternalStorageInputState,
        ChangeChromaPreview,
        ChangePinPPreview,
        ChangeOsdState,
        SetDefaultBackgroundColor,
        ChangeWiFiModeState,
        DoManualSwitching,
        DoAutoSwitching,
        FadeToDefaultColor,
        ChangeAudioMixerSetting,
        ChangeAudioMixer,
        ChangeAudioMixerAll,
        RegisterClient,
        ChangeAudioStateMode,
        TcpHeartBeat,
        ChromaPreviewResult,
        RecordingResult,
        GetInternalState,
        InternalState,
        FirmwareUpdateResult,
        ChangeApNetworkSetting,
        UploadFileResult,
        ChangeEthernetSetting,
        EthernetSettingResult,
        ClearExternalStorageInputState,
        ExternalStorageInputStateResult,
        CancelChromaPreview,
        CancelPinPPreview,
        WiFiModeStateResult,
        GetFileCount,
        FileCount,
        RecordSetting,
        PreviewSetting,
        GetRecordSetting,
        GetPreviewSetting,
        ProgramOutSetting,
        PreviewOutSetting,
        GetProgramOutSetting,
        GetPreviewOutSetting,
        SetVideoInput4,
        SetPinpGeometry,
        SetPinpBorder,
        SetChromaRange,
        SetSubMode,
        ApSettingResult,
        GetApSetting,
        GetNetworkStatus,
        GetCameraResolution,
        Initialize,
        ChangePreviewOutFormat,
        ChangePreviewOutOsd,
        SetTimezone,
        SetTime,
        SetTimeAndZone,
        GetTimeAndZone,
        SetTimeResult,
        GetEthernetSetting,
        GetFileError,
        MountNotify,
        GetMountNotify,
        GetNetworkAddress,
        SetWifiClientSetting,
        GetWifiClientSetting,
        RemoveWifiClientSetting,
        CasterMessage,
        CasterStatistics,
        RegisterClient3,

        // SwApiState_ID
        StateNull = 99,
        StateMode,
        StateAudioMaster,
        StateAudioDetail,
        StateRecording,
        StateFadeToDefaultColor,
        StateExternalInput,
        StateProgramOut,
        StatePreviewOut,
        StateDefaultBackgroundColor,
        StateExternalStorage,
        StatePreviewMode,
        StateTcpConnected,
        StatusAudioMixer,
        StatusAudioMixerAll,
        StatusAudioPeak,
        StatusVideoSwitcher,
        StatusVideoSwitcherAuto,
        StatusSetPinpGeometry,
        StatusSetPinpBorder,
        StatusSetChromaRange,
        StatusSetSubMode,
        StatusApSetting,
        StatusCameraResolution,
        StatusInitialize,
        StatusTimeAndZone,
        StatusEthernetSetting,
        StatusNetworkAddress,
        StatusWifiClientSetting,
        StatusUpdater,

        Debug1 = 500
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AspectType : uint {
        Unknown = 0,
        Aspect16_9,
        Aspect4_3
    }

    public enum FrameRateType : uint {
        FrameRate60 = 0,
        FrameRate5994,
        FrameRate50,
        FrameRate30,
        FrameRate2997,
        FrameRate25,
        FrameRate24,
        FrameRate23976,
        Unknown
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DisplayFormat : uint {
        Auto = 0,
        Format1080P_24,
        Format1080P_23976,
        Format1080P_30,
        Format1080P_2997,
        Format1080P_60,
        Format1080P_5994,
        Format1080I_30,
        Format1080I_2997,
        Format1080I_60,
        Format1080I_5994,
        Format720P_24,
        Format720P_23976,
        Format720P_30,
        Format720P_2997,
        Format720P_60,
        Format720P_5994,
        Format480P_60,
        Format480P_5994,
        Format480I_60,
        Format480I_5994,
        Format480I_30,
        Format480I_2997,
        Format1080P_25,
        Format1080P_50,
        Format720P_25,
        Format720P_50,
        Format576P_25,
        Format576P_50,
        Format576I_25,
        Format576I_50,
    }

    public enum RtspPreviewQualityType : uint {
        High = 0,
        Middle,
        Low,
        Num,
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RecordingQualityType : uint {
        Quality1080_30P_High = 0,
        Quality1080_30P_Middle,
        Quality1080_30P_Low,
        Quality1080_24P_High,
        Quality1080_24P_Middle,
        Quality1080_24P_Low,
        Quality1080_20P_High,
        Quality1080_20P_Middle,
        Quality1080_20P_Low,
        Quality720_30P_High,
        Quality720_30P_Middle,
        Quality720_30P_Low,
        Quality720_24P_High,
        Quality720_24P_Middle,
        Quality720_24P_Low,
        Quality720_20P_High,
        Quality720_20P_Middle,
        Quality720_20P_Low,
        Quality480_30P_High,
        Quality480_30P_Middle,
        Quality480_30P_Low,
        Quality480_24P_High,
        Quality480_24P_Middle,
        Quality480_24P_Low,
        Quality480_20P_High,
        Quality480_20P_Middle,
        Quality480_20P_Low,
        Quality1080_25P_High,
        Quality1080_25P_Middle,
        Quality1080_25P_Low,
        Quality720_25P_High,
        Quality720_25P_Middle,
        Quality720_25P_Low,
        Quality480_25P_High,
        Quality480_25P_Middle,
        Quality480_25P_Low,
        Num,
    }

    public enum FileType : uint {
        All = 0,
        Movie,
        Sound,
        Graphic,
        Folder,
    }

    public enum ApiResult : uint {
        Success,
        InvalidData,

        DnsError,
        CantConnectNetwork,
        UnsupportedWebAuth,
        SocketTimeout,
        ServerError,
        IllegalDisconnectLan,

        NoSd,
        SdNoCapacity,
        SdNotWritable,
        IllegalSdUnmount,

        NoData,
        DhcpError,
        DhcpErrorApFailed,
        ApFailed,
        OnInternet,
        Unplugged,
        Down,

        CantUploadFile,
        CantPlayFile,
        NotFoundFile,

        UnknownError,

        Busy,
        Continue,

        NoDevice,
    }

    public enum InternalStateType : uint {
        Ethernet = 0,
        Wifi,
        Firmware
    }

    public enum InternalState : uint {
        NetworkNo = 0,
        NetworkOff,
        NetworkConnecting,
        NetworkConnected,
        NetworkFailed,
        ApStarting,
        ApStarted,
        ApFailed,
        FirmwareNotFound,
        FirmwareReading,
        FirmwareDecompressing,
        FirmwareUpdating,
        FirmwareFailed,
        FirmwareSucceed,
        LiveNo,
        LiveConnectingDashboard,
        LiveOnline,
        LiveGettingData,
        LiveCantConnectDashboard,
        LiveStarting,
        LiveSuccess,
        LiveFailed
    }

    public enum AvailableFirmwareUpdateType : uint {
        Null = 0,
        Sd,
        Dashboard,
        Both
    }

    public enum FirmwareUpdateType : uint {
        Sd = 0,
        Dashboard,
        Abort,
    }

    public enum NetworkSettingEncType : uint {
        Wpa,
        Wep,
        Auto = Wpa,
    }

    public enum PrivateIpClassType : uint {
        ClassA = 0, // 10.0.0.0/8
        ClassB,     // 172.16.0.0/8
        ClassC      // 192.168.0.0/8
    }

    public enum WiFiMode : uint {
        ModeA = 0,
        ModeB,
        ModeG
    }

    public enum PreviewInputType : uint {
        Type1 = 0,
        Type2,
        Type3,
        Type4,
        TypeTile,
        TypeProgramOut,
        TypeSd = Type4
    }

    public enum PlayTimingType : uint {
        Null = 0,
        Now,
        WhenProgramOut,
    }

    public enum KeySettingType : uint {
        Setting1 = 1,
        Setting2,
        Setting3,
        Setting4,
    }

    public enum TransitionType : uint {
        Null = 0,
        Mix,
        Dip,
        WipeHorizontal,
        WipeHorizontalR, // _R means reversed pattern
        WipeVertical,
        WipeVerticalR,
        WipeHorizontalSlide,
        WipeHorizontalSlideR,
        WipeVerticalSlide,
        WipeVerticalSlideR,
        WipeHorizontalDoubleSlide,
        WipeHorizontalDoubleSlideR,
        WipeVerticalDoubleSlide,
        WipeVerticalDoubleSlideR,
        WipeSquareTopLeft, /* top to bottom and left to right order */
        WipeSquareTopLeftR,
        WipeSquareTop,
        WipeSquareTopR,
        WipeSquareTopRight,
        WipeSquareTopRightR,
        WipeSquareCenterLeft,
        WipeSquareCenterLeftR,
        WipeSquareCenter,
        WipeSquareCenterR,
        WipeSquareCenterRight,
        WipeSquareCenterRightR,
        WipeSquareBottomLeft,
        WipeSquareBottomLeftR,
        WipeSquareBottom,
        WipeSquareBottomR,
        WipeSquareBottomRight,
        WipeSquareBottomRightR,
        Num,
        None = Null,
        WipeCenterSquare = WipeSquareCenter,
        WipeCenterSquareR = WipeSquareCenterR,
        WipeLeftSideSquare = WipeSquareCenterLeft,
        WipeLeftSideSquareR = WipeSquareCenterLeftR,
        WipeRightSideSquare = WipeSquareCenterRight,
        WipeRightSideSquareR = WipeSquareCenterRightR,
        WipeLeftTopSquare = WipeSquareTopLeft,
        WipeLeftTopSquareR = WipeSquareTopLeftR,
        WipeRightTopSquare = WipeSquareTopRight,
        WipeRightTopSquareR = WipeSquareTopRightR,
        WipeTopSquare = WipeSquareTop,
        WipeTopSquareR = WipeSquareTopR,
        WipeLeftBottomSquare = WipeSquareBottomLeft,
        WipeLeftBottomSquareR = WipeSquareBottomLeftR,
        WipeRightBottomSquare = WipeSquareBottomRight,
        WipeRightBottomSquareR = WipeSquareBottomRightR,
        WipeBottomSquare = WipeSquareBottom,
        WipeBottomSquareR = WipeSquareBottomR,
        Cut = Null,
    }

    public enum OverlaySettingType : uint {
        Null = 0,
        Chroma,
        Opaque,
    }

    public enum VcSubMode : uint {
        Chromakey,
        Pinp,
        ChromakeyPinp,
        PinpChromakey = ChromakeyPinp,
    }

    public enum VcMode : byte {
        Main,
        Sub,
        Us,     /* sub in upstream */
        Dual,
    }

    public enum AudioMixerChannel : uint {
        Master,
        Input1,
        Input2,
        Input3,
        Input4,
        Cpu,
        Line,
        Num,
    }

    public enum AudioMixerCategory : uint {
        Gain, /* Master has no gain */
        Volume,
        Mute,
        Delay, /* for only LINE */
    }

    public enum HdmiInputType : uint {
        InputNull = 0,
        Input1,
        Input2,
        Input3,
        Input4,
    }

    public enum SwMode : uint {
        Rtsp = 0,
        Live,
        Recording
    }

    public enum ExternalStorageInputType : uint {
        Hdmi = 0,
        Movie,
        Sound,
        Graphic
    }

    public enum SwMount : uint {
        Unmounted,
        Readonly,
        Readwrite,
        ReqReadonly,
        ReqReadwrite = Readwrite,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RectType {
        public int Height;
        public int Width;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XyPointsType {
        public int XPoint;
        public int YPoint;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DisplayType {
        public uint IsAuto;
        public RectType Pixel;
        public uint Aspect;
        public uint FrameRate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AudioType {
        public uint IsEnable;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AudioVolumeType {
        public ushort Gain;
        public ushort Volume;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SwRect {
        public int X;
        public int Y;
        public uint W;
        public uint H;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SwRectPair {
        public SwRect Scale;
        public SwRect Crop;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SingleTransition {
        public byte Src;
        public byte Effect;
        public byte DipSrc;
        public byte Padding;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoTransition {
        public byte Mode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Paddings;
        public SingleTransition Main;
        public SingleTransition Sub;
    };

    // SW_STATUS_ID_VideoSwitcher
    // SW_STATUS_ID_VideoSwitcherAuto
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoSwitcherStatus {
        public uint Param;
        public uint CmdId;
        public byte MainSrc;      /* main video source */
        public byte SubSrc;       /* sub (PinP/DSK) video source */
        public byte SubMode;      /* sub video mode */
        //public byte Padding;
        public VideoTransition Trans;
        public uint ChromaFloor;  /* DSK keying parameter */
        public uint ChromaCeil;   /* DSK keying parameter */
        public SwRectPair Pinp;   /* sub video PinP geometry */
        public uint PinpBorderColor;
        public byte PinpBorderWidth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Paddings;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoCompositionLayer {
        public int InputNum;
        public int AlphaValue;
        public SwRect Scale;
        public SwRect Crop;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoComposition {
        public uint Size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public VideoCompositionLayer[] Node;
        public uint OverlayType;
        public uint ChromaYuVupper;
        public uint ChromaYuVlower;
        public int PinPFrameThickness;
        public uint PinPFrameRgb;
        public SwRect PinPFrame;
    }
}
