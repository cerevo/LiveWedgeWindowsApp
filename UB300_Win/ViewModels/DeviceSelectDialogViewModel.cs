using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Cerevo.UB300_Win.Api;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace Cerevo.UB300_Win.ViewModels {
    public class DeviceSelectDialogViewModel : IDisposable {
        private bool _disposed = false;
        private readonly CompositeDisposable _disposables;

        public IPAddress LocalAddress { get; }
        public string AppVersion { get; }

        private readonly ObservableCollection<DiscoverResult> _devices;
        public ReadOnlyReactiveCollection<DiscoverResult> Devices { get; }

        private readonly BusyNotifier _discoverBusy;
        public ReactiveCommand DiscoverDeviceCommand { get; }

        public ReactiveProperty<bool> IsSearchIpMode { get; }

        [Required]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
        public ReactiveProperty<string> SearchIpAddr { get; }

        public ReactiveCommand SearchIpCommand { get; }
        private readonly BehaviorSubject<bool> _searchFailed;
        public ReadOnlyReactiveProperty<bool> SearchFailed { get; }

        public DeviceSelectDialogViewModel() {
            _disposables = new CompositeDisposable();

            LocalAddress = SwMainApi.GetAllLocalIPv4Addresses().FirstOrDefault();
            var ver = Assembly.GetCallingAssembly().GetName().Version;
            AppVersion = $"{ver.Major}.{ver.Minor}.{ver.Build}";

            _devices = new ObservableCollection<DiscoverResult>();
            Devices = _devices.ToReadOnlyReactiveCollection().AddTo(_disposables);

            _discoverBusy = new BusyNotifier();
            DiscoverDeviceCommand = _discoverBusy.Inverse().ToReactiveCommand().AddTo(_disposables);
            DiscoverDeviceCommand.Delay(new TimeSpan(100)).Subscribe(_ => DiscoverDevice()).AddTo(_disposables);

            IsSearchIpMode = new ReactiveProperty<bool>(false).AddTo(_disposables);
            IsSearchIpMode.Subscribe(b => {
                if(b) {
                    // Enter mode: clear fail flag.
                    _searchFailed.OnNext(false);
                } else if(DiscoverDeviceCommand.CanExecute()) {
                    // Exit mode: rescan devices.
                    DiscoverDeviceCommand.Execute();
                }
            }).AddTo(_disposables);

            _searchFailed = new BehaviorSubject<bool>(false).AddTo(_disposables);
            SearchFailed = _searchFailed.ToReadOnlyReactiveProperty().AddTo(_disposables);
            SearchIpAddr = new ReactiveProperty<string>().SetValidateAttribute(() => SearchIpAddr).AddTo(_disposables);
            SearchIpCommand = Observable.CombineLatest(SearchIpAddr.ObserveHasErrors, _discoverBusy, (e, c) => !e && !c)
                .ToReactiveCommand().AddTo(_disposables);
            SearchIpCommand.Subscribe(_ => SearchIp()).AddTo(_disposables);
        }

        ~DeviceSelectDialogViewModel() {
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
                _disposables.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        private void DiscoverDevice() {
            var busy = _discoverBusy.ProcessStart();
            _devices.Clear();
            SwMainApi.Discover().Subscribe(d => _devices.Add(d), (ex) => busy.Dispose(), () => busy.Dispose());
        }

        private void SearchIp() {
            IPAddress addr;
            if(!IPAddress.TryParse(SearchIpAddr.Value, out addr)) {
                _searchFailed.OnNext(true);
                return;
            }

            var busy = _discoverBusy.ProcessStart();
            _devices.Clear();
            SwMainApi.DiscoverWithIp(addr).Subscribe(d => _devices.Add(d),
                (ex) => {
                    _searchFailed.OnNext(true);
                    busy.Dispose();
                }, () => {
                    _searchFailed.OnNext(!_devices.Any());
                    busy.Dispose();
                });
        }
    }
}
