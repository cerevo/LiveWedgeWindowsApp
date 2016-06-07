using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Cerevo.UB300_Win.Api;
using Cerevo.UB300_Win.Media;
using Reactive.Bindings.Extensions;

namespace Cerevo.UB300_Win.Models {
    public class DeviceModel : IDisposable {
        private bool _disposed = false;
        private readonly CompositeDisposable _disposables;
        private readonly CompositeDisposable _connectionDisposables;

        private readonly SwDevice _device;
        public bool IsConnected { get; private set; }
        public Uri PreviewUri { get; }

        // SwApiSwBasicInfo
        public uint RevisionNo { get; private set; }
        public AvailableFirmwareUpdateType FirmwareUpdateType { get; private set; }
        public bool HasSdFirmwareUpdate => (FirmwareUpdateType == AvailableFirmwareUpdateType.Sd || FirmwareUpdateType == AvailableFirmwareUpdateType.Both);
        public bool HasDashboardFirmwareUpdate => (FirmwareUpdateType == AvailableFirmwareUpdateType.Dashboard || FirmwareUpdateType == AvailableFirmwareUpdateType.Both);
        public PhysicalAddress MacAddress { get; private set; }

        // SwApiStateMode
        private readonly BehaviorSubject<SwMode> _mode;
        public SwMode Mode => _mode.Value;
        public IObservable<SwMode> ModeChanged => _mode;
        public bool IsModeRtsp => (Mode == SwMode.Rtsp);
        public bool IsModeLive => (Mode == SwMode.Live);
        public bool IsModeRecording => (Mode == SwMode.Recording);

        // SwApiStatePreviewMode
        private readonly BehaviorSubject<PreviewInputType> _previewMode;
        public PreviewInputType PreviewMode => _previewMode.Value;
        public IObservable<PreviewInputType> PreviewModeChanged => _previewMode;
        public bool IsPreviewOutput => (PreviewMode == PreviewInputType.TypeProgramOut);
        public bool IsPreviewInput => !IsPreviewOutput;

        // SwApiVideoSwitcherStatus
        private readonly BehaviorSubject<VideoSwitcherStatus> _videoSwitcherStatus;
        public VideoSwitcherStatus VideoSwitcherStatus => _videoSwitcherStatus.Value;
        public IObservable<VideoSwitcherStatus> VideoSwitcherStatusChanged => _videoSwitcherStatus;

        public DeviceModel() {
            _disposables = new CompositeDisposable();
            _connectionDisposables = new CompositeDisposable();

            _device = null;
            IsConnected = false;
            PreviewUri = null;

            // SwApiSwBasicInfo
            RevisionNo = 0;
            FirmwareUpdateType = AvailableFirmwareUpdateType.Null;
            MacAddress = PhysicalAddress.None;

            // SwApiStateMode
            _mode = new BehaviorSubject<SwMode>(SwMode.Rtsp).AddTo(_disposables);

            // SwApiStatePreviewMode
            _previewMode = new BehaviorSubject<PreviewInputType>(PreviewInputType.TypeProgramOut).AddTo(_disposables);

            // SwApiVideoSwitcherStatus
            _videoSwitcherStatus = new BehaviorSubject<VideoSwitcherStatus>(default(VideoSwitcherStatus)).AddTo(_disposables);
        }

        public DeviceModel(DiscoverResult result) : this() {
            _device = new SwDevice(result.DisplayNameString, result.Address, result.FindSwAck.Command, result.FindSwAck.Tcp, result.FindSwAck.Preview).AddTo(_disposables);
            PreviewUri = _device.GetPreviewUri(RtspHandler.UriSchemeRtsp);
        }

        ~DeviceModel() {
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
                _connectionDisposables.Dispose();
                _disposables.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        public async Task Connect() {
            if(_device == null) {
                return;
            }

            _connectionDisposables.Clear();

            _device.IsConnectedChanged.Subscribe(b => IsConnected = b).AddTo(_connectionDisposables);
            await _device.Connect();

            _device.ControlCommandStream.OfType<SwApiStateMode>().Subscribe(OnStateMode).AddTo(_connectionDisposables);
            _device.ControlCommandStream.OfType<SwApiStatePreviewMode>().Subscribe(OnStatePreviewMode).AddTo(_connectionDisposables);
            _device.ControlCommandStream.OfType<SwApiVideoSwitcherStatus>().Subscribe(OnVideoSwitcherStatus).AddTo(_connectionDisposables);

            _device.GeneralCommandStream.OfType<SwApiSwBasicInfo>().Subscribe(OnSwBasicInfo).AddTo(_connectionDisposables);
        }

        private void OnStateMode(SwApiStateMode cmd) {
            //Debug.WriteLine("DeviceModel::OnStateMode()");
            _mode.OnNext((SwMode)cmd.Mode);
        }

        private void OnStatePreviewMode(SwApiStatePreviewMode cmd) {
            //Debug.WriteLine("DeviceModel::OnStatePreviewMode()");
            _previewMode.OnNext((PreviewInputType)cmd.Mode);
        }

        private void OnVideoSwitcherStatus(SwApiVideoSwitcherStatus cmd) {
            Debug.WriteLine($"DeviceModel::OnVideoSwitcherStatus() MainSrc={cmd.Status.MainSrc} SubSrc={cmd.Status.SubSrc}");
            _videoSwitcherStatus.OnNext(cmd.Status);
        }

        private void OnSwBasicInfo(SwApiSwBasicInfo info) {
            RevisionNo = info.RevisionNo;
            FirmwareUpdateType = info.Update;
            MacAddress = new PhysicalAddress(info.Mac.Take(6).Reverse().ToArray());

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(Configuration.RegisterClientInterval)).Subscribe(async (_) => await _device.RegisterClient()).AddTo(_connectionDisposables);
        }

        public async Task PreviewInputTile() {
            if(_device != null && _device.IsConnected) {
                await _device.SelectInputForPreview(PreviewInputType.TypeTile);
            }
        }

        public async Task PreviewProgramOut() {
            if(_device != null && _device.IsConnected) {
                await _device.SelectInputForPreview(PreviewInputType.TypeProgramOut);
            }
        }

        public async Task SwitchCut(int number) {
            if(_device != null && _device.IsConnected) {
                await _device.ManualSwitchingCut(number);
            }
        }
    }
}
