using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using Cerevo.UB300_Win.Api;
using Cerevo.UB300_Win.Models;
using Cerevo.UB300_Win.Views;
using MaterialDesignThemes.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Cerevo.UB300_Win.ViewModels {
    public class MainWindowViewModel : IDisposable {
        private enum ViewMode {
            None,
            Rec,
            Live,
            Input,
            Output,
        }

        private const double Epsilon = 1.0e-06;
        private bool _disposed = false;
        private readonly CompositeDisposable _disposables;
        private readonly CompositeDisposable _connectionDisposables;

        public VideoPlayerModel VideoPlayer { get; }
        public DeviceModel ConnectedDevice { get; private set; }
        public ReactiveCommand SelectDeviceCommand { get; }

        private readonly BehaviorSubject<ViewMode> _viewMode;
        public ReactiveProperty<bool> IsModeRec { get; }
        public ReactiveProperty<bool> IsModeLive { get; }
        public ReactiveProperty<bool> IsModeInput { get; }
        public ReactiveProperty<bool> IsModeOutput { get; }

        public ReactiveProperty<double>[] SliderValue { get; }
        public ReactiveProperty<bool>[] IsSliderActive { get; }
        public ReactiveProperty<double> SliderMaximum { get; }

        public MainWindowViewModel() {
            _disposables = new CompositeDisposable();
            _connectionDisposables = new CompositeDisposable();

            VideoPlayer = new VideoPlayerModel().AddTo(_disposables);
            ConnectedDevice = new DeviceModel();
            SelectDeviceCommand = new ReactiveCommand().AddTo(_disposables);
            SelectDeviceCommand.Delay(new TimeSpan(100)).ObserveOnUIDispatcher().SelectMany(_ => DialogHost.Show(new DeviceSelectDialog(), OnSelectDeviceDialogClosing).ToObservable())
                .Subscribe(async (result) => await ConnenctDevice(result as DiscoverResult)).AddTo(_disposables);

            _viewMode = new BehaviorSubject<ViewMode>(ViewMode.None).AddTo(_disposables);
            IsModeRec = new ReactiveProperty<bool>().AddTo(_disposables);
            IsModeLive = new ReactiveProperty<bool>().AddTo(_disposables);
            IsModeInput = new ReactiveProperty<bool>().AddTo(_disposables);
            IsModeOutput = new ReactiveProperty<bool>().AddTo(_disposables);
            _viewMode.DistinctUntilChanged().Subscribe(m => {
                IsModeRec.Value = (m == ViewMode.Rec);
                IsModeLive.Value = (m == ViewMode.Live);
                IsModeInput.Value = (m == ViewMode.Input);
                IsModeOutput.Value = (m == ViewMode.Output);
            }).AddTo(_disposables);
            IsModeRec.Where(b => b).Subscribe(async (_) => await ToRecMode()).AddTo(_disposables);
            IsModeLive.Where(b => b).Subscribe(async (_) => await ToLiveMode()).AddTo(_disposables);
            IsModeInput.Where(b => b).Subscribe(async (_) => await ToInputMode()).AddTo(_disposables);
            IsModeOutput.Where(b => b).Subscribe(async (_) => await ToOutputMode()).AddTo(_disposables);
            _viewMode.OnNext(ViewMode.Output);

            SliderValue = new[] {
                new ReactiveProperty<double>(1.0).AddTo(_disposables),
                new ReactiveProperty<double>(0.0).AddTo(_disposables),
                new ReactiveProperty<double>(0.0).AddTo(_disposables),
                new ReactiveProperty<double>(0.0).AddTo(_disposables)
            };
            SliderMaximum = new ReactiveProperty<double>(1.0).AddTo(_disposables);
            IsSliderActive = new[] {
                new ReactiveProperty<bool>(true).AddTo(_disposables),
                new ReactiveProperty<bool>(false).AddTo(_disposables),
                new ReactiveProperty<bool>(false).AddTo(_disposables),
                new ReactiveProperty<bool>(false).AddTo(_disposables)
            };
            SliderValue[0].Subscribe(async (value) => await OnSliderValueChanged(0, value)).AddTo(_disposables);
            SliderValue[1].Subscribe(async (value) => await OnSliderValueChanged(1, value)).AddTo(_disposables);
            SliderValue[2].Subscribe(async (value) => await OnSliderValueChanged(2, value)).AddTo(_disposables);
            SliderValue[3].Subscribe(async (value) => await OnSliderValueChanged(3, value)).AddTo(_disposables);
        }

        ~MainWindowViewModel() {
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

        protected virtual async void Dispose(bool disposing) {
            if(_disposed) return;

            if(disposing) {
                // Dispose managed resources.
                await VideoPlayer.StopVideo();
                _connectionDisposables.Dispose();
                ConnectedDevice.Dispose();
                _disposables.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        private void OnSelectDeviceDialogClosing(object sender, DialogClosingEventArgs e) {
            var result = e.Parameter as DiscoverResult;
            if(result == null) {
                e.Cancel();
                return;
            }

            var dialog = e.Session.Content as FrameworkElement;
            var viewModel = dialog?.DataContext as IDisposable;
            viewModel?.Dispose();
        }

        private async Task ConnenctDevice(DiscoverResult result) {
            await VideoPlayer.StopVideo();

            _connectionDisposables.Clear();
            ConnectedDevice.Dispose();
            ConnectedDevice = new DeviceModel();
            if(result == null) {
                return;
            }

            var oldViewMode = _viewMode.Value;
            var nextDevice = new DeviceModel(result);
            nextDevice.ModeChanged.Subscribe(async (_) => await OnDeviceModeChanged()).AddTo(_connectionDisposables);
            nextDevice.PreviewModeChanged.Subscribe(async (_) => await OnDeviceModeChanged()).AddTo(_connectionDisposables);
            nextDevice.VideoSwitcherStatusChanged.Subscribe(OnVideoSwitcherStatusChanged).AddTo(_connectionDisposables);
            await nextDevice.Connect();

            if(nextDevice.IsConnected == false) {
                _connectionDisposables.Clear();
                nextDevice.Dispose();
                return;
            }

            switch(oldViewMode) {
                case ViewMode.Input:
                    await nextDevice.PreviewInputTile();
                    break;
                case ViewMode.Output:
                    await nextDevice.PreviewProgramOut();
                    break;
                default:
                    break;
            }

            ConnectedDevice = nextDevice;
            VideoPlayer.SetEnable();
        }

        private async Task OnDeviceModeChanged() {
            if(ConnectedDevice.IsModeRtsp) {
                _viewMode.OnNext(ConnectedDevice.IsPreviewOutput ? ViewMode.Output : ViewMode.Input);
                if(ConnectedDevice.IsConnected && VideoPlayer.IsEnabled && !VideoPlayer.IsPlaying) {
                    await VideoPlayer.PlayVideo(ConnectedDevice.PreviewUri);
                }
            } else {
                await VideoPlayer.StopVideo();
                _viewMode.OnNext(ConnectedDevice.IsModeRecording ? ViewMode.Rec : ViewMode.Live);
            }
        }

        private void OnVideoSwitcherStatusChanged(VideoSwitcherStatus status) {
            // for Cut mode
            var index = (int)status.MainSrc - 1;
            if(index < 0 || index >= SliderValue.Length) return;

            // Change active slider
            for(var i = 0; i < SliderValue.Length; i++) {
                IsSliderActive[i].Value = (i == index);
            }
            for(var i = 0; i < SliderValue.Length; i++) {
                SliderValue[i].Value = IsSliderActive[i].Value ? 1.0 : 0.0;
            }
        }

        private async Task OnSliderValueChanged(int index, double newValue) {
            // for Cut mode
            if(IsSliderActive[index].Value) {
                // Cancel deactivation (Keep 1.0)
                if(SliderValue[index].Value < 1.0) {
                    SliderValue[index].Value = 1.0;
                }
                return;
            }
            if(newValue < Epsilon) {
                // Do nothing
                return;
            }

            // Change active slider
            for(var i = 0; i < SliderValue.Length; i++) {
                IsSliderActive[i].Value = (i == index);
            }
            for(var i = 0; i < SliderValue.Length; i++) {
                SliderValue[i].Value = IsSliderActive[i].Value ? 1.0 : 0.0;
            }

            await ConnectedDevice.SwitchCut(index);
        }

        private async Task ToRecMode() {
            if(_viewMode.Value == ViewMode.Rec) return;
            _viewMode.OnNext(ViewMode.Rec);

            if(VideoPlayer.IsPlaying) {
                await VideoPlayer.StopVideo();
            }
        }

        private async Task ToLiveMode() {
            if(_viewMode.Value == ViewMode.Live) return;
            _viewMode.OnNext(ViewMode.Live);

            if(VideoPlayer.IsPlaying) {
                await VideoPlayer.StopVideo();
            }
        }

        private async Task ToInputMode() {
            if(_viewMode.Value == ViewMode.Input) return;
            _viewMode.OnNext(ViewMode.Input);

            if(ConnectedDevice.IsConnected) {
                await ConnectedDevice.PreviewInputTile();
            }
        }

        private async Task ToOutputMode() {
            if(_viewMode.Value == ViewMode.Output) return;
            _viewMode.OnNext(ViewMode.Output);

            if(ConnectedDevice.IsConnected) {
                await ConnectedDevice.PreviewProgramOut();
            }
        }
    }
}
