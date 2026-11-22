#define AppName "CapsUnlock"
#define AppVersion "0.2.1"
#define AppPublisher "Manuel Melendez"
#define AppExeName "CapsUnlock.Hotkeys.exe"

[Setup]
AppId={{8F9F45B4-8EAC-4D14-9947-F2DBA6C3D0C2}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\CapsUnlock
DefaultGroupName=CapsUnlock
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=CapsUnlock-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
UninstallDisplayIcon={app}\{#AppExeName}
Uninstallable=yes

[Files]
Source: "..\windows\CapsUnlock.Hotkeys\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\CapsUnlock"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\CapsUnlock"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CapsUnlock"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch CapsUnlock"; Flags: nowait postinstall skipifsilent
