using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.MediaFoundation;
using SharpDX.Win32;

namespace Cerevo.UB300_Win.Media {
    [ComVisible(true)]
    [Guid("DB94A290-5F40-4FEF-884C-10577437A831")]
    public class RtspMediaSource : IMFMediaSource, IDisposable {
        public struct StreamConfig {
            public IObservable<IRtspSample> SourceStream;
            public string ParameterString;
            public Ratio FixedFrameRate;
            public string DebugSaveFilename;
        }

        public enum SourceState {
            Closed,
            Playing,
            Paused,
            Stopped,
            Shutdowned
        }

        private bool _disposed = false;
        private readonly MediaEventGeneratorImpl _eventGenerator;
        private readonly BehaviorSubject<SourceState> _stateSubject;
        private RtspMediaStream[] _streams;
        private PresentationDescriptor _presentationDescriptor;
        private IDisposable _isSampleQueueEmptySubscription;
        private SpinLock _bufferingStateLock;
        private bool _bufferingStartedSent;

        public RtspMediaSource() {
            _eventGenerator = new MediaEventGeneratorImpl();
            _stateSubject = new BehaviorSubject<SourceState>(SourceState.Closed);
            _streams = new RtspMediaStream[0];
            _presentationDescriptor = null;
            _isSampleQueueEmptySubscription = null;
            _bufferingStateLock = new SpinLock();
            _bufferingStartedSent = false;
        }

        ~RtspMediaSource() {
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
                _stateSubject.Dispose();
                _eventGenerator.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        #region IMFMediaEventGenerator members
        public void GetEvent(uint dwFlags, /*IMFMediaEvent*/out IntPtr ppEvent) {
            CheckShutdown();
            _eventGenerator.GetEvent(dwFlags, out ppEvent);
        }

        public void BeginGetEvent(/*IMFAsyncCallback*/IntPtr pCallback, /*IUnknown*/IntPtr punkState) {
            CheckShutdown();
            _eventGenerator.BeginGetEvent(pCallback, punkState);
        }

        public void EndGetEvent(/*IMFAsyncResult*/IntPtr pResult, /*IMFMediaEvent*/out IntPtr ppEvent) {
            CheckShutdown();
            _eventGenerator.EndGetEvent(pResult, out ppEvent);
        }

        public void QueueEvent(MediaEventTypes met, ref Guid guidExtendedType, int hrStatus, /*ref Variant*/IntPtr pvValue) {
            CheckShutdown();
            _eventGenerator.QueueEvent(met, ref guidExtendedType, hrStatus, pvValue);
        }
        #endregion

        #region IMFMediaSource members
        public void GetCharacteristics(out MediaSourceCharacteristics pdwCharacteristics) {
            pdwCharacteristics = MediaSourceCharacteristics.IsLive;
        }

        public void CreatePresentationDescriptor(/*IMFPresentationDescriptor*/ out IntPtr ppPresentationDescriptor) {
            CheckShutdown();
            Trace.WriteLine("RtspMediaSource::CreatePresentationDescriptor()");
            if(!IsOpened) {
                throw new SharpDXException(ResultCode.NotInitializeD);
            }
            if(_presentationDescriptor == null) {
                throw new SharpDXException(ResultCode.InvalidStateTransition);
            }
            // Clone
            PresentationDescriptor pd = null;
            try {
                _presentationDescriptor.Clone(out pd);
                ppPresentationDescriptor = pd.Detach();
            } finally {
                pd?.Dispose();
            }
        }

        public void Start(/*IMFPresentationDescriptor*/IntPtr pPresentationDescriptor, ref Guid pguidTimeFormat, ref Variant pvarStartPosition) {
            CheckShutdown();
            Trace.WriteLine("RtspMediaSource::Start()");
            if(!IsStopped && !IsPaused) {
                throw new SharpDXException(ResultCode.InvalidStateTransition);
            }
            if(pguidTimeFormat != null && pguidTimeFormat != Guid.Empty) {
                throw new SharpDXException(ResultCode.UnsupportedTimeFormat);
            }

            if(pPresentationDescriptor == IntPtr.Zero) {
                throw new ArgumentNullException();
            }
            var pd = ComObjectUtils.AttachAs<PresentationDescriptor>(pPresentationDescriptor);
            try {
                // Get stream selection
                var selection = new bool[_streams.Length];
                for(var i = 0; i < pd.StreamDescriptorCount; i++) {
                    RawBool isSelected;
                    var sdesc = pd.GetStreamDescriptorByIndex(i, out isSelected);
                    var id = sdesc.StreamIdentifier;
                    switch(id) {
                        case (int)RtspMediaStream.StreamType.Video:
                        case (int)RtspMediaStream.StreamType.Audio:
                            selection[id] = isSelected;
                            break;
                        default:
                            throw new ArgumentException();
                    }
                }
                // Activate selected stream
                for(var i = 0; i < selection.Length; i++) {
                    if(selection[i]) {
                        using(var comStream = new ComObject(_streams[i])) {
                            if(_streams[i].IsActive) {
                                // stream updated
                                _eventGenerator.QueueEventParamUnk(MediaEventTypes.UpdatedStream, Guid.Empty, Result.Ok, comStream);
                            } else {
                                // new stream started
                                _streams[i].Activate();
                                _eventGenerator.QueueEventParamUnk(MediaEventTypes.NewStream, Guid.Empty, Result.Ok, comStream);
                            }
                        }
                    } else {
                        _streams[i].Deactivate();
                    }
                }

                // Ignore pvarStartPosition
                _bufferingStartedSent = false;
                ChangeState(SourceState.Playing);
                _eventGenerator.QueueEventParamVar(MediaEventTypes.SourceStarted, Guid.Empty, Result.Ok, new Variant { Value = 0L });
            } catch(Exception ex) {
                if(!IsShutdown) {
                    _eventGenerator.QueueEventParamErr(ex.HResult);
                }
                throw;
            } finally {
                pd?.Dispose();
            }
        }

        public void Stop() {
            CheckShutdown();
            Trace.WriteLine("RtspMediaSource::Stop()");
            if(!IsPlaying && !IsPaused) {
                throw new SharpDXException(ResultCode.InvalidStateTransition);
            }
            ChangeState(SourceState.Stopped);
            _eventGenerator.QueueEventParamNone(MediaEventTypes.SourceStopped);
        }

        public void Pause() {
            CheckShutdown();
            Trace.WriteLine("RtspMediaSource::Pause()");
            /*if(!IsPlaying) {
                // Pausing is allowed only from the started state. 
                throw new SharpDXException(ResultCode.InvalidStateTransition);
            }*/
            ChangeState(SourceState.Paused);
            _eventGenerator.QueueEventParamNone(MediaEventTypes.SourcePaused);
        }

        public void Shutdown() {
            Trace.WriteLine("RtspMediaSource::Shutdown()");
            if(IsOpened) Close();
            ChangeState(SourceState.Shutdowned);
            _stateSubject.OnCompleted();
            _eventGenerator.Shutdown();
        }
        #endregion

        public SourceState CurrentState => _stateSubject.Value;
        public bool IsOpened => (CurrentState != SourceState.Closed && CurrentState != SourceState.Shutdowned);
        public bool IsPlaying => (CurrentState == SourceState.Playing);
        public bool IsPaused => (CurrentState == SourceState.Paused);
        public bool IsStopped => (CurrentState == SourceState.Stopped);
        public bool IsShutdown => (CurrentState == SourceState.Shutdowned);

        public IObservable<SourceState> StateChanged => _stateSubject.DistinctUntilChanged();

        private void CheckShutdown() {
            if(_disposed || IsShutdown) {
                throw new SharpDXException(ResultCode.Shutdown);
            }
        }

        private void ChangeState(SourceState newState) {
            _stateSubject.OnNext(newState);
        }

        private void NotifyBuffering(bool isSampleQueueEmpty) {
            if(!IsPlaying) return;
            var lockTaken = false;
            try {
                _bufferingStateLock.Enter(ref lockTaken);
                if(isSampleQueueEmpty && !_bufferingStartedSent) {
                    Trace.WriteLine("RtspMediaSource::NotifyBuffering() Buffering Started.");
                    _eventGenerator.QueueEventParamNone(MediaEventTypes.BufferingStarted);
                    _bufferingStartedSent = true;
                } else if(!isSampleQueueEmpty && _bufferingStartedSent) {
                    Trace.WriteLine("RtspMediaSource::NotifyBuffering() Buffering Stopped.");
                    _eventGenerator.QueueEventParamNone(MediaEventTypes.BufferingStopped);
                    _bufferingStartedSent = false;
                }
            } finally {
                if(lockTaken) _bufferingStateLock.Exit(false);
            }
        }

        public void Open(StreamConfig videoConfig, StreamConfig audiocConfig) {
            CheckShutdown();
            if(IsOpened) {
                throw new SharpDXException(ResultCode.InvalidStateTransition);
            }
            Close();
            Trace.WriteLine("RtspMediaSource::Open()");

            try {
                // Create streams
                _streams = new[] {
                    new RtspMediaStream(this, RtspMediaStream.StreamType.Video, videoConfig),
                    new RtspMediaStream(this, RtspMediaStream.StreamType.Audio, audiocConfig)
                };

                // Create PresentationDescriptor
                MediaFactory.CreatePresentationDescriptor(2, new[] { _streams[0].Descriptor, _streams[1].Descriptor }, out _presentationDescriptor);
                // Mark all stream activated
                _presentationDescriptor.SelectStream(0);
                _presentationDescriptor.SelectStream(1);

                // Monitor video sample queue
                _isSampleQueueEmptySubscription = _streams[0].IsSampleQueueEmptyChanged.Subscribe(NotifyBuffering);

                ChangeState(SourceState.Stopped);
            } catch {
                Close();
                throw;
            }
        }

        private void Close() {
            if(IsPlaying || IsPaused) {
                Stop();
            }
            if(IsOpened) {
                ChangeState(SourceState.Closed);
            }

            _isSampleQueueEmptySubscription?.Dispose();
            _isSampleQueueEmptySubscription = null;

            foreach(var s in _streams) {
                s?.Dispose();
            }
            _streams = new RtspMediaStream[0];

            _presentationDescriptor?.Dispose();
            _presentationDescriptor = null;
        }
    }
}
