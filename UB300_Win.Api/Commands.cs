using System.Runtime.InteropServices;

namespace Cerevo.UB300_Win.Api {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial class SwApiCommand {
        public SwApiId Cmd;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiGetInternalState : SwApiCommand {
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiInternalState : SwApiCommand {
        public uint State;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiFindSwAck : SwApiCommand {
        public short Command;      // UDP Port (for Control)
        public short Tcp;          // TCP Port (for General)
        public short Preview;      // RTSP Port (for Preview)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Paddings;
        public uint IsWiFiAp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.DisplayNameLength)]
        public byte[] DisplayName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSwBasicInfo : SwApiCommand {
        public uint RevisionNo;    // UB300 Firmware Version
        public AvailableFirmwareUpdateType Update;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] Mac;  // new PhysicalAddress(Mac.Reverse().ToArray())
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiProgramOutSetting : SwApiCommand {
        public uint Format;
        public AudioType Audio;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiPreviewOutSetting : SwApiCommand {
        public uint Format;
        public AudioType Audio;
        public uint IsOsdEnable;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiCameraSetting : SwApiCommand {
        public uint QualityType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiDoFirmwareUpdate : SwApiCommand {
        public uint UpdateType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiDoFirmwareUpdateResult : SwApiCommand {
        public uint UpdateResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiGetFileCount : SwApiCommand {
        public uint SearchType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiFileCount : SwApiCommand {
        public int Count;
        public uint SearchType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiGetFileList : SwApiCommand {
        public int Offset;
        public int Limit;
        public uint SearchType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiFileList : SwApiCommand {
        public int Offset;
        public uint Type;
        public uint FileSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiGetFile : SwApiCommand {
        public uint Offset;
        public uint Size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiGetFileError : SwApiCommand {
        public uint Result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiFile : SwApiCommand {
        public uint Offset;
        public uint ContentLength;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiUploadFile : SwApiCommand {
        public uint ContentLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] FileName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiUploadFileResult : SwApiCommand {
        public uint UploadResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeClientNetworkSetting : SwApiCommand {
        public uint Auth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiEssidLength)]
        public byte[] Ssid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiPassphraseLength)]
        public byte[] Psk;
        public uint Address;
        public uint Gateway;
        public uint Netmask;
        public uint Dns;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiWifiClientSetting : SwApiCommand {
        public byte Num;
        public byte Auth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiEssidLength)]
        public byte[] Ssid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiPassphraseLength)]
        public byte[] Psk;
        public uint Address;
        public uint Gateway;
        public uint Netmask;
        public uint Dns;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeEthernetSetting : SwApiCommand {
        public uint IPAddress;
        public uint DefaultGateway;
        public uint SubnetMask;
        public uint DnsServer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeApNetworkSetting : SwApiCommand {
        public uint NetworkSettingEnc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiEssidLength)]
        public byte[] Essid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.WifiPassphraseLength)]
        public byte[] Passphrase;
        public uint Mode;
        public uint Channel;
        public uint PrivateIpClass;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiNetworkSettingResult : SwApiCommand {
        public uint NetworkSettingResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSelectInputForPreview : SwApiCommand {
        public uint PreviewInput;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeLiveBroadcastState : SwApiCommand {
        public uint IsRequestOnline;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiLiveBroadcastState : SwApiCommand {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.DeviceIdLength)]
        public byte[] DeviceId;
        public uint IsStateOnline;
        public uint LiveBroadcastResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeRecordingState : SwApiCommand {
        public uint RecordingState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiRecordingState : SwApiCommand {
        public uint IsStateRecording;
        public uint RecordingResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeExternalStorageInputState : SwApiCommand {
        public uint PlayTiming;
        public uint IsRepeat;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InternalConfiguration.SwPathMax)]
        public byte[] Path;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiExternalStorageInputStateResult : SwApiCommand {
        public uint InputStateResult;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeChromaPreview : SwApiCommand {
        public uint Key;
        public uint ChromaMode;
        public uint YuVupper;
        public uint YuVlower;
        public XyPointsType Points;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChromaPreviewResult : SwApiCommand {
        public uint YuVupper;
        public uint YuVlower;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangePinPPreview : SwApiCommand {
        public uint Key;
        public uint FrameRgb;
        public int FrameThickness;
        public XyPointsType Points;
        public RectType Size;
        public XyPointsType ClippingPoints;
        public RectType ClippingSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSetDefaultBackgroundColor : SwApiCommand {
        public uint Rgb;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeWiFiModeState : SwApiCommand {
        public uint EnableApMode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiWiFiModeStateResult : SwApiCommand {
        public uint ModeResult;
    }

    /* SW_ID_DoManualSwitching */
    /* SW_ID_DoAutoSwitching */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoTransition : SwApiCommand {
        public int CmdId;
        public uint Param;
        public VideoTransition Trans;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoSetPinpGeometry : SwApiCommand {
        public SwRectPair Geometry;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoSetBorder : SwApiCommand {
        public uint Color;
        public uint Width;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoSetChromaRange : SwApiCommand {
        public uint Floor;
        public uint Ceil;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoSetSubMode : SwApiCommand {
        public uint Mode;  // pinp, chroma, pinp_chroma
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiVideoSwitcherStatus : SwApiCommand {
        public VideoSwitcherStatus Status;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiFadeToDefaultColor : SwApiCommand {
        public uint AutoTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeAudioMixer : SwApiCommand {
        public byte Channel;
        public byte Category;
        public ushort Value;    /* Q15 */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeAudioMixerAll : SwApiCommand {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)AudioMixerChannel.Num)]
        public AudioVolumeType[] Pairs;
        public ushort Mute;     /* bitmap */
        public ushort Delay;    /* LINE delay */
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiChangeAudioStateMode : SwApiCommand {
        public uint EnableDetail;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStatusAudioPeak : SwApiCommand {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public ushort[] Peak;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSetVideoInput4 : SwApiCommand {
        public ushort Xres;
        public ushort Yres;
        public ushort Left;
        public ushort Upper;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateMode : SwApiCommand {
        public uint Mode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateRecording : SwApiCommand {
        public uint RecordedTime;
        public uint RecordRemainTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateFadeToDefaultColor : SwApiCommand {
        public uint IsFade;
        public uint AutoRemainTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateProgramOut : SwApiCommand {
        public uint IsConnected;
        public DisplayType Display;
        public AudioType Audio;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStatePreviewOut : SwApiCommand {
        public uint IsConnected;
        public DisplayType Display;
        public AudioType Audio;
        public uint IsOsdEnable;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateDefaultBackgroundColor : SwApiCommand {
        public uint BackgroundColorRgb;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateExternalInput : SwApiCommand {
        public uint InputType;
        public uint PlayTiming;
        public uint IsRepeat;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateExternalStorage : SwApiCommand {
        public uint IsConnected;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStatePreviewMode : SwApiCommand {
        public uint Mode;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateTcpConnected : SwApiCommand {
        public uint IsConnected;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiStateCameraResolution : SwApiCommand {
        public XyPointsType Margin;
        public RectType Size;
        public AspectType Aspect;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiPreviewOutFormat : SwApiCommand {
        public uint Format;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiPreviewOutOsd : SwApiCommand {
        public uint Enable;
    }

    /* SW_ID_SetTimezone */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSetTimezone : SwApiCommand {
        public int MinutesWest;
    }

    /* SW_ID_SetTime */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSetTime : SwApiCommand {
        public int Time;
    }

    /* SW_ID_SetTimeAndZone, SW_STATUS_ID_TimeAndZone */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiTimeAndZone : SwApiCommand {
        public uint Time;
        public int MinutesWest;
    }

    /* SW_ID_SetTimeResult */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiSetTimeResult : SwApiCommand {
        public uint Result;
    }

    /* SW_ID_MountNotify */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiMountNotify : SwApiCommand {
        public uint State;
    }

    /* SW_STATUS_ID_NetworkAddress */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiNetworkAddress : SwApiCommand {
        public uint EtherAddress;
        public uint EtherNetmask;
        public uint WlanAddress;
        public uint WlanNetmask;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiRemoveWifiClientSetting : SwApiCommand {
        public uint Num;
    }

    /* SW_ID_CasterMessage */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiCasterMessage : SwApiCommand {
        public byte Category;
        public byte Message;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Stuff;
    }

    /* SW_ID_CasterStatistics */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SwApiCasterStatistics : SwApiCommand {
        public uint Bitrate;
        public ushort Queue;
        public byte Fps;
        public byte Stuff;
    }
}
