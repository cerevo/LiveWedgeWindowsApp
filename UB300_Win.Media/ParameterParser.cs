using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cerevo.UB300_Win.Media {
    internal class BitParser {
        public const int BitsPerByte = 8;
        private readonly byte[] _buffer;
        private int _byteIndex;
        private int _bitIndex;

        public BitParser(byte[] buf) {
            if(buf == null) throw new ArgumentNullException(nameof(buf));
            if(buf.Length < 1) throw new ArgumentException(nameof(buf));
            _buffer = buf;
            _byteIndex = 0;
            _bitIndex = 0;
        }

        public bool ReadBit() {
            var value = ExtractBit(_buffer[_byteIndex], _bitIndex);
            Advance();
            return (value != 0);
        }

        public void SkipBit() {
            Advance();
        }

        public ulong ReadBits(int num) {
            if(num < 1) {
                return 0;
            }
            if(num > sizeof(ulong) * BitsPerByte) {
                throw new ArgumentOutOfRangeException(nameof(num));
            }

            var value = 0UL;
            for(var i = 0; i < num; i++) {
                value <<= 1;
                if(ReadBit()) {
                    value |= 1;
                }
            }
            return value;
        }

        public void SkipBits(int num) {
            for(var i = 0; i < num; i++) {
                Advance();
            }
        }

        public byte ReadByte() {
            if(_bitIndex != 0) {
                return (byte)ReadBits(BitsPerByte);
            }

            var value = _buffer[_byteIndex];
            _byteIndex++;
            return value;
        }

        public ulong ReadBytes(int num) {
            if(num < 1) {
                return 0;
            }
            if(num > sizeof(ulong)) {
                throw new ArgumentOutOfRangeException(nameof(num));
            }
            if(_bitIndex != 0) {
                return ReadBits(num * BitsPerByte);
            }

            var value = 0UL;
            for(var i = 0; i < num; i++) {
                value <<= 8;
                value |= _buffer[_byteIndex];
                _byteIndex++;
            }
            return value;
        }

        public void SkipByte() {
            _byteIndex++;
        }

        public void SkipBytes(int num) {
            _byteIndex += num;
        }

        public ulong ReadUExpGolomb() {
            // T-REC-H.264-201402 / 9.1 Parsing process for Exp-Golomb codes
            /*var leadingZeroBits = -1;
            for (var b = false; !b; leadingZeroBits++) {
                b = ReadBit();
            }
            return Math.Pow(2, leadingZeroBits) - 1 + ReadBits(leadingZeroBits);
            */
            var leadingZeroBits = 0;
            ulong codeStart = 1;

            while(!ReadBit()) {
                leadingZeroBits++;
                codeStart *= 2;
            }
            return codeStart - 1 + ReadBits(leadingZeroBits);
        }

        public long ReadSExpGolomb() {
            // T-REC-H.264-201402 / 9.1.1 Mapping process for signed Exp-Golomb codes
            var uvalue = ReadUExpGolomb();
            if((uvalue & 1UL) != 0) {
                return (long)((uvalue + 1) >> 1);
            }
            return (long)((uvalue + 1) >> 1) * -1;
        }

        private static int ExtractBit(byte data, int position) {
            switch(position) {
                case 0:
                    return (data & 0x80) >> 7;
                case 1:
                    return (data & 0x40) >> 6;
                case 2:
                    return (data & 0x20) >> 5;
                case 3:
                    return (data & 0x10) >> 4;
                case 4:
                    return (data & 0x08) >> 3;
                case 5:
                    return (data & 0x04) >> 2;
                case 6:
                    return (data & 0x02) >> 1;
                case 7:
                    return (data & 0x01);
                default:
                    throw new ArgumentOutOfRangeException(nameof(position));
            }
        }

        private void Advance() {
            if(_bitIndex < BitsPerByte - 1) {
                _bitIndex++;
            } else {
                _byteIndex++;
                _bitIndex = 0;
            }
        }
    }

    internal static class VideoParameterParser {
        public struct VideoParameter {
            public int H264Level;
            public int IdcProfile;
            public long FrameWidth;
            public long FrameHeight;
            public Ratio AspectRatio;
            public Ratio FrameRate;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static VideoParameter Parse(byte[] sequenceParameterSet, byte[] pictureParameterSet) {
            const byte Extended_SAR = 255;
            var frame_crop_left_offset = 0UL;
            var frame_crop_right_offset = 0UL;
            var frame_crop_top_offset = 0UL;
            var frame_crop_bottom_offset = 0UL;
            var sar = new Ratio(1, 1);   // sar_width, sar_height
            var num_units_in_tick = 0U;
            var time_scale = 0U;
            var fixed_frame_rate_flag = false;

            var sps = new BitParser(sequenceParameterSet);
            sps.SkipByte();  // NALU Header

            // T-REC-H.264-201402 / 7.3.2.1.1 Sequence parameter set data syntax
            var profile_idc = sps.ReadByte();
            var constraint_setN_flags = sps.ReadBits(6);
            sps.SkipBits(2); // reserved_zero_2bits
            var level_idc = sps.ReadByte();
            var seq_parameter_set_id = sps.ReadUExpGolomb();
            Debug.Assert(seq_parameter_set_id <= 31);
            if(profile_idc == 100 || profile_idc == 110 || profile_idc == 122 || profile_idc == 244 ||
                profile_idc == 44 || profile_idc == 83 || profile_idc == 86 || profile_idc == 118 ||
                profile_idc == 128) {
                var chroma_format_idc = sps.ReadUExpGolomb();
                if(chroma_format_idc == 3) {
                    var separate_colour_plane_flag = sps.ReadBit();
                }
                var bit_depth_luma_minus8 = sps.ReadUExpGolomb();
                var bit_depth_chroma_minus8 = sps.ReadUExpGolomb();
                var qpprime_y_zero_transform_bypass_flag = sps.ReadBit();
                var seq_scaling_matrix_present_flag = sps.ReadBit();
                if(seq_scaling_matrix_present_flag) {
                    for(var i = 0; i < ((chroma_format_idc != 3) ? 8 : 12); i++) {
                        var seq_scaling_list_present_flag = sps.ReadBit();
                        if(seq_scaling_list_present_flag) {
                            var sizeOfScalingList = i < 6 ? 16 : 64;
                            // 7.3.2.1.1.1 Scaling list syntax
                            ulong lastScale = 8;
                            ulong nextScale = 8;
                            for(var j = 0; j < sizeOfScalingList; j++) {
                                if(nextScale != 0) {
                                    var delta_scale = sps.ReadUExpGolomb();
                                    nextScale = (lastScale + delta_scale + 256) % 256;
                                    var useDefaultScalingMatrixFlag = (j == 0 && nextScale == 0);
                                }
                                var scalingList = (nextScale == 0) ? lastScale : nextScale;
                                lastScale = scalingList;
                            }
                        }
                    }
                }
            }
            var log2_max_frame_num_minus4 = sps.ReadUExpGolomb();
            Debug.Assert(log2_max_frame_num_minus4 <= 12);
            var pic_order_cnt_type = sps.ReadUExpGolomb();
            Debug.Assert(pic_order_cnt_type <= 2);
            if(pic_order_cnt_type == 0) {
                var log2_max_pic_order_cnt_lsb_minus4 = sps.ReadUExpGolomb();
                Debug.Assert(log2_max_pic_order_cnt_lsb_minus4 <= 12);
            } else if(pic_order_cnt_type == 1) {
                var delta_pic_order_always_zero_flag = sps.ReadBit();
                var offset_for_non_ref_pic = sps.ReadSExpGolomb();
                var offset_for_top_to_bottom_field = sps.ReadSExpGolomb();
                var num_ref_frames_in_pic_order_cnt_cycle = sps.ReadUExpGolomb();
                Debug.Assert(pic_order_cnt_type <= 255);
                for(ulong i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; ++i) {
                    var offset_for_ref_frame = sps.ReadSExpGolomb();
                }
            }
            var max_num_ref_frames = sps.ReadUExpGolomb();
            var gaps_in_frame_num_value_allowed_flag = sps.ReadBit();
            var pic_width_in_mbs_minus1 = sps.ReadUExpGolomb();
            var pic_height_in_map_units_minus1 = sps.ReadUExpGolomb();
            var frame_mbs_only_flag = sps.ReadBit();
            if(!frame_mbs_only_flag) {
                var mb_adaptive_frame_field_flag = sps.ReadBit();
            }
            var direct_8x8_inference_flag = sps.ReadBit();
            var frame_cropping_flag = sps.ReadBit();
            if(frame_cropping_flag) {
                frame_crop_left_offset = sps.ReadUExpGolomb();
                frame_crop_right_offset = sps.ReadUExpGolomb();
                frame_crop_top_offset = sps.ReadUExpGolomb();
                frame_crop_bottom_offset = sps.ReadUExpGolomb();
            }
            var vui_parameters_present_flag = sps.ReadBit();
            if(vui_parameters_present_flag) {
                // Annex E.1.1 VUI parameters syntax
                var aspect_ratio_info_present_flag = sps.ReadBit();
                if(aspect_ratio_info_present_flag) {
                    var aspect_ratio_idc = sps.ReadByte();
                    if(aspect_ratio_idc == Extended_SAR) {
                        sar = new Ratio((long)sps.ReadBytes(2), (long)sps.ReadBytes(2));
                    } else {
                        sar = GetAspectRatioFromIndicator(aspect_ratio_idc);
                    }
                }
                var overscan_info_present_flag = sps.ReadBit();
                if(overscan_info_present_flag) {
                    var overscan_appropriate_flag = sps.ReadBit();
                }
                var video_signal_type_present_flag = sps.ReadBit();
                if(video_signal_type_present_flag) {
                    var video_format = sps.ReadBits(3);
                    var video_full_range_flag = sps.ReadBit();
                    var colour_description_present_flag = sps.ReadBit();
                    if(colour_description_present_flag) {
                        var colour_primaries = sps.ReadByte();
                        var transfer_characteristics = sps.ReadByte();
                        var matrix_coefficients = sps.ReadByte();
                    }
                }
                var chroma_loc_info_present_flag = sps.ReadBit();
                if(chroma_loc_info_present_flag) {
                    var chroma_sample_loc_type_top_field = sps.ReadUExpGolomb();
                    var chroma_sample_loc_type_bottom_field = sps.ReadUExpGolomb();
                }
                var timing_info_present_flag = sps.ReadBit();
                if(timing_info_present_flag) {
                    num_units_in_tick = (uint)sps.ReadBytes(4);
                    time_scale = (uint)sps.ReadBytes(4);
                    fixed_frame_rate_flag = sps.ReadBit();
                }
                /*
                var nal_hrd_parameters_present_flag = sps.ReadBit();
                if(nal_hrd_parameters_present_flag) {
                    hrd_parameters();
                }
                var vcl_hrd_parameters_present_flag = sps.ReadBit();
                if(vcl_hrd_parameters_present_flag) {
                    hrd_parameters();
                }
                if(nal_hrd_parameters_present_flag || vcl_hrd_parameters_present_flag) {
                    var low_delay_hrd_flag = sps.ReadBit();
                }
                var pic_struct_present_flag = sps.ReadBit();
                var bitstream_restriction_flag = sps.ReadBit();
                if(vcl_hrd_parameters_present_flag) {
                    var motion_vectors_over_pic_boundaries_flag = sps.ReadBit();
                    var max_bytes_per_pic_denom = sps.ReadUExpGolomb();
                    var max_bits_per_mb_denom = sps.ReadUExpGolomb();
                    var log2_max_mv_length_horizontal = sps.ReadUExpGolomb();
                    var log2_max_mv_length_vertical = sps.ReadUExpGolomb();
                    var max_num_reorder_frames = sps.ReadUExpGolomb();
                    var max_dec_frame_buffering = sps.ReadUExpGolomb();
                }
                */
            }

            return new VideoParameter {
                H264Level = level_idc,
                IdcProfile = profile_idc,
                FrameWidth = (long)((pic_width_in_mbs_minus1 + 1) * 16 - frame_crop_right_offset * 2 - frame_crop_left_offset * 2),
                FrameHeight = (long)((frame_mbs_only_flag ? 1UL : 2UL) * (pic_height_in_map_units_minus1 + 1) * 16 - frame_crop_top_offset * 2 - frame_crop_bottom_offset * 2),
                AspectRatio = sar,
                FrameRate = fixed_frame_rate_flag ? new Ratio(time_scale, num_units_in_tick) : new Ratio(0, 0)
            };
        }

        private static Ratio GetAspectRatioFromIndicator(byte idc) {
            switch(idc) {
                case 1:
                    return new Ratio(1, 1);
                case 2:
                    return new Ratio(12, 11);
                case 3:
                    return new Ratio(10, 11);
                case 4:
                    return new Ratio(16, 11);
                case 5:
                    return new Ratio(40, 33);
                case 6:
                    return new Ratio(24, 11);
                case 7:
                    return new Ratio(20, 11);
                case 8:
                    return new Ratio(32, 11);
                case 9:
                    return new Ratio(80, 33);
                case 10:
                    return new Ratio(18, 11);
                case 11:
                    return new Ratio(15, 11);
                case 12:
                    return new Ratio(64, 33);
                case 13:
                    return new Ratio(160, 99);
                case 14:
                    return new Ratio(4, 3);
                case 15:
                    return new Ratio(3, 2);
                case 16:
                    return new Ratio(2, 1);
                default:
                    return new Ratio(1, 1);
            }
        }
    }

    internal static class AudioParameterParser {
        public struct AudioParameter {
            public int NumChannels;
            public int Frequency;
            public SpeakerPositions ChannelMask;
        }

        public static AudioParameter Parse(uint audioSpecificConfig) {
            var objectType = (audioSpecificConfig & 0xf800) >> 11;          // 5 bits: object type
            var frequencyIndex = (audioSpecificConfig & 0x0780) >> 7;       // 4 bits: frequency index
            var channelConfiguration = (audioSpecificConfig & 0x0078) >> 3; // 4 bits: channel configuration

            var config = new AudioParameter() {
                Frequency = GetFrequency(frequencyIndex)
            };

            // MPEG-4 Channel Configuration
            switch(channelConfiguration) {
                case 0:
                    break;
                case 1:
                    config.NumChannels = 1;
                    config.ChannelMask = SpeakerPositions.FrontCenter;
                    break;
                case 2:
                    config.NumChannels = 2;
                    config.ChannelMask = SpeakerPositions.Stereo;
                    break;
                case 3:
                    config.NumChannels = 3;
                    config.ChannelMask = SpeakerPositions.Stereo | SpeakerPositions.FrontCenter;
                    break;
                case 4:
                    config.NumChannels = 4;
                    config.ChannelMask = SpeakerPositions.Surround;
                    break;
                case 5:
                    config.NumChannels = 5;
                    config.ChannelMask = SpeakerPositions.Quad | SpeakerPositions.FrontCenter;
                    break;
                case 6:
                    config.NumChannels = 6;
                    config.ChannelMask = SpeakerPositions.FivePoint1;
                    break;
                case 7:
                    config.NumChannels = 8;
                    config.ChannelMask = SpeakerPositions.SevenPoint1Surround;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channelConfiguration));
            }
            return config;
        }

        // MPEG-4 Sampling Frequency Index
        private static int GetFrequency(uint frequencyIndex) {
            switch(frequencyIndex) {
                case 0:
                    return 96000;
                case 1:
                    return 88200;
                case 2:
                    return 64000;
                case 3:
                    return 48000;
                case 4:
                    return 44100;
                case 5:
                    return 32000;
                case 6:
                    return 24000;
                case 7:
                    return 22050;
                case 8:
                    return 16000;
                case 9:
                    return 12000;
                case 10:
                    return 11025;
                case 11:
                    return 8000;
                case 12:
                    return 7350;
                default:
                    throw new ArgumentOutOfRangeException(nameof(frequencyIndex));
            }
        }
    }
}
