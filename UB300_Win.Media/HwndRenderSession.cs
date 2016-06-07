using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.MediaFoundation;
using SharpDX.Win32;

namespace Cerevo.UB300_Win.Media {
    public class HwndRenderSession : IDisposable {
        private bool _disposed = false;
        private readonly BehaviorSubject<bool> _isSessionReady;
        private readonly Subject<int> _playFailed;
        private MediaSession _mediaSession;
        private MediaSessionCallback _callback;
        private VideoDisplayControl _videoControl;
        private SimpleAudioVolume _audioVolume;

        public HwndRenderSession() {
            Hwnd = IntPtr.Zero;
            _isSessionReady = new BehaviorSubject<bool>(false);
            _playFailed = new Subject<int>();
        }

        ~HwndRenderSession() {
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
                Close();
                AfterClose();
                _isSessionReady.Dispose();
                _playFailed.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        public IntPtr Hwnd { get; set; }
        public bool IsLoaded => (_mediaSession != null);
        public bool IsSessionReady => _isSessionReady.Value;
        public IObservable<bool> IsSessionReadyChanged => _isSessionReady.DistinctUntilChanged();
        public IObservable<int> PlayFailed => _playFailed;

        public float Volume {
            get {
                if(!IsLoaded || _audioVolume == null) return 0;
                return _audioVolume.MasterVolume;
            }
            set {
                if(IsLoaded && _audioVolume != null) {
                    _audioVolume.MasterVolume = value;
                }
            }
        }

        public bool IsMuted {
            get {
                if(!IsLoaded || _audioVolume == null) return false;
                return _audioVolume.Mute;
            }
            set {
                if(IsLoaded && _audioVolume != null) {
                    _audioVolume.Mute = value;
                }
            }
        }

        public void Load(object customSource) {
            Close();
            AfterClose();
            if(Hwnd == IntPtr.Zero) {
                throw new InvalidOperationException();
            }
            Trace.WriteLine("HwndRenderSession::Load()");

            MediaSource source = null;
            Topology topo = null;
            PresentationDescriptor pdesc = null;
            try {
                // Create MediaSource(check argument)
                source = ComObject.As<MediaSource>(Marshal.GetIUnknownForObject(customSource)); // GetIUnknownForObject adds reference count
                // Create MediaSession
                MediaFactory.CreateMediaSession(null, out _mediaSession);
                _callback = new MediaSessionCallback(_mediaSession, OnMediaEvent);
                // Create Topology
                MediaFactory.CreateTopology(out topo);

                // Get PresentationDescriptor from MediaSource
                source.CreatePresentationDescriptor(out pdesc);
                // Connect each stream
                for(var i = 0; i < pdesc.StreamDescriptorCount; i++) {
                    RawBool isSelected;
                    using(var sdesc = pdesc.GetStreamDescriptorByIndex(i, out isSelected)) {
                        if(!isSelected) continue;

                        Activate renderer = null;
                        TopologyNode srcnode = null;
                        TopologyNode outnode = null;
                        try {
                            // Renderer
                            if(sdesc.MediaTypeHandler.MajorType == MediaTypeGuids.Video) {
                                MediaFactory.CreateVideoRendererActivate(Hwnd, out renderer);
                            } else if(sdesc.MediaTypeHandler.MajorType == MediaTypeGuids.Audio) {
                                MediaFactory.CreateAudioRendererActivate(out renderer);
                            } else {
                                // not supported
                                continue;
                            }
                            // Source Node
                            MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out srcnode);
                            srcnode.Set(TopologyNodeAttributeKeys.Source, source);
                            srcnode.Set(TopologyNodeAttributeKeys.PresentationDescriptor, pdesc);
                            srcnode.Set(TopologyNodeAttributeKeys.StreamDescriptor, sdesc);
                            // Output Node
                            MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outnode);
                            outnode.Object = renderer;

                            // Connect
                            topo.AddNode(srcnode);
                            topo.AddNode(outnode);
                            srcnode.ConnectOutput(0, outnode, 0);
                        } finally {
                            srcnode?.Dispose();
                            outnode?.Dispose();
                            renderer?.Dispose();
                        }
                    }
                }
                // Set to session
                _mediaSession.SetTopology(SessionSetTopologyFlags.None, topo);
            } catch {
                Close();
                AfterClose();
                throw;
            } finally {
                pdesc?.Dispose();
                topo?.Dispose();
                source?.Dispose();
            }
        }

        public void Close() {
            Stop();
            try {
                _audioVolume?.Dispose();
                _videoControl?.Dispose();
                _mediaSession?.Close();
            } catch(SharpDXException) { }
            _audioVolume = null;
            _videoControl = null;
        }

        private void AfterClose() {
            try {
                _mediaSession?.Shutdown();
                _mediaSession?.Dispose();
                _callback?.Dispose();
            } catch(SharpDXException) { }
            _mediaSession = null;
            _callback = null;
        }

        public void Play() {
            if(!IsLoaded) return;
            Trace.WriteLine("HwndRenderSession::Play()");
            _mediaSession.Start(Guid.Empty, new Variant { Value = 0L });
        }

        public void Stop() {
            if(!IsLoaded) return;
            Trace.WriteLine("HwndRenderSession::Stop()");
            _mediaSession.Stop();
        }

        public bool DrawVideo() {
            if(_videoControl == null) return false;
            try {
                _videoControl.RepaintVideo();
                return true;
            } catch(Exception ex) {
                Debug.WriteLine("HwndRenderSession::DrawVideo(): Exception: " + ex.Message);
                return false;
            }
        }

        public void SetVideoSize(int left, int top, int right, int bottom) {
            _videoControl?.SetVideoPosition(null, new RawRectangle(left, top, right, bottom));
        }

        private void OnMediaEvent(MediaEvent ev) {
            Trace.WriteLine($"HwndRenderSession::OnMediaEvent(): {ev.TypeInfo} {ev.Status}");

            switch(ev.TypeInfo) {
                case MediaEventTypes.SessionClosed:
                    // cleanup
                    AfterClose();
                    _isSessionReady.OnNext(false);
                    break;
                case MediaEventTypes.SessionTopologyStatus:
                    if(ev.Status.Failure) {
                        _playFailed.OnNext(ev.Status.Code);
                        return;
                    }
                    Trace.WriteLine($"HwndRenderSession::OnMediaEvent(): => TopologyStatus={ev.Get(EventAttributeKeys.TopologyStatus)}");
                    if(ev.Status.Success && ev.Get(EventAttributeKeys.TopologyStatus) == TopologyStatus.Ready) {
                        using(var sp = _mediaSession.QueryInterface<ServiceProvider>()) {
                            _videoControl = sp.GetService<VideoDisplayControl>(CustomServiceKeys.VideoRender);
                            _audioVolume = sp.GetService<SimpleAudioVolume>(MediaServiceKeys.PolicyVolume);
                        }
                        _isSessionReady.OnNext(true);
                    }
                    break;
                //case MediaEventTypes.SessionStarted:
                //case MediaEventTypes.EndOfPresentation:
                //case MediaEventTypes.SessionStopped:
                default:
                    break;
            }
        }
    }
}
