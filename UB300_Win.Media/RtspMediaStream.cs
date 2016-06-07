using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Win32;

namespace Cerevo.UB300_Win.Media {
    [ComVisible(true)]
    [Guid("A27D5CD0-8311-48AC-994F-5637372D6A8B")]
    internal class RtspMediaStream : IMFMediaStream, IDisposable {
        public enum StreamType {
            Video,
            Audio
        }

        private const int MaxQueueLength = 200;
        private bool _disposed = false;
        private RtspMediaSource.StreamConfig _config;
        private readonly MediaEventGeneratorImpl _eventGenerator;
        private readonly IDisposable _stateSubscription;
        private readonly IDisposable _packetSubscription;
        private SpinLock _queueConsumerLock;
        private readonly ConcurrentQueue<IRtspSample> _sampleQueue;
        private readonly ConcurrentQueue<ComObject> _requestQueue;
        private readonly BehaviorSubject<bool> _isSampleQueueEmpty;
        private readonly Stream _debugSaveStream;

        public RtspMediaStream(RtspMediaSource source, StreamType type, RtspMediaSource.StreamConfig config) {
            _eventGenerator = new MediaEventGeneratorImpl();
            _queueConsumerLock = new SpinLock();
            _sampleQueue = new ConcurrentQueue<IRtspSample>();
            _requestQueue = new ConcurrentQueue<ComObject>();
            _isSampleQueueEmpty = new BehaviorSubject<bool>(false);
            MediaSource = source;
            Type = type;
            _config = config;
            Descriptor = (type == StreamType.Video) ? CreateVideoStreamDescriptor((int)type) : CreateAudioStreamDescriptor((int)type);
            IsActive = false;
            // Subscribe packet arrival
            _packetSubscription = _config.SourceStream.ObserveOn(ThreadPoolScheduler.Instance).Subscribe(HandlePacket);
            // Subscribe state change
            _stateSubscription = source.StateChanged.Subscribe(HandleStateChange);
            if(!string.IsNullOrEmpty(_config.DebugSaveFilename)) {
                _debugSaveStream = new FileStream(_config.DebugSaveFilename, FileMode.Create);
            }
        }

        ~RtspMediaStream() {
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
                Deactivate();
                _stateSubscription.Dispose();
                _packetSubscription.Dispose();
                Descriptor.Dispose();
                _eventGenerator.Shutdown();
                _eventGenerator.Dispose();
                _debugSaveStream?.Close();
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
        public void GetMediaSource(out IMFMediaSource ppMediaSource) {
            CheckShutdown();
            Trace.WriteLine($"RtspMediaStream<{Type}>::GetMediaSource()");
            ppMediaSource = MediaSource;
        }

        public void GetStreamDescriptor(/*IMFStreamDescriptor*/out IntPtr ppStreamDescriptor) {
            CheckShutdown();
            Trace.WriteLine($"RtspMediaStream<{Type}>::GetStreamDescriptor()");
            ppStreamDescriptor = Descriptor.NativePointer;
            Marshal.AddRef(Descriptor.NativePointer);
        }

        public void RequestSample(/*IUnknown*/IntPtr pToken) {
            CheckShutdown();
            if(!IsActive || (!MediaSource.IsPlaying && !MediaSource.IsPaused)) {
                throw new SharpDXException(ResultCode.InvalidRequest);
            }
            //Trace.WriteLine($"RtspMediaStream<{Type}>::RequestSample()");

            try {
                _requestQueue.Enqueue(ComObjectUtils.Attach(pToken));
                if(!_sampleQueue.IsEmpty) {
                    DeliverSamples();
                }
                _isSampleQueueEmpty.OnNext(_sampleQueue.IsEmpty);
                //if(Type == StreamType.Video) {
                //    Debug.WriteLine($"RtspMediaStream<{Type}>::RequestSample(): SampleQueue={_sampleQueue.Count} RequestQueue={_requestQueue.Count}");
                //}
            } catch(Exception ex) {
                if(!MediaSource.IsShutdown) {
                    _eventGenerator.QueueEventParamErr(ex.HResult);
                }
                Debug.WriteLine($"RtspMediaStream<{Type}>::RequestSample(): Error={ex.Message}");
                throw;
            }
        }
        #endregion

        public RtspMediaSource MediaSource { get; }
        public StreamType Type { get; }
        public StreamDescriptor Descriptor { get; }
        public bool IsActive { get; private set; }
        public IObservable<bool> IsSampleQueueEmptyChanged => _isSampleQueueEmpty;

        private void CheckShutdown() {
            if(_disposed || MediaSource.IsShutdown) {
                throw new SharpDXException(ResultCode.Shutdown);
            }
        }

        public void Activate() {
            if(IsActive) return;

            Trace.WriteLine($"RtspMediaStream<{Type}>::Activate()");
            IsActive = true;
        }

        public void Deactivate() {
            if(!IsActive) return;

            Trace.WriteLine($"RtspMediaStream<{Type}>::Deactivate()");
            IsActive = false;
            ClearQueue();
        }

        private void HandleStateChange(RtspMediaSource.SourceState state) {
            if(!IsActive) return;

            switch(state) {
                case RtspMediaSource.SourceState.Playing:
                    _eventGenerator.QueueEventParamVar(MediaEventTypes.StreamStarted, Guid.Empty, Result.Ok, new Variant { Value = 0L });
                    break;
                case RtspMediaSource.SourceState.Paused:
                    _eventGenerator.QueueEventParamNone(MediaEventTypes.StreamPaused);
                    break;
                case RtspMediaSource.SourceState.Stopped:
                    ClearQueue();
                    _eventGenerator.QueueEventParamNone(MediaEventTypes.StreamStopped);
                    break;
                case RtspMediaSource.SourceState.Shutdowned:
                    ClearQueue();
                    _eventGenerator.Shutdown();
                    break;
                default:
                    break;
            }
        }

        private void HandlePacket(IRtspSample packet) {
            //if(!IsActive || !MediaSource.IsPlaying || packet == null) return;
            //Trace.WriteLine($"RtspMediaStream<{Type}>::HandlePacket(): Length={packet.Buffer.Length}");

            try {
                if(!packet.IsKeyFrame && _sampleQueue.Count > MaxQueueLength) {
                    // Sample queue is too long. Discard non-Key frame
                    Debug.WriteLine($"RtspMediaStream<{Type}>::HandlePacket(): SampleQueue is too long. Discard non-Key frame.");
                    return;
                }

                _sampleQueue.Enqueue(packet);
                if(!_requestQueue.IsEmpty) {
                    DeliverSamples();
                }
                _isSampleQueueEmpty.OnNext(_sampleQueue.IsEmpty);
                //if(Type == StreamType.Video) {
                //    Debug.WriteLine($"RtspMediaStream<{Type}>::HandlePacket():  SampleQueue={_sampleQueue.Count} RequestQueue={_requestQueue.Count}");
                //}
            } catch (Exception ex) {
                if(!MediaSource.IsShutdown) {
                    _eventGenerator.QueueEventParamErr(ex.HResult);
                }
                Debug.WriteLine($"RtspMediaStream<{Type}>::HandlePacket(): Error={ex.Message}");
                //throw;
            }
        }

        private void DeliverSamples() {
            while(true) {
                var lockTaken = false;
                ComObject requestToken;
                IRtspSample sample;
                try {
                    _queueConsumerLock.Enter(ref lockTaken);
                    if(_requestQueue.IsEmpty || _sampleQueue.IsEmpty) return;

                    _sampleQueue.TryDequeue(out sample);
                    _requestQueue.TryDequeue(out requestToken);
                } finally {
                    if(lockTaken) _queueConsumerLock.Exit(false);
                }
                Debug.Assert(requestToken != null && sample != null);
                DeliverSample(requestToken, sample);
            }
        }

        private void DeliverSample(ComObject requestToken, IRtspSample packet) {
            using(var sample = MediaFactory.CreateSample()) {
                using(var buffer = MediaFactory.CreateMemoryBuffer(packet.Buffer.Length)) {
                    int max, cur;
                    var ptr = buffer.Lock(out max, out cur);
                    Marshal.Copy(packet.Buffer, 0, ptr, packet.Buffer.Length);
                    buffer.CurrentLength = packet.Buffer.Length;
                    buffer.Unlock();
                    sample.AddBuffer(buffer);
                }
                sample.SampleTime = packet.SampleTime;
                if(requestToken.NativePointer != IntPtr.Zero) {
                    sample.Set(SampleAttributeKeys.Token, requestToken);
                }
                if(packet.IsKeyFrame) {
                    sample.Set(SampleAttributeKeys.CleanPoint, true);
                }
                if(packet.Discontinuity) {
                    sample.Set(SampleAttributeKeys.Discontinuity, true);
                }
                if(Type == StreamType.Video) {
                    sample.Set(NaluAttributeKeys.LengthInformation, packet.LengthInfo.Select(BitConverter.GetBytes).SelectMany(b => b).ToArray());
                }
                //Trace.WriteLine($"RtspMediaStream<{Type}>::DeliverSample() SampleTime={packet.SampleTime / 10000}(ms) Length={packet.Buffer.Length}");
                _eventGenerator.QueueEventParamUnk(MediaEventTypes.MediaSample, Guid.Empty, Result.Ok, sample);
            }
            requestToken.Dispose();
            _debugSaveStream?.Write(packet.Buffer, 0, packet.Buffer.Length);
        }

        private void ClearQueue() {
            var lockTaken = false;
            try {
                _queueConsumerLock.Enter(ref lockTaken);
                IRtspSample sample;
                ComObject requestToken;
                while(_sampleQueue.TryDequeue(out sample)) { }
                while(_requestQueue.TryDequeue(out requestToken)) { requestToken.Dispose(); }
            } finally {
                if(lockTaken) _queueConsumerLock.Exit(false);
            }
        }

        private StreamDescriptor CreateVideoStreamDescriptor(int id) {
            var parts = _config.ParameterString.Split(',');
            Debug.Assert(parts.Length == 2);
            var sequenceParameterSet = Convert.FromBase64String(parts[0]);
            var pictureParameterSet = Convert.FromBase64String(parts[1]);
            var param = VideoParameterParser.Parse(sequenceParameterSet, pictureParameterSet);

            using(var mtVideo = new MediaType()) {
                MediaFactory.CreateMediaType(mtVideo);
                // Media Type Attributes https://msdn.microsoft.com/en-us/library/windows/desktop/aa376629(v=vs.85).aspx
                mtVideo.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                mtVideo.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.H264);
                mtVideo.Set(MediaTypeAttributeKeys.Mpeg2Level, param.H264Level);
                mtVideo.Set(MediaTypeAttributeKeys.Mpeg2Profile, param.IdcProfile);
                mtVideo.Set(MediaTypeAttributeKeys.MpegSequenceHeader, sequenceParameterSet);
                mtVideo.Set(NaluAttributeKeys.LengthSet, 1);
                mtVideo.Set(MediaTypeAttributeKeys.FrameSize, (param.FrameWidth << 32) | param.FrameHeight);
                mtVideo.Set(MediaTypeAttributeKeys.PixelAspectRatio, (param.AspectRatio.Numerator << 32) | param.AspectRatio.Denominator);
                if(_config.FixedFrameRate.IsValid()) {
                    mtVideo.Set(MediaTypeAttributeKeys.FrameRate, (_config.FixedFrameRate.Numerator << 32) | _config.FixedFrameRate.Denominator);
                } else if(param.FrameRate.IsValid()) {
                    mtVideo.Set(MediaTypeAttributeKeys.FrameRate, (param.FrameRate.Numerator << 32) | param.FrameRate.Denominator);
                }
                mtVideo.Set(MediaTypeAttributeKeys.InterlaceMode, (int)VideoInterlaceMode.MixedInterlaceOrProgressive);

                StreamDescriptor sdescVideo;
                MediaFactory.CreateStreamDescriptor(id, 1, new[] { mtVideo }, out sdescVideo);
                sdescVideo.MediaTypeHandler.CurrentMediaType = mtVideo;

                return sdescVideo;
            }
        }

        private StreamDescriptor CreateAudioStreamDescriptor(int id) {
            var audioSpecificConfig = uint.Parse(_config.ParameterString, NumberStyles.HexNumber);
            var param = AudioParameterParser.Parse(audioSpecificConfig);

            using(var mtAudio = new MediaType()) {
                MediaFactory.CreateMediaType(mtAudio);
                mtAudio.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Audio);
                mtAudio.Set(MediaTypeAttributeKeys.Subtype, AudioFormatGuids.Aac);
                mtAudio.Set(MediaTypeAttributeKeys.AudioBitsPerSample, 16);             // 16bit Output
                mtAudio.Set(MediaTypeAttributeKeys.AacPayloadType, 0);                  // Raw AAC
                mtAudio.Set(MediaTypeAttributeKeys.AacAudioProfileLevelIndication, 0);  // Unknown
                mtAudio.Set(MediaTypeAttributeKeys.AudioSamplesPerSecond, param.Frequency);
                mtAudio.Set(MediaTypeAttributeKeys.AudioNumChannels, param.NumChannels);
                mtAudio.Set(MediaTypeAttributeKeys.AudioChannelMask, (int)param.ChannelMask);

                var userData = new byte[] {
                    0, 0,            // wPayloadType = 0(raw AAC)
                    0, 0,            // wAudioProfileLevelIndication = 0 (Unknown)
                    0, 0,            // wStructType = 0
                    0, 0,            // wReserved1
                    0, 0, 0, 0,      // dwReserved2
                    (byte)((audioSpecificConfig & 0xff00) >> 8), (byte)(audioSpecificConfig & 0xff) // AudioSpecificConfig
                };
                mtAudio.Set(MediaTypeAttributeKeys.UserData, userData);

                StreamDescriptor sdescAudio;
                MediaFactory.CreateStreamDescriptor(id, 1, new[] { mtAudio }, out sdescAudio);
                sdescAudio.MediaTypeHandler.CurrentMediaType = mtAudio;

                return sdescAudio;
            }
        }
    }
}
