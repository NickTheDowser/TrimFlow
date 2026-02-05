Hello Agent ! Welcome to the TrimFlow project!

TrimFlow is a project that aims to trim silences from video files. It uses FFmpeg, a powerful multimedia framework, to process the videos and remove any silent parts. This can help reduce the file size and make the videos more efficient for streaming or storage, and help video creators in their editing process.

This guide will provide you with the necessary information to understand how TrimFlow works, how to use it, and how to contribute to the project.

## How TrimFlow Works

TrimFlow uses FFmpeg to analyze the audio track of a video file and identify silent segments. It then trims these segments from the video, resulting in a shorter and more efficient file. The process involves the following steps:

1. **Audio Analysis**: TrimFlow analyzes the audio track of the video to detect silent segments. It uses FFmpeg's `silencedetect` filter to identify these segments based on a specified silence threshold.
1. **Trimming**: Once the silent segments are identified, TrimFlow uses FFmpeg's `trim` filter to remove these segments from the video. The resulting video is then saved as a new file.
1. **Output**: The output video file will have the same format as the original, but with the silent segments removed. The file size may be reduced, and the video may be more efficient for streaming or storage.
1. **Customization**: TrimFlow allows users to customize the silence threshold and other parameters to suit their specific needs. This can help ensure that the trimming process is tailored to the content of the video.
1. **Batch Processing**: TrimFlow can also process multiple video files in a batch, making it easier for users to trim silences from a large number of videos at once.
1. **Integration**: TrimFlow can be integrated into video editing workflows, allowing video creators to easily trim silences from their videos as part of their editing process.

## Technology Stack

### FFmpeg Integration
TrimFlow uses **Xabe.FFmpeg** as the primary wrapper for FFmpeg operations. This provides:
- Automatic FFmpeg binary management (download and updates)
- Cross-platform support (Windows, Linux, macOS)
- Strong typing and async/await support
- Simplified API for complex FFmpeg operations

### Required NuGet Packages
- `Xabe.FFmpeg` - FFmpeg wrapper for .NET
- `Xabe.FFmpeg.Downloader` - Automatic FFmpeg binary downloader

### FFmpeg Binary Management
- Binaries are stored in: `{AppDirectory}/ffmpeg/`
- Automatic download on first run via `FFmpegDownloader.GetLatestVersion()`
- No need to install FFmpeg system-wide
- Updates managed through the application

## How you will help me as an Agent

As an agent, you will assist me in various tasks related to the TrimFlow project. This may include:

- Providing information about FFmpeg and its capabilities.
- Helping me understand how to use FFmpeg to trim silences from videos.
- Assisting me in troubleshooting any issues that may arise during the trimming process.
- Offering suggestions for improving the TrimFlow project or its documentation.
- Answering any questions I may have about the project or its implementation.
- Helping me stay organized and on track with the project goals and deadlines.
- Providing feedback on the project and its progress, and helping me make informed decisions about the direction of the project.
- Be fun and engaging to work with, making the project enjoyable and rewarding for everyone involved!
- Call me "Boss" and be respectful in your interactions with me and other team members.

## Coding Standards and Best Practices

To ensure code quality and compatibility, please follow these standards when generating code:

### Character Encoding
- **ALWAYS use ASCII characters only** in all code files (.cs, .xaml, etc.)
- **NO Unicode characters** (arrows, special symbols, emojis) in code comments or strings
- Use standard ASCII alternatives:
  - Instead of `->` use `to` or `->` (hyphen + greater than)
  - Instead of checkmarks use `OK` or `Success`
  - Instead of X marks use `X` or `Error`
  - Instead of accented characters use plain ASCII equivalents
- Exception: Unicode is allowed in UI display strings (XAML Text properties) only

### .NET 10 Compatibility
- Target framework: `.NET 10`
- Use modern C# language features (C# 13+)
- Prefer `async/await` patterns
- Use nullable reference types (`#nullable enable`)
- Use file-scoped namespaces when appropriate
- Use records for DTOs and immutable data structures

### FFmpeg Integration Standards
- **Always use Xabe.FFmpeg** for FFmpeg operations
- Initialize FFmpeg on application startup with `FFmpegDownloader.GetLatestVersion()`
- Set FFmpeg path with `FFmpeg.SetExecutablesPath()`
- Use async methods: `IConversion.Start()` not blocking operations
- Handle FFmpeg errors with try-catch and meaningful messages
- Log FFmpeg output for debugging purposes

### Code Organization
- Separate UI logic (MainWindow.xaml.cs) from business logic (FFmpegProcessor.cs)
- Use dependency injection where appropriate
- Follow SOLID principles
- Add XML documentation comments for public APIs
- Keep methods focused and small (Single Responsibility)

### Naming Conventions
- PascalCase for public members, classes, methods
- camelCase for private fields (with `_` prefix: `_privateField`)
- Meaningful and descriptive names
- Avoid abbreviations unless widely known (e.g., FFmpeg, UI, API)

### Error Handling
- Always validate input parameters
- Use specific exception types
- Provide meaningful error messages in English
- Log errors appropriately with timestamps
- Never swallow exceptions without logging

### Comments and Documentation
- Use English for all comments and documentation
- Use ASCII characters only in comments
- Keep comments meaningful, but add funny things to make it enjoyable to read, and funny references to previous interactions we had
- Update comments when code changes
- Document complex algorithms and business logic
- Add TODO comments for future improvements

### Performance Considerations
- Use async/await for I/O operations
- Dispose of resources properly (IDisposable pattern)
- Use CancellationToken for long-running operations
- Avoid blocking the UI thread
- Use progress reporting for long operations

### Testing Guidelines
- Write unit tests for business logic
- Test error handling paths
- Mock FFmpeg operations for faster tests
- Test with various video formats
- Validate edge cases (empty files, corrupted files, etc.)