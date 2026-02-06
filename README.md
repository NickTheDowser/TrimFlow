<div align="center">

# 🎬 TrimFlow

### Automatically remove silences from your videos

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=flat-square&logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/github/license/NickTheDowser/TrimFlow?style=flat-square)](LICENSE)
[![Release](https://img.shields.io/github/v/release/NickTheDowser/TrimFlow?style=flat-square)](https://github.com/NickTheDowser/TrimFlow/releases)
[![Downloads](https://img.shields.io/github/downloads/NickTheDowser/TrimFlow/total?style=flat-square)](https://github.com/NickTheDowser/TrimFlow/releases)

**TrimFlow** is a powerful and user-friendly desktop application that automatically detects and removes silent segments from video files, saving you time and storage space.

[Download](https://github.com/NickTheDowser/TrimFlow/releases) • [Report Bug](https://github.com/NickTheDowser/TrimFlow/issues) • [Request Feature](https://github.com/NickTheDowser/TrimFlow/issues)

</div>

---

## ✨ Features

- **🎯 Smart Silence Detection** - Automatically identifies silent segments using FFmpeg's powerful audio analysis
- **⚡ Fast Processing** - Optimized segment extraction with minimal re-encoding
- **📦 Batch Processing** - Process multiple video files at once
- **🎨 Modern UI** - Beautiful WPF interface with dark/light theme support
- **⚙️ Customizable Settings** - Adjust silence threshold and minimum duration to fit your needs
- **📊 Real-time Progress** - Visual progress bar and detailed logging
- **🔧 No Setup Required** - FFmpeg binaries are downloaded automatically
- **💾 Smart Output** - Automatic file naming with `_trimmed` suffix
- **🌍 Cross-format Support** - Works with MP4, AVI, MKV, MOV, WMV, and FLV

---

## 📸 Screenshots

<img width="666" height="510" alt="TrimFlow" src="https://github.com/user-attachments/assets/327ebaf2-c0d6-4e33-a7e3-5cd4a0687839" />

---

## 🚀 Quick Start

### Installation

#### Option 1: Installer (Recommended)

1. Download the latest `TrimFlow_Setup_vX.X.X.exe` from [Releases](https://github.com/NickTheDowser/TrimFlow/releases)
2. Run the installer
3. Follow the installation wizard
4. Launch TrimFlow from Start Menu or Desktop icon

#### Option 2: Portable Version

1. Download `TrimFlow_vX.X.X_Portable.zip` from [Releases](https://github.com/NickTheDowser/TrimFlow/releases)
2. Extract to any folder
3. Run `TrimFlow.exe`

### System Requirements

- **OS**: Windows 10 (19041+) or Windows 11 (64-bit)
- **RAM**: 4 GB minimum (8 GB recommended)
- **Storage**: 500 MB free space
- **Internet**: Required for first-time FFmpeg download

---

## 📖 Usage

### Basic Workflow

1. **Select Video File**
   - Click the **Browse** button
   - Choose your video file (MP4, AVI, MKV, MOV, WMV, or FLV)

2. **Configure Settings** (Optional)
   - **Silence Threshold**: Adjust from -60 dB to -10 dB (lower = more sensitive)
   - **Minimum Duration**: Set from 0.1 to 5.0 seconds (minimum silence length to remove)

3. **Choose Output Location** (Optional)
   - Click **Choose** to specify output location
   - Leave default for automatic `_trimmed` suffix

4. **Process**
   - Click the **Process** button
   - Watch real-time progress in the progress panel
   - Done! Your trimmed video is ready

### Batch Processing

Process multiple videos at once:

1. Enable **Batch processing** checkbox
2. Click **Browse** and select multiple files
3. Output files will be automatically generated in the same folder
4. Click **Process** to start batch operation

---

## 🎛️ Configuration

### Silence Detection Parameters

#### Silence Threshold (dB)
- **Default**: -30 dB
- **Range**: -60 dB to -10 dB
- **Description**: Audio level considered as silence. Lower values detect quieter sounds as silence.
- **Use Cases**:
  - `-50 dB`: Very sensitive, removes even quiet background noise
  - `-30 dB`: Balanced (default), good for most videos
  - `-20 dB`: Less sensitive, only removes near-complete silence

#### Minimum Duration (seconds)
- **Default**: 0.5 seconds
- **Range**: 0.1 to 5.0 seconds
- **Description**: Minimum length of silence to remove. Shorter pauses are kept.
- **Use Cases**:
  - `0.1s`: Removes very short pauses (aggressive)
  - `0.5s`: Balanced (default), preserves natural speech rhythm
  - `2.0s`: Only removes long pauses

---

## 🛠️ Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.12+) or [Visual Studio Code](https://code.visualstudio.com/)
- Windows 10/11 Development Environment

### Clone and Build
