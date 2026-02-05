using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace TrimFlow
{
    /// <summary>
    /// Video processing manager using FFmpeg via Xabe.FFmpeg wrapper.
    /// Boss reported stalling at 50% - now with proper process management!
    /// </summary>
    public class FFmpegProcessor
    {
        private static bool _ffmpegInitialized = false;
        private static readonly string FfmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<LogEventArgs>? LogReceived;
        public event EventHandler<ProcessCompletedEventArgs>? ProcessCompleted;

        /// <summary>
        /// Initialize FFmpeg binaries (download if necessary)
        /// </summary>
        public static async Task InitializeFFmpegAsync()
        {
            if (_ffmpegInitialized)
            {
                return;
            }

            try
            {
                // Check if FFmpeg is bundled with the app first
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var bundledFfmpegPath = Path.Combine(appPath, "ffmpeg");
                var bundledFfmpegExe = Path.Combine(bundledFfmpegPath, "ffmpeg.exe");

                string targetPath;

                if (File.Exists(bundledFfmpegExe))
                {
                    // FFmpeg is bundled - copy to AppData for updates
                    targetPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "TrimFlow",
                        "ffmpeg"
                    );
                    
                    Directory.CreateDirectory(targetPath);
                    
                    // Copy bundled files if not already present
                    var targetFfmpegExe = Path.Combine(targetPath, "ffmpeg.exe");
                    if (!File.Exists(targetFfmpegExe))
                    {
                        File.Copy(bundledFfmpegExe, targetFfmpegExe, true);
                        File.Copy(
                            Path.Combine(bundledFfmpegPath, "ffprobe.exe"),
                            Path.Combine(targetPath, "ffprobe.exe"),
                            true
                        );
                    }
                }
                else
                {
                    // FFmpeg not bundled - download to AppData
                    targetPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "TrimFlow",
                        "ffmpeg"
                    );
                    
                    Directory.CreateDirectory(targetPath);
                    
                    var ffmpegExe = Path.Combine(targetPath, "ffmpeg.exe");
                    if (!File.Exists(ffmpegExe))
                    {
                        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, targetPath);
                    }
                }

                FFmpeg.SetExecutablesPath(targetPath);
                _ffmpegInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize FFmpeg: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Process one or multiple video files to remove silences
        /// </summary>
        public async Task ProcessVideosAsync(VideoProcessingParameters parameters, CancellationToken cancellationToken = default)
        {
            if (!_ffmpegInitialized)
            {
                await InitializeFFmpegAsync();
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                if (parameters.IsBatchMode && parameters.InputFiles != null)
                {
                    await ProcessBatchAsync(parameters, _cancellationTokenSource.Token);
                }
                else
                {
                    await ProcessSingleFileAsync(parameters, _cancellationTokenSource.Token);
                }

                OnProcessCompleted(new ProcessCompletedEventArgs(true, "Processing completed successfully"));
            }
            catch (OperationCanceledException)
            {
                OnLogReceived("Processing cancelled by user");
                OnProcessCompleted(new ProcessCompletedEventArgs(false, "Processing cancelled"));
                throw;
            }
            catch (Exception ex)
            {
                OnLogReceived($"ERROR: {ex.Message}");
                OnProcessCompleted(new ProcessCompletedEventArgs(false, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Process a single video file
        /// </summary>
        private async Task ProcessSingleFileAsync(VideoProcessingParameters parameters, CancellationToken cancellationToken)
        {
            OnLogReceived($"Processing: {Path.GetFileName(parameters.InputFile)}");
            OnProgressChanged(0, "Analyzing file...");

            if (!File.Exists(parameters.InputFile))
            {
                throw new FileNotFoundException($"Input file not found: {parameters.InputFile}");
            }

            OnProgressChanged(10, "Detecting silences...");
            var silenceIntervals = await DetectSilencesAsync(parameters, cancellationToken);
            OnLogReceived($"Number of silences detected: {silenceIntervals.Count}");

            if (silenceIntervals.Count == 0)
            {
                OnLogReceived("No silence detected. Copying file without modification.");
                File.Copy(parameters.InputFile, parameters.OutputFile, true);
                OnProgressChanged(100, "Completed (no silence detected)");
                return;
            }

            OnProgressChanged(50, "Removing silences...");
            await TrimSilencesAsync(parameters, silenceIntervals, cancellationToken);

            OnProgressChanged(100, "Processing completed");
        }

        /// <summary>
        /// Process multiple files in batch
        /// </summary>
        private async Task ProcessBatchAsync(VideoProcessingParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters.InputFiles == null || parameters.InputFiles.Length == 0)
            {
                throw new ArgumentException("No files to process");
            }

            OnLogReceived($"Batch mode: {parameters.InputFiles.Length} file(s)");

            for (int i = 0; i < parameters.InputFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inputFile = parameters.InputFiles[i];
                var directory = Path.GetDirectoryName(inputFile) ?? string.Empty;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
                var extension = Path.GetExtension(inputFile);
                var outputFile = Path.Combine(directory, $"{fileNameWithoutExtension}_trimmed{extension}");

                var singleParams = new VideoProcessingParameters
                {
                    InputFile = inputFile,
                    OutputFile = outputFile,
                    SilenceThreshold = parameters.SilenceThreshold,
                    SilenceDuration = parameters.SilenceDuration
                };

                OnLogReceived($"[{i + 1}/{parameters.InputFiles.Length}] Processing {Path.GetFileName(inputFile)}");

                await ProcessSingleFileAsync(singleParams, cancellationToken);

                var overallProgress = ((i + 1) * 100) / parameters.InputFiles.Length;
                OnProgressChanged(overallProgress, $"File {i + 1}/{parameters.InputFiles.Length} completed");
            }

            OnLogReceived("Batch processing completed");
        }

        /// <summary>
        /// Detect silence intervals using FFmpeg silencedetect filter.
        /// Boss reported DB threshold issues - now using proper stderr capture!
        /// </summary>
        private async Task<List<SilenceInterval>> DetectSilencesAsync(VideoProcessingParameters parameters, CancellationToken cancellationToken)
        {
            var silenceIntervals = new List<SilenceInterval>();
            var stderrOutput = new StringBuilder();

            OnLogReceived($"Detecting silences with: {parameters.SilenceThreshold}dB, {parameters.SilenceDuration}s");

            var ffmpegExe = Path.Combine(FfmpegPath, "ffmpeg.exe");
            if (!File.Exists(ffmpegExe))
            {
                ffmpegExe = Path.Combine(FfmpegPath, "ffmpeg");
            }

            // Build FFmpeg command - silencedetect outputs to stderr!
            var arguments = $"-i \"{parameters.InputFile}\" " +
                           $"-af silencedetect=noise={parameters.SilenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}dB:d={parameters.SilenceDuration.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                           $"-f null -";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegExe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stderrOutput.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync(cancellationToken);

            // Parse stderr output for silence intervals
            var output = stderrOutput.ToString();
            OnLogReceived($"FFmpeg detection output length: {output.Length} chars");

            var lines = output.Split('\n');
            double? silenceStart = null;

            foreach (var line in lines)
            {
                if (line.Contains("silence_start:"))
                {
                    var match = Regex.Match(line, @"silence_start:\s*([\d.]+)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var start))
                    {
                        silenceStart = start;
                        OnLogReceived($"Silence detected starting at: {start:F2}s");
                    }
                }
                else if (line.Contains("silence_end:") && silenceStart.HasValue)
                {
                    var match = Regex.Match(line, @"silence_end:\s*([\d.]+)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var end))
                    {
                        var duration = end - silenceStart.Value;
                        silenceIntervals.Add(new SilenceInterval
                        {
                            Start = silenceStart.Value,
                            End = end,
                            Duration = duration
                        });

                        OnLogReceived($"Silence confirmed: {silenceStart.Value:F2}s to {end:F2}s (duration: {duration:F2}s)");
                        silenceStart = null;
                    }
                }
            }

            return silenceIntervals;
        }

        /// <summary>
        /// Remove detected silences by extracting and concatenating non-silent segments.
        /// Fixed Boss's stalling issue: proper process disposal and -ss positioning!
        /// </summary>
        private async Task TrimSilencesAsync(VideoProcessingParameters parameters, List<SilenceInterval> silences, CancellationToken cancellationToken)
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(parameters.InputFile, cancellationToken);
            var duration = mediaInfo.Duration.TotalSeconds;

            var keepSegments = BuildKeepSegments(silences, duration);

            if (keepSegments.Count == 0)
            {
                throw new InvalidOperationException("No segments to keep after removing silences");
            }

            OnLogReceived($"Creating {keepSegments.Count} segment(s) to keep");

            var tempFolder = Path.Combine(Path.GetTempPath(), "TrimFlow_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            var ffmpegExe = Path.Combine(FfmpegPath, "ffmpeg.exe");
            if (!File.Exists(ffmpegExe))
            {
                ffmpegExe = Path.Combine(FfmpegPath, "ffmpeg");
            }

            try
            {
                var segmentFiles = new List<string>();

                // Extract each segment with proper process management
                for (int i = 0; i < keepSegments.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var segment = keepSegments[i];
                    var segmentFile = Path.Combine(tempFolder, $"segment_{i:D3}.mp4");

                    OnLogReceived($"Extracting segment {i + 1}/{keepSegments.Count}: {segment.Start:F2}s to {segment.End:F2}s");

                    var startSeconds = segment.Start.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                    var durationSeconds = (segment.End - segment.Start).ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

                    // CRITICAL: -ss BEFORE -i for fast and accurate seeking!
                    var arguments = $"-ss {startSeconds} -i \"{parameters.InputFile}\" -t {durationSeconds} " +
                                   $"-c:v libx264 -preset ultrafast -crf 23 -c:a aac -b:a 192k " +
                                   $"-avoid_negative_ts make_zero -y \"{segmentFile}\"";

                    OnLogReceived($"FFmpeg command: {arguments}");

                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegExe,
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    var errorOutput = new StringBuilder();
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorOutput.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    // Wait with timeout (30 seconds per segment should be plenty)
                    var completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(30), cancellationToken);

                    if (!completed)
                    {
                        OnLogReceived($"Segment {i + 1} extraction timed out, killing process...");
                        try
                        {
                            process.Kill(true);
                        }
                        catch { }
                        throw new TimeoutException($"Segment {i + 1} extraction timed out");
                    }

                    if (process.ExitCode != 0)
                    {
                        OnLogReceived($"FFmpeg error output: {errorOutput}");
                        throw new InvalidOperationException($"Failed to extract segment {i + 1}, exit code: {process.ExitCode}");
                    }

                    // Verify segment file was created and has content
                    if (!File.Exists(segmentFile) || new FileInfo(segmentFile).Length == 0)
                    {
                        throw new InvalidOperationException($"Segment {i + 1} file is empty or missing");
                    }

                    segmentFiles.Add(segmentFile);
                    var segmentSize = new FileInfo(segmentFile).Length / 1024;
                    OnLogReceived($"Segment {i + 1} extracted successfully ({segmentSize}KB)");

                    // Update progress during segment extraction
                    var segmentProgress = 50 + ((i + 1) * 40 / keepSegments.Count);
                    OnProgressChanged(segmentProgress, $"Extracting segments... {i + 1}/{keepSegments.Count}");
                }

                OnLogReceived("All segments extracted, starting concatenation...");
                OnProgressChanged(90, "Concatenating segments...");

                // Create concat file with relative paths
                var concatFile = Path.Combine(tempFolder, "concat.txt");
                var concatLines = segmentFiles.Select(f => $"file '{Path.GetFileName(f)}'");
                await File.WriteAllTextAsync(concatFile, string.Join(Environment.NewLine, concatLines), cancellationToken);

                OnLogReceived($"Concat file created with {segmentFiles.Count} entries");

                // Concatenate all segments
                var concatArguments = $"-f concat -safe 0 -i \"{Path.GetFileName(concatFile)}\" " +
                                     $"-c copy -y \"{parameters.OutputFile}\"";

                OnLogReceived($"Concatenation command: {concatArguments}");

                using var concatProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegExe,
                        Arguments = concatArguments,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        WorkingDirectory = tempFolder
                    }
                };

                var concatError = new StringBuilder();
                concatProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        concatError.AppendLine(e.Data);
                    }
                };

                concatProcess.Start();
                concatProcess.BeginErrorReadLine();
                concatProcess.BeginOutputReadLine();

                var concatCompleted = await concatProcess.WaitForExitAsync(TimeSpan.FromSeconds(60), cancellationToken);

                if (!concatCompleted)
                {
                    OnLogReceived("Concatenation timed out, killing process...");
                    try
                    {
                        concatProcess.Kill(true);
                    }
                    catch { }
                    throw new TimeoutException("Concatenation timed out");
                }

                if (concatProcess.ExitCode != 0)
                {
                    OnLogReceived($"Concatenation error output: {concatError}");
                    throw new InvalidOperationException($"Failed to concatenate segments, exit code: {concatProcess.ExitCode}");
                }

                // Verify output file
                if (!File.Exists(parameters.OutputFile) || new FileInfo(parameters.OutputFile).Length == 0)
                {
                    throw new InvalidOperationException("Output file is empty or missing");
                }

                var outputSize = new FileInfo(parameters.OutputFile).Length / (1024 * 1024);
                OnLogReceived($"File created successfully: {parameters.OutputFile} ({outputSize}MB)");
            }
            finally
            {
                // Cleanup temporary files - ensure all handles are closed
                await Task.Delay(500); // Small delay to ensure file handles are released

                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        // Try to delete, but don't fail if we can't
                        var deleted = TryDeleteDirectory(tempFolder, maxAttempts: 3);
                        if (deleted)
                        {
                            OnLogReceived("Temporary files cleaned up");
                        }
                        else
                        {
                            OnLogReceived($"Warning: Some temporary files remain in {tempFolder}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLogReceived($"Warning: Unable to delete temporary files: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Try to delete a directory with retries.
        /// Boss's files were locked, so we need to be patient!
        /// </summary>
        private bool TryDeleteDirectory(string path, int maxAttempts = 3)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return true;
                }
                catch
                {
                    if (i < maxAttempts - 1)
                    {
                        Thread.Sleep(500); // Wait a bit before retrying
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Build segments to keep (inverse of silences).
        /// Fixed Boss's blank-at-end issue: don't include trailing silence!
        /// </summary>
        private List<TimeSegment> BuildKeepSegments(List<SilenceInterval> silences, double totalDuration)
        {
            var keepSegments = new List<TimeSegment>();
            double currentStart = 0;

            var sortedSilences = silences.OrderBy(s => s.Start).ToList();

            foreach (var silence in sortedSilences)
            {
                // Add segment before this silence if it exists
                if (silence.Start > currentStart + 0.1) // Minimum 0.1s segment
                {
                    keepSegments.Add(new TimeSegment
                    {
                        Start = currentStart,
                        End = silence.Start
                    });
                    OnLogReceived($"Keep segment: {currentStart:F2}s to {silence.Start:F2}s");
                }

                currentStart = silence.End;
            }

            // Add final segment only if there's meaningful content after last silence
            var remainingDuration = totalDuration - currentStart;
            if (remainingDuration > 0.5) // At least 0.5 seconds of content
            {
                keepSegments.Add(new TimeSegment
                {
                    Start = currentStart,
                    End = totalDuration
                });
                OnLogReceived($"Keep final segment: {currentStart:F2}s to {totalDuration:F2}s");
            }
            else
            {
                OnLogReceived($"Skipping trailing segment (only {remainingDuration:F2}s remaining)");
            }

            return keepSegments;
        }

        /// <summary>
        /// Cancel current processing
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            OnLogReceived("Cancellation requested");
        }

        /// <summary>
        /// Check if FFmpeg is available
        /// </summary>
        public static async Task<bool> IsFFmpegInstalledAsync()
        {
            try
            {
                await InitializeFFmpegAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void OnProgressChanged(int percentage, string status)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs(percentage, status));
        }

        protected virtual void OnLogReceived(string message)
        {
            LogReceived?.Invoke(this, new LogEventArgs(message));
        }

        protected virtual void OnProcessCompleted(ProcessCompletedEventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }
    }

    #region Event Args Classes

    public class ProgressEventArgs : EventArgs
    {
        public int Percentage { get; }
        public string Status { get; }

        public ProgressEventArgs(int percentage, string status)
        {
            Percentage = percentage;
            Status = status;
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public LogEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class ProcessCompletedEventArgs : EventArgs
    {
        public bool Success { get; }
        public string Message { get; }

        public ProcessCompletedEventArgs(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    #endregion

    #region Helper Classes

    public class SilenceInterval
    {
        public double Start { get; set; }
        public double End { get; set; }
        public double Duration { get; set; }
    }

    public class TimeSegment
    {
        public double Start { get; set; }
        public double End { get; set; }
    }

    #endregion
}

// Extension method for WaitForExitAsync with timeout
public static class ProcessExtensions
{
    public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}