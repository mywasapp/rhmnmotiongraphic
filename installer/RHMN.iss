#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
[Setup]
AppId={{88BBFB08-BDF5-43CF-97D9-118EAF12D174}
AppName=RHMN Motion Graphic Studio
AppVersion={#MyAppVersion}
AppPublisher=RHMN
DefaultDirName={localappdata}\Programs\RHMN Motion Graphic Studio
DefaultGroupName=RHMN Motion Graphic Studio
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts
OutputBaseFilename=RHMN-Motion-Graphic-Studio-{#MyAppVersion}-win-x64-Setup
SetupIconFile=..\src\Rhmn.Studio\Assets\app.ico
UninstallDisplayIcon={app}\RHMN Motion Graphic Studio.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
MinVersion=10.0.17763
CloseApplications=yes
[Files]
Source: "..\artifacts\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\RHMN Motion Graphic Studio"; Filename: "{app}\RHMN Motion Graphic Studio.exe"
Name: "{autodesktop}\RHMN Motion Graphic Studio"; Filename: "{app}\RHMN Motion Graphic Studio.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Buat pintasan di desktop"; Flags: unchecked
[Run]
Filename: "{app}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Menyiapkan WebView2 Runtime (memerlukan internet)..."; Check: NeedsWebView2; Flags: waituntilterminated
Filename: "{app}\RHMN Motion Graphic Studio.exe"; Description: "Buka RHMN Motion Graphic Studio"; Flags: nowait postinstall skipifsilent
[Code]
function NeedsWebView2: Boolean;
var MachineVersion, UserVersion: String;
begin
  MachineVersion := '';
  UserVersion := '';
  RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', MachineVersion);
  RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', UserVersion);
  Result := ((MachineVersion = '') or (MachineVersion = '0.0.0.0')) and ((UserVersion = '') or (UserVersion = '0.0.0.0'));
end;
