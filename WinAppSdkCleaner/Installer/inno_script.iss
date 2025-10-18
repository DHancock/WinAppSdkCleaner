; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.5.4

#define appName "WinAppSdkCleaner"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "winappsdkcleaner.47345980516833259"
#define appMutexName "4ACA5302-CE42-4882-AA6E-FC54667A934B"
#define setupMutexName "47DBB18F-2BDF-4A04-AE5E-92342D63623A"

[Setup]
AppId={#appId}
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
AppMutex={#appMutexName},Global\{#appMutexName}
SetupMutex={#setupMutexName},Global\{#setupMutexName}
Compression=lzma2 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
PrivilegesRequired=lowest
WizardStyle=modern
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableFinishedPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/WinAppSdkCleaner/releases
ArchitecturesInstallIn64BitMode=x64compatible or arm64

[Files]
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs;
Source: "..\bin\Release\win-x64\publish\*";   DestDir: "{app}"; Check: PreferX64Files;   Flags: ignoreversion recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*";   DestDir: "{app}"; Check: PreferX86Files;   Flags: ignoreversion recursesubdirs solidbreak;

[Icons]
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"

[Run]
Filename: "{app}\{#appExeName}"; Flags: nowait postinstall skipifsilent

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"

[Code]
function PreferArm64Files: Boolean;
begin
  Result := IsArm64;
end;

function PreferX64Files: Boolean;
begin
  Result := not PreferArm64Files and IsX64Compatible;
end;

function PreferX86Files: Boolean;
begin
  Result := not PreferArm64Files and not PreferX64Files;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // if an old version of the app is running ensure inno setup shuts it down
  if CurPageID = wpPreparing then
  begin
    WizardForm.PreparingNoRadio.Enabled := false;
  end;
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
  InstalledVersion, UninstallerPath: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) and 
     RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin   
    // check both the app version and that it (may be) possible to uninstall it 
    Result := (VersionComparer(InstalledVersion, '{#appVer}') > 0) and FileExists(RemoveQuotes(UninstallerPath));
  end;
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

procedure UninstallAnyPreviousVersion;
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
      
      // If the file still exists then the uninstall failed. 
      // There isn't much that can be done, informing the user or aborting 
      // won't acheive much and could render it imposible to install this new version.
      // Installing the new version will over write the registry and add a new uninstaller exe etc.
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // When upgrading uninstall first or the app may trap on start up.
    // While some dll versions aren't incremented that isn't the only problem 
    UninstallAnyPreviousVersion;
  end;
end;

