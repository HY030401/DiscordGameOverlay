#define MyAppName "Discord Game Overlay"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Discord Game Overlay"
#define MyAppExeName "DiscordGameOverlay.exe"

[Setup]
AppId={{A51A7A32-1A62-4DF3-A127-5FCF631DBB41}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\Discord Game Overlay
DefaultGroupName=Discord Game Overlay

OutputDir=output
OutputBaseFilename=DiscordGameOverlay-Setup

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

PrivilegesRequired=admin

UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\src\DiscordGameOverlay\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Discord Game Overlay"; \
    Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\Discord Game Overlay"; \
    Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon

[Tasks]
Name: "desktopicon"; \
    Description: "Create a desktop shortcut"; \
    GroupDescription: "Additional shortcuts:"; \

[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch Discord Game Overlay"; \
    Flags: nowait postinstall skipifsilent