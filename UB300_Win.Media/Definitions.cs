using System;

namespace Cerevo.UB300_Win.Media {
    // Numerator : Denominator
    public struct Ratio {
        public long Numerator;
        public long Denominator;

        public Ratio(long num, long den) {
            Numerator = num;
            Denominator = den;
        }

        public bool IsValid() => (Denominator != 0);
    }

    // <x3daudio.h>
    [Flags]
    internal enum SpeakerPositions : uint {
        FrontLeft = 0x00000001,
        FrontRight = 0x00000002,
        FrontCenter = 0x00000004,
        LowFrequency = 0x00000008,
        BackLeft = 0x00000010,
        BackRight = 0x00000020,
        FrontLeftOfCenter = 0x00000040,
        FrontRightOfCenter = 0x00000080,
        BackCenter = 0x00000100,
        SideLeft = 0x00000200,
        SideRight = 0x00000400,
        TopCenter = 0x00000800,
        TopFrontLeft = 0x00001000,
        TopFrontCenter = 0x00002000,
        TopFrontRight = 0x00004000,
        TopBackLeft = 0x00008000,
        TopBackCenter = 0x00010000,
        TopBackRight = 0x00020000,
        Reserved = 0x7FFC0000,      // bit mask locations reserved for future use
        All = 0x80000000,           // used to specify that any possible permutation of speaker configurations

        Mono = FrontCenter,
        Stereo = (FrontLeft | FrontRight),
        TwoPoint1 = (FrontLeft | FrontRight | LowFrequency),
        Surround = (FrontLeft | FrontRight | FrontCenter | BackCenter),
        Quad = (FrontLeft | FrontRight | BackLeft | BackRight),
        FourPoint1 = (FrontLeft | FrontRight | LowFrequency | BackLeft | BackRight),
        FivePoint1 = (FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight),
        SevenPoint1 = (FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | FrontLeftOfCenter | FrontRightOfCenter),
        FivePoint1Surround = (FrontLeft | FrontRight | FrontCenter | LowFrequency | SideLeft | SideRight),
        SevenPoint1Surround = (FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | SideLeft | SideRight)
    }

    // <evr.h>
    internal static class CustomServiceKeys {
        // MR_VIDEO_RENDER_SERVICE
        public static readonly Guid VideoRender = new Guid("1092a86c-ab1a-459a-a336-831fbc4d11ff");
    }
}
