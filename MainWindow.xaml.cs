using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace TrimFlow
{
    /// <summary>
    /// Main window interaction logic for TrimFlow
    /// This is where the magic happens, Boss!
    /// </summary>
    public partial class MainWindow : Window
    {
        private FFmpegProcessor? _processor;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isProcessing = false;
        private string? _selectedInputFile;
        private string? _selectedOutputFile;
        private string[]? _selectedInputFiles;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEventHandlers();
            CheckFFmpegInstallation();
        }

        /// <summary>
        /// Initialize all event handlers for UI controls.
        /// Remember when Boss asked for dark mode? That's why we have so many handlers now!
        /// </summary>
        private void InitializeEventHandlers()
        {
            // Dark mode toggle handlers (because developers love dark mode!)
            DarkModeToggle.Checked += DarkModeToggle_Changed;
            DarkModeToggle.Unchecked += DarkModeToggle_Changed;

            // Slider value change handlers
            SilenceThresholdSlider.ValueChanged += SilenceThresholdSlider_ValueChanged;
            SilenceDurationSlider.ValueChanged += SilenceDurationSlider_ValueChanged;

            // Button click handlers
            BrowseButton.Click += BrowseButton_Click;
            OutputBrowseButton.Click += OutputBrowseButton_Click;
            ProcessButton.Click += ProcessButton_Click;
            CancelButton.Click += CancelButton_Click;

            // Batch mode checkbox handlers
            BatchModeCheckBox.Checked += BatchModeCheckBox_Changed;
            BatchModeCheckBox.Unchecked += BatchModeCheckBox_Changed;
        }

        #region FFmpeg Validation

        /// <summary>
        /// Check if FFmpeg is installed and available.
        /// Thanks to Xabe.FFmpeg, this will auto-download if needed. Sweet!
        /// </summary>
        private async void CheckFFmpegInstallation()
        {
            UpdateStatus("Initializing FFmpeg...");
            LogMessage("Checking FFmpeg installation...");

            var isInstalled = await FFmpegProcessor.IsFFmpegInstalledAsync();
            
            if (!isInstalled)
            {
                MessageBox.Show(
                    "FFmpeg initialization in progress.\n\n" +
                    "The application will download FFmpeg binaries automatically.\n" +
                    "Please ensure you have an internet connection.",
                    "FFmpeg Initialization",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                UpdateStatus("Ready (FFmpeg will be downloaded on first use)");
                LogMessage("FFmpeg will be downloaded automatically");
            }
            else
            {
                UpdateStatus("Ready");
                LogMessage("FFmpeg is ready to rock!");
            }
        }

        #endregion

        #region Dark Mode Management

        /// <summary>
        /// Handle dark mode toggle change.
        /// Boss loves the dark side of the Force!
        /// </summary>
        private void DarkModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (DarkModeToggle.IsChecked == true)
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
        }

        /// <summary>
        /// Apply dark theme colors to all UI elements.
        /// VS Code dark theme inspired colors here!
        /// </summary>
        private void ApplyDarkTheme()
        {
            this.Resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            this.Resources["CardBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"));
            this.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46"));
            this.Resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            this.Resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            this.Resources["InputBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252526"));
        }

        /// <summary>
        /// Apply light theme colors to all UI elements.
        /// For those rare souls who prefer light mode...
        /// </summary>
        private void ApplyLightTheme()
        {
            this.Resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F9F9"));
            this.Resources["CardBackgroundBrush"] = new SolidColorBrush(Colors.White);
            this.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            this.Resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            this.Resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            this.Resources["InputBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
        }

        #endregion

        #region Slider Value Updates

        /// <summary>
        /// Update silence threshold value display when slider changes
        /// </summary>
        private void SilenceThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SilenceThresholdValue != null)
            {
                SilenceThresholdValue.Text = $"{e.NewValue:F0} dB";
            }
        }

        /// <summary>
        /// Update silence duration value display when slider changes
        /// </summary>
        private void SilenceDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SilenceDurationValue != null)
            {
                SilenceDurationValue.Text = $"{e.NewValue:F1} s";
            }
        }

        #endregion

        #region File Selection

        /// <summary>
        /// Handle browse button click for input file selection.
        /// Single or batch mode, we got you covered!
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchModeCheckBox.IsChecked == true)
            {
                SelectMultipleFiles();
            }
            else
            {
                SelectSingleFile();
            }
        }

        /// <summary>
        /// Open dialog to select a single video file
        /// </summary>
        private void SelectSingleFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a video file",
                Filter = "Video files (*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv)|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv|All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedInputFile = dialog.FileName;
                _selectedInputFiles = null;
                InputFileTextBox.Text = _selectedInputFile;
                
                GenerateOutputFileName();
                ProcessButton.IsEnabled = true;
                
                UpdateStatus($"File selected: {Path.GetFileName(_selectedInputFile)}");
                LogMessage($"Input file: {_selectedInputFile}");
            }
        }

        /// <summary>
        /// Open dialog to select multiple video files for batch processing.
        /// Because one video is never enough!
        /// </summary>
        private void SelectMultipleFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select video files",
                Filter = "Video files (*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv)|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv|All files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedInputFiles = dialog.FileNames;
                _selectedInputFile = null;
                InputFileTextBox.Text = $"{dialog.FileNames.Length} file(s) selected";
                
                OutputFileTextBox.Text = "Files generated automatically";
                _selectedOutputFile = null;
                ProcessButton.IsEnabled = true;
                
                UpdateStatus($"{dialog.FileNames.Length} file(s) selected");
                LogMessage($"Batch mode: {dialog.FileNames.Length} file(s)");
            }
        }

        /// <summary>
        /// Handle output file browse button click
        /// </summary>
        private void OutputBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatchModeCheckBox.IsChecked == true)
            {
                MessageBox.Show(
                    "In batch processing mode, output files are generated automatically.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save output file",
                Filter = "MP4 video files (*.mp4)|*.mp4|AVI video files (*.avi)|*.avi|MKV video files (*.mkv)|*.mkv|All files (*.*)|*.*",
                FileName = _selectedOutputFile ?? "output.mp4"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedOutputFile = dialog.FileName;
                OutputFileTextBox.Text = _selectedOutputFile;
                LogMessage($"Output file: {_selectedOutputFile}");
            }
        }

        /// <summary>
        /// Generate output filename automatically based on input file.
        /// Adds "_trimmed" suffix because naming things is hard!
        /// </summary>
        private void GenerateOutputFileName()
        {
            if (string.IsNullOrEmpty(_selectedInputFile))
                return;

            var directory = Path.GetDirectoryName(_selectedInputFile) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_selectedInputFile);
            var extension = Path.GetExtension(_selectedInputFile);

            _selectedOutputFile = Path.Combine(directory, $"{fileNameWithoutExtension}_trimmed{extension}");
            OutputFileTextBox.Text = _selectedOutputFile;
        }

        /// <summary>
        /// Handle batch mode checkbox state change
        /// </summary>
        private void BatchModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _selectedInputFile = null;
            _selectedInputFiles = null;
            _selectedOutputFile = null;
            InputFileTextBox.Text = "No file selected";
            OutputFileTextBox.Text = BatchModeCheckBox.IsChecked == true 
                ? "Files generated automatically" 
                : "Generated automatically";
            ProcessButton.IsEnabled = false;

            LogMessage(BatchModeCheckBox.IsChecked == true 
                ? "Batch processing mode enabled" 
                : "Single file mode enabled");
        }

        #endregion

        #region Video Processing

        /// <summary>
        /// Handle process button click to start video processing.
        /// This is where the real work begins!
        /// </summary>
        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedInputFile) && _selectedInputFiles == null)
            {
                MessageBox.Show(
                    "Please select an input video file.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _isProcessing = true;
            UpdateUIForProcessing(true);
            ProgressPanel.Visibility = Visibility.Visible;
            LogMessage("=== PROCESSING STARTED ===");

            try
            {
                // Create processing parameters
                var parameters = new VideoProcessingParameters
                {
                    InputFile = _selectedInputFile ?? string.Empty,
                    OutputFile = _selectedOutputFile ?? string.Empty,
                    SilenceThreshold = SilenceThresholdSlider.Value,
                    SilenceDuration = SilenceDurationSlider.Value,
                    IsBatchMode = BatchModeCheckBox.IsChecked == true,
                    InputFiles = _selectedInputFiles
                };

                // Create processor and subscribe to events
                _processor = new FFmpegProcessor();
                _cancellationTokenSource = new CancellationTokenSource();

                _processor.ProgressChanged += Processor_ProgressChanged;
                _processor.LogReceived += Processor_LogReceived;
                _processor.ProcessCompleted += Processor_ProcessCompleted;

                // Start processing
                await _processor.ProcessVideosAsync(parameters, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Processing cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                UpdateStatus("Error during processing");
            }
            finally
            {
                _isProcessing = false;
                UpdateUIForProcessing(false);
                
                if (_processor != null)
                {
                    _processor.ProgressChanged -= Processor_ProgressChanged;
                    _processor.LogReceived -= Processor_LogReceived;
                    _processor.ProcessCompleted -= Processor_ProcessCompleted;
                }
            }
        }

        /// <summary>
        /// Handle cancel button click to stop processing.
        /// Sometimes you just need to abort mission!
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing && _processor != null)
            {
                var result = MessageBox.Show(
                    "Do you really want to cancel the current processing?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _processor.Cancel();
                    _cancellationTokenSource?.Cancel();
                    LogMessage("Cancellation in progress...");
                }
            }
        }

        #endregion

        #region FFmpeg Processor Event Handlers

        /// <summary>
        /// Handle progress change events from FFmpeg processor
        /// </summary>
        private void Processor_ProgressChanged(object? sender, ProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProcessProgressBar.Value = e.Percentage;
                ProgressStatusText.Text = e.Status;
            });
        }

        /// <summary>
        /// Handle log message events from FFmpeg processor
        /// </summary>
        private void Processor_LogReceived(object? sender, LogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LogMessage($"[{e.Timestamp:HH:mm:ss}] {e.Message}");
            });
        }

        /// <summary>
        /// Handle process completion events from FFmpeg processor
        /// </summary>
        private void Processor_ProcessCompleted(object? sender, ProcessCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    MessageBox.Show(
                        "Video processing completed successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    UpdateStatus("Processing completed");
                }
                else
                {
                    UpdateStatus($"Error: {e.Message}");
                }
            });
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Update UI controls state based on processing status
        /// </summary>
        private void UpdateUIForProcessing(bool isProcessing)
        {
            BrowseButton.IsEnabled = !isProcessing;
            OutputBrowseButton.IsEnabled = !isProcessing;
            ProcessButton.IsEnabled = !isProcessing;
            CancelButton.IsEnabled = isProcessing;
            SilenceThresholdSlider.IsEnabled = !isProcessing;
            SilenceDurationSlider.IsEnabled = !isProcessing;
            BatchModeCheckBox.IsEnabled = !isProcessing;
        }

        /// <summary>
        /// Update status text in the footer
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        /// <summary>
        /// Append a message to the log textbox.
        /// Because Boss loves detailed logs!
        /// </summary>
        private void LogMessage(string message)
        {
            LogTextBox.AppendText($"{message}\n");
            LogTextBox.ScrollToEnd();
        }

        #endregion
    }
}