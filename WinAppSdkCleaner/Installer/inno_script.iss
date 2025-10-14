; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.2.2

#define appName "WinAppSdkCleaner"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "winappsdkcleaner.47345980516833259"

[Setup]
AppId={#appId}
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
WizardSizePercent=110,110
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
DisableDirPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/WinAppSdkCleaner/releases
ArchitecturesInstallIn64BitMode=x64 arm64
ArchitecturesAllowed=x86 x64 arm64

[Files]
Source: "..\bin\Release\win-x64\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs;
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs solidbreak;

[Icons]
Name: "{group}\{#appName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent

[Code]

// because "DisableReadyPage" and "DisableProgramGroupPage" are set to yes adjust the next/install button text
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;

// A < B returns -ve
// A = B returns 0
// A > B returns +ve
function VersionComparer(const A, B: String): Integer;
var
  X, Y: Int64;
begin
  if not (StrToVersion(A, X) and StrToVersion(B, Y)) then
    RaiseException('StrToVersion(''' + A + ''', ''' + B + ''')');
  
  Result := ComparePackedVersion(X, Y);
end;

function GetUninstallRegKey: String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';
end;

function IsDowngradeInstall: Boolean;
var
  InstalledVersion: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) then
    Result := VersionComparer(InstalledVersion, '{#appVer}') > 0;
end;

function NewLine: String;
begin
  Result := #13#10;
end;

function InitializeSetup: Boolean;
var 
  Message: String;
begin
  Result := true;
  
  try
    if IsDowngradeInstall then
      RaiseException('Downgrading isn''t supported.' + NewLine + 'Please uninstall the current version first.');

  except
    Message := 'An error occured when checking install prerequesites:' + NewLine + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;

procedure UninatallAnyPreviousVersion();
var
  ResultCode, Attempts: Integer;
  UninstallerPath: String; 
begin    
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin
    Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    if ResultCode = 0 then // wait until the uninstall has completed
    begin
      Attempts := 2 * 30 ; // timeout after approximately 30 seconds
       
      while FileExists(UninstallerPath) and (Attempts > 0) do
      Begin
        Sleep(500);
        Attempts := Attempts - 1;
      end;
    end;
  
    if (ResultCode <> 0) or FileExists(UninstallerPath) then
    begin
      SuppressibleMsgBox('Setup failed to uninstall a previous version.', mbCriticalError, MB_OK, IDOK) ;
      Abort;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then
  begin
    // when upgrading the remnants of an old install may cause the new version to fail to start. 
    UninatallAnyPreviousVersion();
  end;
end;

