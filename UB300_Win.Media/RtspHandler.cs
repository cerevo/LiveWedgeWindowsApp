using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Media.Common.Loggers;
using Media.Rtp;
using Media.Rtsp;
using Media.Sdp;

namespace Cerevo.UB300_Win.Media {
    public interface IRtspSample {
        byte[] Buffer { get; }
        long SampleTime { get; }
        bool IsKeyFrame { get; }    // for MFSampleExtension_CleanPoint
        bool Discontinuity { get; } // for MFSampleExtension_Discontinuity
        int[] LengthInfo { get; }   // for MF_NALU_LENGTH_INFORMATION
    }

    public class RtspHandler : IDisposable {
        private class SimpleSample : IRtspSample {
            public byte[] Buffer { get; }
            public long SampleTime { get; }
            public bool IsKeyFrame { get; }
            public bool Discontinuity { get; }
            public int[] LengthInfo { get; }

            public SimpleSample(long time, bool discontinuity, IEnumerable<byte> data) {
                SampleTime = time;
                IsKeyFrame = false;
                Discontinuity = discontinuity;
                Buffer = data.ToArray();
                LengthInfo = new[] { Buffer.Length };
            }
        }

        private class VideoSample : IRtspSample {
            public byte[] Buffer { get; private set; }
            public long SampleTime { get; }
            public bool IsKeyFrame { get; }
            public bool Discontinuity { get; }
            public int[] LengthInfo { get; private set; }

            private readonly List<List<byte>> _units;
            private List<byte> _lastUnit;

            public VideoSample(long time, bool discontinuity, bool keyframe) {
                SampleTime = time;
                IsKeyFrame = keyframe;
                Discontinuity = discontinuity;
                Buffer = null;
                _units = new List<List<byte>>();
                _lastUnit = null;
            }

            public void BeginUnit() {
                if(_lastUnit != null) {
                    EndUnit();
                }
                _lastUnit = new List<byte>(VideoStartCode);
            }

            public void AddUnitData(byte data) {
                if(_lastUnit == null) {
                    BeginUnit();
                }
                _lastUnit.Add(data);
            }

            public void AddUnitData(IEnumerable<byte> data) {
                if(_lastUnit == null) {
                    BeginUnit();
                }
                _lastUnit.AddRange(data);
            }

            public void EndUnit() {
                if(_lastUnit == null) {
                    return;
                }
                _units.Add(_lastUnit);
                _lastUnit = null;
            }

            public void Commit() {
                EndUnit();
                LengthInfo = _units.Select(f => f.Count).ToArray();
                Buffer = _units.SelectMany(f => f).ToArray();
                _units.Clear();
            }
        }

        private class StreamInfo {
            public Func<long, RtpPacket, bool, IRtspSample> Handler { get; }
            public Subject<IRtspSample> Subject { get; }
            public string Parameter { get; set; }
            public int ClockRate { get; set; }
            public ushort? LastSequenceNumber { get; set; }
            public uint? LastTimeStamp { get; set; }
            public long TotalTime { get; set; }

            public StreamInfo(Func<long, RtpPacket, bool, IRtspSample> handler) {
                Handler = handler;
                Subject = new Subject<IRtspSample>();
                Parameter = string.Empty;
                ClockRate = 1;
                Reset();
            }

            public void Reset() {
                LastSequenceNumber = null;
                LastTimeStamp = null;
                TotalTime = 0;
            }

            public long ToSampleTime(long offset) {
                const float mfTimeBase = 10000000.0f;   // 100-nanosecond units.
                return (long)(offset * mfTimeBase / ClockRate);
            }
        }

        public static readonly int DefaultPort = 554;
        public static readonly string UriSchemeRtsp = RtspMessage.ReliableTransportScheme;
        public static readonly string UriSchemeRtspTcp = RtspMessage.TcpTransportScheme;
        public static readonly string UriSchemeRtspUdp = RtspMessage.UnreliableTransportScheme;

        private static readonly byte[] VideoStartCode = new byte[] { 0x00, 0x00, 0x00, 0x01 };
        private const string VideoParameterIdentifier = "sprop-parameter-sets=";
        private const string AudioParameterIdentifier = "config=";

        private bool _disposed = false;
        private readonly RtspClient _rtspClient;
        private readonly StreamInfo _videoStreamInfo;
        private readonly StreamInfo _audioStreamInfo;
        private readonly BehaviorSubject<bool> _playStarted;
        private VideoSample _lastVideoSample;

        public RtspHandler(Uri uri) {
            _rtspClient = new RtspClient(uri, RtspClient.ClientProtocolType.Udp) {
                AutomaticallyReconnect = true,
                /*ConfigureSocket = (s) => {
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }*/
            };
            _videoStreamInfo = new StreamInfo(OnVideoPacket);
            _audioStreamInfo = new StreamInfo(OnAudioPacket);
            _playStarted = new BehaviorSubject<bool>(false);
            _lastVideoSample = null;
#if DEBUG
            _rtspClient.Logger = new DebuggingLogger();
            _rtspClient.OnConnect += (s, o) => Trace.WriteLine("RtspClient::OnConnect()");
            _rtspClient.OnDisconnect += (s, o) => Trace.WriteLine("RtspClient::OnDisconnect()");
            _rtspClient.OnPause += (s, o) => Trace.WriteLine("RtspClient::OnPause()");
            _rtspClient.OnPlay += (s, o) => Trace.WriteLine("RtspClient::OnPlay()");
            _rtspClient.OnRequest += (s, req) => Trace.WriteLine("RtspClient::OnRequest(): " + req.HeaderEncoding.GetString(req.PrepareStatusLine(false).ToArray()));
            _rtspClient.OnResponse += (s, req, res) => Trace.WriteLine($"RtspClient::OnResponse(): {req.MethodString} => {res?.StatusCode} {res?.HttpStatusCode}\n{res?.Body}");
            _rtspClient.OnStop += (s, a) => Trace.WriteLine("RtspClient::OnStop()");
#endif
        }

        public RtspHandler(string uri) : this(new Uri(uri)) { }

        ~RtspHandler() {
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
                Stop();
                Disconnect();
                _playStarted.Dispose();
                _videoStreamInfo.Subject.Dispose();
                _audioStreamInfo.Subject.Dispose();
                _rtspClient.Dispose();
            }

            // Dispose unmanaged resources.

            // Set disposed flag.
            _disposed = true;
        }

        public Uri Uri => _rtspClient.CurrentLocation;
        public bool IsConnected => _rtspClient.IsConnected;
        public bool IsPlaying => _rtspClient.IsPlaying;
        public IObservable<bool> PlayStarted => _playStarted;
        public IObservable<IRtspSample> VideoStream => _videoStreamInfo.Subject;
        public IObservable<IRtspSample> AudioStream => _audioStreamInfo.Subject;
        public string VideoParameter => _videoStreamInfo.Parameter;
        public string AudioParameter => _audioStreamInfo.Parameter;

        public void Connect() {
            Trace.WriteLine("RtspHandler::Connect()");
            _rtspClient.Connect();
            _rtspClient.SendOptions().Dispose();
            _rtspClient.SendDescribe().Dispose();

            var videoDesc = _rtspClient.SessionDescription.MediaDescriptions.First(d => d.MediaType == MediaType.video);
            var audioDesc = _rtspClient.SessionDescription.MediaDescriptions.First(d => d.MediaType == MediaType.audio);
            // a=fmtp:97 packetization-mode=1;profile-level-id=64001F;sprop-parameter-sets=Z2QAH62EBUViuKxUdCAqKxXFYqOhAVFYrisVHQgKisVxWKjoQFRWK4rFR0ICorFcVio6ECSFITk8nyfk/k/J8nm5s00IEkKQnJ5Pk/J/J+T5PNzZprQCgC3I,aO48sA==
            _videoStreamInfo.Parameter = videoDesc.FmtpLine.Parts.ElementAt(1).Split(';').First(l => l.StartsWith(VideoParameterIdentifier)).Substring(VideoParameterIdentifier.Length);
            // a=fmtp:96 streamtype=5;profile-level-id=1;mode=AAC-hbr;sizelength=13;indexlength=3;indexdeltalength=3;config=1190
            _audioStreamInfo.Parameter = audioDesc.FmtpLine.Parts.ElementAt(1).Split(';').First(l => l.StartsWith(AudioParameterIdentifier)).Substring(AudioParameterIdentifier.Length);
            // a=rtpmap:97 H264/90000
            _videoStreamInfo.ClockRate = int.Parse(videoDesc.RtpMapLine.Parts.ElementAt(1).Split('/')[1]);
            // a=rtpmap:96 MPEG4-GENERIC/48000/2
            _audioStreamInfo.ClockRate = int.Parse(audioDesc.RtpMapLine.Parts.ElementAt(1).Split('/')[1]);
            Trace.WriteLine($"RtspHandler::Connect(): Video ClockRate={_videoStreamInfo.ClockRate} / Config={_videoStreamInfo.Parameter}");
            Trace.WriteLine($"RtspHandler::Connect(): Audio ClockRate={_audioStreamInfo.ClockRate} / Config={_audioStreamInfo.Parameter}");

            _rtspClient.Client.Logger = _rtspClient.Logger;
            _rtspClient.Client.HandleFrameChanges = false;
            _rtspClient.Client.RtpPacketReceieved += OnRtpPacketReceieved;
        }

        public void Disconnect() {
            Trace.WriteLine("RtspHandler::Disconnect()");
            _rtspClient.Client.RtpPacketReceieved -= OnRtpPacketReceieved;
            _rtspClient.Disconnect();
        }

        public void Play() {
            Trace.WriteLine("RtspHandler::Play()");
            _playStarted.OnNext(false);
            if(!IsConnected) {
                Connect();
            }
            _videoStreamInfo.Reset();
            _audioStreamInfo.Reset();
            _rtspClient.StartPlaying();
            //_rtspClient.Client.ThreadEvents = true;
        }

        public void Stop() {
            Trace.WriteLine("RtspHandler::Stop()");
            //_rtspClient.Client.ThreadEvents = false;
            _rtspClient.StopPlaying();
        }

        private void OnRtpPacketReceieved(object sender, RtpPacket packet, RtpClient.TransportContext tc) {
            using(var packet2 = packet.Clone(true, true, true, true, false)) {
                if(tc == null || tc.RemoteSynchronizationSourceIdentifier != packet2.SynchronizationSourceIdentifier) {
                    return;
                }
                //Trace.WriteLine($"RtspHandler::OnRtpPacketReceieved() SSID={(uint)packet.SynchronizationSourceIdentifier} Timestamp={(uint)packet.Timestamp}");

                StreamInfo info;
                switch(tc.MediaDescription.MediaType) {
                    case MediaType.video:
                        info = _videoStreamInfo;
                        break;
                    case MediaType.audio:
                        info = _audioStreamInfo;
                        break;
                    default:
                        return;
                }

                var discontinuity = false;
                var uintTimestamp = (uint)packet2.Timestamp;
                // Check Sequence number
                if(!info.LastSequenceNumber.HasValue) {
                    info.LastSequenceNumber = (ushort)packet2.SequenceNumber;
                    discontinuity = true;
                } else if(info.LastSequenceNumber.Value < ushort.MaxValue) {
                    if(packet2.SequenceNumber == info.LastSequenceNumber.Value + 1) {
                        // OK
                        discontinuity = false;
                    } else if(packet2.SequenceNumber > info.LastSequenceNumber.Value) {
                        // Skipped
                        Debug.WriteLine(
                            $"RtspHandler::OnRtpPacketReceieved<{tc.MediaDescription.MediaType}>(): Sequence number error(Skipped) {info.LastSequenceNumber}=>{packet2.SequenceNumber}");
                        discontinuity = true;
                        Debug.Assert(packet2.SequenceNumber != 21328);
                    } else {
                        // Rollbacked
                        Debug.WriteLine(
                            $"RtspHandler::OnRtpPacketReceieved<{tc.MediaDescription.MediaType}>(): Sequence number error(Rollbacked) {info.LastSequenceNumber}=>{packet2.SequenceNumber}");
                        Debug.Assert(packet2.SequenceNumber != 21328);
                        return; // Discard
                    }
                } else {
                    Debug.Assert(info.LastTimeStamp.HasValue);
                    // wrapped
                    if(packet2.SequenceNumber == 0) {
                        // OK
                        discontinuity = false;
                    } else if(uintTimestamp >= info.LastTimeStamp.Value) {
                        // Skipped
                        Debug.WriteLine(
                            $"RtspHandler::OnRtpPacketReceieved<{tc.MediaDescription.MediaType}>(): Sequence number error(Skipped) {info.LastSequenceNumber}=>{packet2.SequenceNumber}");
                        discontinuity = true;
                        Debug.Assert(packet2.SequenceNumber != 21328);
                    } else {
                        // Rollbacked
                        Debug.WriteLine(
                            $"RtspHandler::OnRtpPacketReceieved<{tc.MediaDescription.MediaType}>(): Sequence number error(Rollbacked) {info.LastSequenceNumber}=>{packet2.SequenceNumber}");
                        Debug.Assert(packet2.SequenceNumber != 21328);
                        return; // Discard
                    }
                }
                info.LastSequenceNumber = (ushort)packet2.SequenceNumber;

                // Check Timestamp
                if(!info.LastTimeStamp.HasValue) {
                    info.TotalTime = 0;
                } else if(uintTimestamp >= info.LastTimeStamp.Value) {
                    info.TotalTime += (uintTimestamp - info.LastTimeStamp.Value);
                } else {
                    // wrapped
                    info.TotalTime += (uintTimestamp + (uint.MaxValue - info.LastTimeStamp.Value));
                }
                info.LastTimeStamp = uintTimestamp;

                var sample = info.Handler(info.ToSampleTime(info.TotalTime), packet2, discontinuity);
                if(sample == null) return;

                info.Subject.OnNext(sample);
                if(!_playStarted.Value) {
                    Debug.WriteLine("RtspHandler::OnRtpPacketReceieved(): Play started.");
                    _playStarted.OnNext(true);
                }
            }
        }

        private void AddVideoParamaterSet(VideoSample sample) {
            var configpart = VideoParameter.Split(',');
            // Add SPS
            sample.BeginUnit();
            sample.AddUnitData(Convert.FromBase64String(configpart[0]));
            sample.EndUnit();
            // Add PPS
            sample.BeginUnit();
            sample.AddUnitData(Convert.FromBase64String(configpart[1]));
            sample.EndUnit();
        }

        private IRtspSample OnVideoPacket(long sampleTime, RtpPacket packet, bool discontinuity) {
            if(discontinuity) {
                // clear fragmented NAL unit
                _lastVideoSample = null;
            }

            var data = packet.PayloadData.ToArray();
            if(data.Length < 1) {
                return null;
            }

            // Interleaved mode is not supported.
            var nalUnitFlags = data[0] & 0xe0;
            var nalUnitType = data[0] & 0x1f;

            if(nalUnitType <= 23) {
                // NAL unit (one packet, one NAL)
                //Trace.WriteLine($"RtspHandler::OnVideoPacket(): NALType({nalUnitType}) Timestamp={packet.Timestamp} Length={data.Length}");
                var sample = new VideoSample(sampleTime, discontinuity, (nalUnitType == 5));
                if(sampleTime == 0) {
                    AddVideoParamaterSet(sample);
                }
                sample.BeginUnit();
                sample.AddUnitData(data);
                sample.EndUnit();
                sample.Commit();
                return sample;
            }
            if(nalUnitType == 24) {
                // STAP-A (Single-time aggregation packet: one packet, multiple NALs)
                // TBD
                Debug.WriteLine("RtspHandler::OnVideoPacket(): STAP-A");
                return null;
            }
            if(nalUnitType == 28) {
                // FU-A (Fragmentation Units: multiple packets, one NAL)
                //var fuIndicator = data[0];
                var fuHeader = data[1];
                if((fuHeader & 0x80) != 0) { // check start bit
                    // start of a fragmented NAL unit
                    var fuNalUnitType = fuHeader & 0x1f;
                    //Trace.WriteLine($"RtspHandler::OnVideoPacket(): FU-A Start NALType({fuNalUnitType}) Timestamp={packet.Timestamp} Length={data.Length - 2}");
                    if(_lastVideoSample != null) {
                        Debug.WriteLine("RtspHandler::OnVideoPacket(): Invalid fragmented NAL start.");
                    }
                    _lastVideoSample = new VideoSample(sampleTime, discontinuity, (fuNalUnitType == 5));
                    if(sampleTime == 0) {
                        AddVideoParamaterSet(_lastVideoSample);
                    }
                    _lastVideoSample.BeginUnit();
                    _lastVideoSample.AddUnitData((byte)(nalUnitFlags | fuNalUnitType));
                    _lastVideoSample.AddUnitData(data.Skip(2));
                    return null;
                }
                if(discontinuity) {
                    Debug.WriteLine("RtspHandler::OnVideoPacket(): Ignore middle-started fragmented NAL.");
                    return null;
                }
                if((fuHeader & 0x40) != 0) { // check end bit
                    // end of a fragmented NAL unit
                    //Trace.WriteLine($"RtspHandler::OnVideoPacket(): FU-A End Timestamp={packet.Timestamp} Length={data.Length - 2}");
                    if(_lastVideoSample != null) {
                        _lastVideoSample.AddUnitData(data.Skip(2));
                        _lastVideoSample.EndUnit();
                        _lastVideoSample.Commit();
                        var sample = _lastVideoSample;
                        _lastVideoSample = null;
                        return sample;
                    } else {
                        Debug.WriteLine("RtspHandler::OnVideoPacket(): Invalid fragmented NAL end.");
                        return null;
                    }
                }
                // following FU payload
                //Trace.WriteLine($"RtspHandler::OnVideoPacket(): FU-A Continue Timestamp={packet.Timestamp} Length={data.Length - 2}");
                if(_lastVideoSample != null) {
                    _lastVideoSample.AddUnitData(data.Skip(2));
                } else {
                    Debug.WriteLine("RtspHandler::OnVideoPacket(): Invalid fragmented NAL.");
                }
                return null;
            }

            Debug.WriteLine($"RtspHandler::OnVideoPacket(): Unknown NALType({nalUnitType})");
            return null;
        }

        private IRtspSample OnAudioPacket(long sampleTime, RtpPacket packet, bool discontinuity) {
            var data = packet.PayloadData.ToArray();
            if(data.Length < 1) {
                return null;
            }
            //Trace.WriteLine($"RtspHandler::OnAudioPacket(): Timestamp={packet.Timestamp} Length={data.Length}");

            var auHeadersLength = (data[0] << 8) | data[1];
            //Debug.Assert(auHeadersLength == 16);
            if(auHeadersLength != 16) return null;
            var auSize = ((data[2] << 8) | data[3]) >> 3;
            return new SimpleSample(sampleTime, discontinuity, data.Skip(4).Take(auSize));
        }

        /*private static void BuildAdtsHeader(IList<byte> header, uint config, int packetLen) {
            Debug.Assert(header != null && header.Count == 7);
            // http://wiki.multimedia.cx/index.php?title=ADTS
            var objectType = (config & 0xf800) >> 11;          // 5 bits: object type
            var frequencyIndex = (config & 0x0780) >> 7;       // 4 bits: frequency index
            var channelConfiguration = (config & 0x0078) >> 3; // 4 bits: channel configuration
            var aacFrameLength = packetLen + 7;

            header[0] = (byte)0xFF; //Sync
            header[1] = (byte)0xF1; //Sync + MPEG-4 + protection absent
            header[2] = (byte)(((objectType - 1) << 6) + (frequencyIndex << 2) + (channelConfiguration >> 2));
            header[3] = (byte)(((channelConfiguration & 0x3) << 6) + (aacFrameLength >> 11));
            header[4] = (byte)((aacFrameLength & 0x7FF) >> 3);
            header[5] = (byte)(((aacFrameLength & 7) << 5) + 0x1F);
            header[6] = (byte)0xFC;
        }*/
    }
}
