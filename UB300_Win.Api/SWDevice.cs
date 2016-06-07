using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Cerevo.UB300_Win.Api {
    public class SwDevice : IDisposable {
        private bool _disposed = false;
        private readonly Random _random;
        private readonly int _sessionId;
        private readonly SemaphoreSlim _sendControlLock;
        private readonly SemaphoreSlim _sendGeneralLock;
        private readonly BehaviorSubject<bool> _isConnected;
        private readonly Subject<SwApiCommand> _controlSubject;
        private readonly Subject<SwApiCommand> _generalSubject;
        private readonly UdpClient _controlUdp;
        private readonly TcpClient _generalTcp;
        private NetworkStream _generalTcpStream;

        public SwDevice(string name, IPAddress deviceIp, int controlPort, int generalPort, int previewPort) {
            Name = name;
            DeviceIp = deviceIp;
            ControlPort = controlPort;
            GeneralPort = generalPort;
            PreviewPort = previewPort;
            _random = new Random();
            _sessionId = _random.Next(InternalConfiguration.SessionIdMax);
            _sendControlLock = new SemaphoreSlim(1, 1);
            _sendGeneralLock = new SemaphoreSlim(1, 1);
            _isConnected = new BehaviorSubject<bool>(false);
            _controlUdp = new UdpClient();
            _generalTcp = new TcpClient {
                NoDelay = true,
                ReceiveTimeout = InternalConfiguration.NetworkTimeoutMsec,
                SendTimeout = InternalConfiguration.NetworkTimeoutMsec
            };
            _controlSubject = new Subject<SwApiCommand>();
            _generalSubject = new Subject<SwApiCommand>();
        }

        ~SwDevice() {
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
                _controlSubject.Dispose();
                _generalSubject.Dispose();
                Disconnect();
                _isConnected.Dispose();
                _sendControlLock.Dispose();
                _sendGeneralLock.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        #region private members
        private IObservable<SwApiCommand> ObserveControl() => Observable.Create<SwApiCommand>(async (observer, cancelToken) => {
            Debug.Assert(_controlUdp.Client.Connected);
            try {
                while(IsConnected) {
                    if(cancelToken.IsCancellationRequested) {
                        return;
                    }
                    try {
                        var received = await _controlUdp.ReceiveAsync().WithCancellation(cancelToken);
                        observer.OnNext(ParseControlCommand(received.Buffer));
                    } catch(OperationCanceledException) {
                        break;
                    }
                }
                observer.OnCompleted();
            } catch(Exception ex) {
                Debug.WriteLine($"ObserveControl Error. '{ex.Message}'");
                observer.OnError(ex);
            }
        });

        private IObservable<SwApiCommand> ObserveGeneral(Stream stream) => Observable.Create<SwApiCommand>(async (observer, cancelToken) => {
            Debug.Assert(stream != null);
            var sizebuf = new byte[4];
            try {
                while(IsConnected) {
                    if(cancelToken.IsCancellationRequested) {
                        return;
                    }
                    try {
                        // read payload size
                        if(await ReadStream(stream, sizebuf, 4, cancelToken) == false) {
                            break;
                        }
                        var payloadSize = BitConverter.ToUInt32(sizebuf, 0);
                        if(payloadSize < 1) {
                            break;
                        }
                        Debug.Assert(payloadSize < int.MaxValue);

                        // read payload
                        var payload = new byte[payloadSize];
                        if(await ReadStream(stream, payload, (int)payloadSize, cancelToken) == false) {
                            break;
                        }
                        observer.OnNext(ParseGeneralCommand(payload));
                    } catch(OperationCanceledException) {
                        break;
                    }
                }
                observer.OnCompleted();
            } catch(Exception ex) {
                Debug.WriteLine($"ObserveGeneral Error. '{ex.Message}'");
                observer.OnError(ex);
            }
        });

        private static async Task<bool> ReadStream(Stream stream, byte[] buf, int len, CancellationToken cancelToken) {
            Debug.Assert(buf != null && buf.Length >= len);
            var remain = len;
            while(remain > 0) {
                var idx = len - remain;
                var done = await stream.ReadAsync(buf, idx, remain, cancelToken);
                if(done < 1) {
                    return false;
                }
                remain -= done;
            }
            return true;
        }

        private static SwApiCommand ParseControlCommand(byte[] buf) {
            var apiId = SwApiCommand.GetApiId(buf);
            switch(apiId) {
                case SwApiId.StateMode:
                    // SwApi_State_Mode
                    return SwApiCommand.FromBytes<SwApiStateMode>(buf);
                case SwApiId.StateRecording:
                    // SwApi_State_Recording
                    return SwApiCommand.FromBytes<SwApiStateRecording>(buf);
                case SwApiId.StateFadeToDefaultColor:
                    // SwApi_State_FadeToDefaultColor
                    return SwApiCommand.FromBytes<SwApiStateFadeToDefaultColor>(buf);
                case SwApiId.StateExternalInput:
                    // SwApi_State_ExternalInput
                    return SwApiCommand.FromBytes<SwApiStateExternalInput>(buf);
                case SwApiId.StateProgramOut:
                    // SwApi_State_ProgramOut
                    return SwApiCommand.FromBytes<SwApiStateProgramOut>(buf);
                case SwApiId.StatePreviewOut:
                    // SwApi_State_PreviewOut
                    return SwApiCommand.FromBytes<SwApiStatePreviewOut>(buf);
                case SwApiId.StateDefaultBackgroundColor:
                    // SwApi_State_DefaultBackgroundColor
                    return SwApiCommand.FromBytes<SwApiStateDefaultBackgroundColor>(buf);
                case SwApiId.StateExternalStorage:
                    // SwApi_State_ExternalStorage
                    return SwApiCommand.FromBytes<SwApiStateExternalStorage>(buf);
                case SwApiId.StatePreviewMode:
                    // SwApi_State_PreviewMode
                    return SwApiCommand.FromBytes<SwApiStatePreviewMode>(buf);
                case SwApiId.StateTcpConnected:
                    // no payload
                    return SwApiCommand.FromBytes<SwApiCommand>(buf);
                case SwApiId.StatusAudioMixer:
                    // SwApi_ChangeAudioMixer
                    return SwApiCommand.FromBytes<SwApiChangeAudioMixer>(buf);
                case SwApiId.StatusAudioMixerAll:
                    // SwApi_ChangeAudioMixerAll
                    return SwApiCommand.FromBytes<SwApiChangeAudioMixerAll>(buf);
                case SwApiId.StatusAudioPeak:
                    // SwApi_Status_AudioPeak
                    return SwApiCommand.FromBytes<SwApiStatusAudioPeak>(buf);
                case SwApiId.StatusVideoSwitcher:
                    // SwApi_VideoSwitcherStatus
                    return SwApiCommand.FromBytes<SwApiVideoSwitcherStatus>(buf);
                case SwApiId.StatusVideoSwitcherAuto:
                    // SwApi_VideoSwitcherStatus
                    return SwApiCommand.FromBytes<SwApiVideoSwitcherStatus>(buf);
                case SwApiId.StatusSetPinpGeometry:
                    // SwApi_VideoSetPinpGeometry
                    return SwApiCommand.FromBytes<SwApiVideoSetPinpGeometry>(buf);
                case SwApiId.StatusSetPinpBorder:
                    // SwApi_VideoSetBorder
                    return SwApiCommand.FromBytes<SwApiVideoSetBorder>(buf);
                case SwApiId.StatusSetChromaRange:
                    // SwApi_VideoSetChromaRange
                    return SwApiCommand.FromBytes<SwApiVideoSetChromaRange>(buf);
                case SwApiId.StatusSetSubMode:
                    // SwApi_VideoSetSubMode
                    return SwApiCommand.FromBytes<SwApiVideoSetSubMode>(buf);
                case SwApiId.StatusNetworkAddress:
                    // SwApi_NetworkAddress
                    return SwApiCommand.FromBytes<SwApiNetworkAddress>(buf);
                case SwApiId.StatusUpdater:
                    // SwApi_DoFirmwareUpdateResult
                    return SwApiCommand.FromBytes<SwApiDoFirmwareUpdateResult>(buf);
                case SwApiId.EthernetSettingResult:
                case SwApiId.ApSettingResult:
                case SwApiId.WiFiNetworkSettingResult:
                    // SwApi_NetworkSettingResult
                    return SwApiCommand.FromBytes<SwApiNetworkSettingResult>(buf);
                case SwApiId.MountNotify:
                    // SwApi_MountNotify
                    return SwApiCommand.FromBytes<SwApiMountNotify>(buf);
                case SwApiId.LiveBroadcastStateResult:
                    // SwApi_LiveBroadcastState
                    return SwApiCommand.FromBytes<SwApiLiveBroadcastState>(buf);
                default:
                    Debug.WriteLine($"Unsupported ControlCommand({apiId})");
                    return null;
            }
        }

        private static SwApiCommand ParseGeneralCommand(byte[] buf) {
            var apiId = SwApiCommand.GetApiId(buf);
            switch(apiId) {
                case SwApiId.SwBasicInfo:
                    // SwApi_SwBasicInfo
                    return SwApiCommand.FromBytes<SwApiSwBasicInfo>(buf);
                case SwApiId.FileList:
                    // SwApi_FileList
                    return SwApiCommand.FromBytes<SwApiFileList>(buf);
                case SwApiId.ChromaPreviewResult:
                    // SwApi_ChromaPreviewResult
                    return SwApiCommand.FromBytes<SwApiChromaPreviewResult>(buf);
                case SwApiId.LiveBroadcastStateResult:
                    // SwApi_LiveBroadcastState
                    return SwApiCommand.FromBytes<SwApiLiveBroadcastState>(buf);
                case SwApiId.UploadFileResult:
                    // SwApi_UploadFileResult
                    return SwApiCommand.FromBytes<SwApiUploadFileResult>(buf);
                case SwApiId.ExternalStorageInputStateResult:
                    // SwApi_ExternalStorageInputStateResult
                    return SwApiCommand.FromBytes<SwApiExternalStorageInputStateResult>(buf);
                case SwApiId.EthernetSettingResult:
                case SwApiId.WiFiNetworkSettingResult:
                case SwApiId.ApSettingResult:
                    // SwApi_NetworkSettingResult
                    return SwApiCommand.FromBytes<SwApiNetworkSettingResult>(buf);
                case SwApiId.MountNotify:
                    // SwApi_MountNotify
                    return SwApiCommand.FromBytes<SwApiMountNotify>(buf);
                case SwApiId.RecordingResult:
                    // SwApi_RecordingState
                    return SwApiCommand.FromBytes<SwApiRecordingState>(buf);
                case SwApiId.RecordSetting:
                case SwApiId.PreviewSetting:
                    // SwApi_CameraSetting
                    return SwApiCommand.FromBytes<SwApiCameraSetting>(buf);
                case SwApiId.FirmwareUpdateResult:
                    // SwApi_DoFirmwareUpdateResult
                    return SwApiCommand.FromBytes<SwApiDoFirmwareUpdateResult>(buf);
                case SwApiId.StatusNetworkAddress:
                    // SwApi_NetworkAddress
                    return SwApiCommand.FromBytes<SwApiNetworkAddress>(buf);
                case SwApiId.File:
                case SwApiId.ChangeProgramOutSetting:
                case SwApiId.TcpHeartBeat:
                case SwApiId.SetTimeResult:
                case SwApiId.ChangePreviewOutOsd:
                case SwApiId.ChangePreviewOutFormat:
                case SwApiId.GetFileError:
                    // Do nothing
                    return null;
                default:
                    Debug.WriteLine($"Unsupported GeneralCommand({apiId})");
                    return null;
            }
        }

        private int MakeCmdId() {
            return (_sessionId << 16) + _random.Next(InternalConfiguration.CmdIdMax);
        }
        #endregion

        public bool IsConnected => _isConnected.Value;
        public IObservable<bool> IsConnectedChanged => _isConnected;
        public IObservable<SwApiCommand> ControlCommandStream => _controlSubject;
        public IObservable<SwApiCommand> GeneralCommandStream => _generalSubject;
        public string Name { get; }
        public IPAddress DeviceIp { get; }
        public int ControlPort { get; }
        public int GeneralPort { get; }
        public int PreviewPort { get; }

        public Uri GetPreviewUri(string scheme) {
            return new UriBuilder(scheme, DeviceIp.ToString(), PreviewPort, InternalConfiguration.PreviewUriPath).Uri;
        }

        public async Task Connect() {
            if(IsConnected || _controlUdp.Client.Connected || _generalTcp.Connected) {
                throw new InvalidOperationException();
            }

            _controlUdp.Connect(DeviceIp, ControlPort);
            await _generalTcp.ConnectAsync(DeviceIp, GeneralPort);
            _generalTcpStream = _generalTcp.GetStream();
            _isConnected.OnNext(true);

            ObserveControl().Where(c => c != null).Subscribe(_controlSubject.OnNext, _controlSubject.OnError);
            ObserveGeneral(_generalTcpStream).Where(c => c != null).Subscribe(_generalSubject.OnNext, _generalSubject.OnError);
        }

        public void Disconnect() {
            _isConnected.OnNext(false);
            _generalTcpStream?.Close();
            _generalTcpStream = null;
            _generalTcp.Close();
            _controlUdp.Close();
        }

        public async Task SendControlCommand(SwApiCommand cmd) {
            if(!_controlUdp.Client.Connected) {
                throw new InvalidOperationException();
            }

            var buf = cmd.ToBytes();

            await _sendControlLock.WaitAsync();
            try {
                await _controlUdp.SendAsync(buf, buf.Length);
            } finally {
                _sendControlLock.Release();
            }
        }

        public async Task SendGeneralCommand(SwApiCommand cmd) {
            if(_generalTcpStream == null) {
                throw new InvalidOperationException();
            }

            var payload = cmd.ToBytes();
            var buf = BitConverter.GetBytes((uint)payload.Length).Concat(payload).ToArray();

            await _sendGeneralLock.WaitAsync();
            try {
                await _generalTcpStream.WriteAsync(buf, 0, buf.Length);
            } finally {
                _sendGeneralLock.Release();
            }
        }

        public Task RegisterClient() => SendControlCommand(new SwApiCommand {
            Cmd = SwApiId.RegisterClient3
        });

        public Task SelectInputForPreview(PreviewInputType type) => SendGeneralCommand(new SwApiSelectInputForPreview() {
            Cmd = SwApiId.SelectInputForPreview,
            PreviewInput = (uint)type
        });

        public Task ManualSwitchingCut(int number) => SendControlCommand(new SwApiVideoTransition() {
            Cmd = SwApiId.DoManualSwitching,
            CmdId = MakeCmdId(),
            Param = InternalConfiguration.Value1,
            Trans = new VideoTransition() {
                Mode = (byte)VcMode.Main,
                Main = new SingleTransition() {
                    Src = (byte)(number + 1),
                    Effect = (byte)TransitionType.Cut
                }
            }
        });
    }
}
