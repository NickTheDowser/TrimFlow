namespace TrimFlow
{
    /// <summary>
    /// Parameters for video processing operations.
    /// Boss wanted this simple and clean, so here it is!
    /// </summary>
    public class VideoProcessingParameters
    {
        /// <summary>
        /// Input video file path
        /// </summary>
        public string InputFile { get; set; } = string.Empty;

        /// <summary>
        /// Output video file path (the trimmed masterpiece!)
        /// </summary>
        public string OutputFile { get; set; } = string.Empty;

        /// <summary>
        /// Silence detection threshold in decibels (default: -30dB)
        /// Lower values = more sensitive to quiet sounds
        /// </summary>
        public double SilenceThreshold { get; set; } = -30;

        /// <summary>
        /// Minimum silence duration in seconds (default: 0.5s)
        /// Silences shorter than this will be kept in the video
        /// </summary>
        public double SilenceDuration { get; set; } = 0.5;

        /// <summary>
        /// Enable batch processing mode for multiple files
        /// </summary>
        public bool IsBatchMode { get; set; } = false;

        /// <summary>
        /// Array of input files for batch processing mode
        /// </summary>
        public string[]? InputFiles { get; set; }
    }
}