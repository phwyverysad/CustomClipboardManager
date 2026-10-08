; Custom Clipboard Manager - Inno Setup Script (with Certificate Install)

#define MyAppName "Custom Clipboard Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "phwyverysad"
#define MyAppExeName "CustomClipboardManager.exe"
#define MySourceExe "bin\Release\net10.0-windows\win-x64\publish\CustomClipboardManager.exe"
#define MyCertFile "installer_output\ClipboardManager.cer"

[Setup]
AppId={{A3F7B2C1-4D5E-4F6A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=installer_output
OutputBaseFilename=CustomClipboardManager_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create Desktop Shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; ไฟล์โปรแกรม
Source: "{#MySourceExe}"; DestDir: "{app}"; Flags: ignoreversion
; Certificate สำหรับติดตั้งในเครื่อง
Source: "{#MyCertFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Code]
procedure InstallCertificate();
var
  CertPath: String;
  ResultCode: Integer;
begin
  CertPath := ExpandConstant('{tmp}\ClipboardManager.cer');
  // ติดตั้ง cert เข้า Trusted Root Certification Authorities (เครื่อง Local)
  Exec('certutil.exe', '-addstore -f "Root" "' + CertPath + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // ติดตั้ง cert เข้า Trusted Publishers ด้วย
  Exec('certutil.exe', '-addstore -f "TrustedPublisher" "' + CertPath + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    InstallCertificate();
  end;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"
