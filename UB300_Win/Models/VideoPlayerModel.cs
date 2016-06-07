using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cerevo.UB300_Win.Api;
using Cerevo.UB300_Win.Media;

namespace Cerevo.UB300_Win.Models {
    public class VideoPlayerModel : IDisposable {
        private bool _disposed = false;

        private RtspHandler _rtspHandler;
        private RtspMediaSource _mediaSource;
        private readonly SemaphoreSlim _sessionSetupLock;
        private readonly HwndRenderSession _session;

        public bool IsPlaying => _session.IsSessionReady;
        public IObservable<bool> IsPlayingChanged => _session.IsSessionReadyChanged;
        private static readonly TimeSpan OperationTimeoutSpan = TimeSpan.FromMilliseconds(3000);

        public bool IsEnabled { get; private set; }

        private Int32Rect _videoHostPosition;
        public Int32Rect VideoHostPosition {
            get { return _videoHostPosition; }
            set {
                _videoHostPosition = value;
                SetVideoSize(_videoHostPosition);
            }
        }

        private float _volume;
        public float Volume {
            get { return _volume; }
            set {
                _volume = value;
                _session.Volume = _volume;
            }
        }

        private bool _isMuted;
        public bool IsMuted {
            get { return _isMuted; }
            set {
                _isMuted = value;
                _session.IsMuted = _isMuted;
            }
        }

        public VideoPlayerModel() {
            _rtspHandler = null;
            _mediaSource = null;
            _sessionSetupLock = new SemaphoreSlim(1, 1);
            _session = new HwndRenderSession();
            _session.IsSessionReadyChanged.Where(b => b).Subscribe(_ => {
                SetVideoSize(_videoHostPosition);
                _session.Volume = _volume;
                _session.IsMuted = _isMuted;
            });

            IsEnabled = true;
            _videoHostPosition = Int32Rect.Empty;
            _volume = 1.0f;
            _isMuted = false;
        }

        ~VideoPlayerModel() {
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
                await StopVideo();
                _session.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        public void SetEnable() {
            IsEnabled = true;
        }

        public void SetVideoHostHandle(IntPtr handle) {
            _session.Hwnd = handle;
        }

        public bool DrawVideo() {
            return IsPlaying && _session.DrawVideo();
        }

        public async Task PlayVideo(Uri uri) {
            if(await _sessionSetupLock.WaitAsync(0) == false) return;
            Debug.WriteLine("VideoPlayerViewModel::PlayVideo()");
            try {
                if(_rtspHandler != null || _mediaSource != null) {
                    await StopVideoCore();
                }

                _rtspHandler = new RtspHandler(uri);
                _rtspHandler.Connect();
                _mediaSource = new RtspMediaSource();
                var videoConfig = new RtspMediaSource.StreamConfig() {
                    SourceStream = _rtspHandler.VideoStream,
                    ParameterString = _rtspHandler.VideoParameter,
                    FixedFrameRate = new Ratio(Configuration.VideoFrameRate, 1)
                };
                var audioConfig = new RtspMediaSource.StreamConfig() {
                    SourceStream = _rtspHandler.AudioStream,
                    ParameterString = _rtspHandler.AudioParameter
                };

                _mediaSource.Open(videoConfig, audioConfig);
                _session.Load(_mediaSource);

                _rtspHandler.Play();
                // Wait PlayStarted to be true
                await _rtspHandler.PlayStarted.Where(b => b).Timeout(OperationTimeoutSpan).Take(1);
                _session.Play();
                // Wait IsPlaying to be true or PlayFailed
                var playresult = await _session.IsSessionReadyChanged.Where(b => b).Select(b => 0).Merge(_session.PlayFailed).Timeout(OperationTimeoutSpan).Take(1);
                if(playresult != 0) {
                    Debug.WriteLine($"VideoPlayerViewModel::PlayVideo() Play Failed. Code=0x{playresult:X8}");
                    IsEnabled = false;
                    await StopVideoCore();
                }
            } catch(Exception ex) {
                Debug.WriteLine($"VideoPlayerViewModel::PlayVideo() Exception '{ex.Message}'");
                IsEnabled = false;
            } finally {
                _sessionSetupLock.Release();
            }
        }

        public async Task StopVideo() {
            await _sessionSetupLock.WaitAsync();
            try {
                await StopVideoCore();
            } finally {
                _sessionSetupLock.Release();
            }
        }

        private async Task StopVideoCore() {
            Debug.WriteLine("VideoPlayerViewModel::StopVideoCore()");
            try {
                _session.Stop();
                _session.Close();
                // Wait IsSessionReady to be false
                await _session.IsSessionReadyChanged.Where(b => !b).Timeout(OperationTimeoutSpan).Take(1);
                _rtspHandler?.Disconnect();
                _rtspHandler?.Dispose();
                _mediaSource?.Shutdown();
                _mediaSource?.Dispose();
            } catch(Exception ex) {
                Debug.WriteLine($"VideoPlayerViewModel::StopVideoCore() Exception '{ex.Message}'");
            } finally {
                _rtspHandler = null;
                _mediaSource = null;
            }
        }

        private void SetVideoSize(Int32Rect rc) {
            _session.SetVideoSize(rc.X, rc.Y, rc.X + rc.Width, rc.Y + rc.Height);
        }
    }
}
