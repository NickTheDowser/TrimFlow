; TrimFlow Installer Script
; Boss's professional installer for the best silence trimmer!

#define MyAppName "TrimFlow"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Nicolas DESENY / Aspermind studio"
#define MyAppURL "https://github.com/NickTheDowser/trimflow"
#define MyAppExeName "TrimFlow.exe"

[Setup]
; App information
AppId={{8B5F3A2C-9D4E-4F1B-A6C3-7E2D8F9A1B0C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no

; Output
OutputDir=.\dist
OutputBaseFilename=TrimFlow_Setup_v{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes

; System requirements
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Visual appearance
WizardStyle=modern
WizardSizePercent=100,100
DisableWelcomePage=no

; Uninstall
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; Main application files
Source: ".\bin\Release\net10.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; FFmpeg binaries - install to AppData
Source: "bin\Release\net10.0-windows\ffmpeg\ffmpeg.exe"; DestDir: "{localappdata}\TrimFlow\ffmpeg"; Flags: ignoreversion
Source: "bin\Release\net10.0-windows\ffmpeg\ffprobe.exe"; DestDir: "{localappdata}\TrimFlow\ffmpeg"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpWelcome then
  begin
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  FFmpegDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Ensure FFmpeg directory exists in AppData
    FFmpegDir := ExpandConstant('{localappdata}\TrimFlow\ffmpeg');
    ForceDirectories(FFmpegDir);
  end;
end;

[Messages]
WelcomeLabel1=Welcome to [name] Setup
WelcomeLabel2=This will install [name/ver] on your computer.%n%nTrimFlow automatically removes silence from video files using FFmpeg.%n%nIt is recommended that you close all other applications before continuing.

[UninstallDelete]
Type: filesandordirs; Name: "{app}\ffmpeg"
Type: filesandordirs; Name: "{localappdata}\TrimFlow"