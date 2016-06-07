using SharpDX.MediaFoundation;

namespace Cerevo.UB300_Win.Media {
    public static class MediaUtils {
        /// <summary>
        /// Initialize MediaFoundation
        /// </summary>
        public static void MediaFoundationStartup() {
            MediaManager.Startup();
        }

        /// <summary>
        /// Terminate MediaFoundation
        /// </summary>
        public static void MediaFoundationShutdown() {
            MediaManager.Shutdown();
        }
    }
}
