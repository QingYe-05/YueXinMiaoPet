#define MyAppName "月薪喵桌宠"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "YueXinMiao"
#define MyAppExeName "YueXinMiaoPet.exe"
#define DotNet48Installer "redist\NDP48-x86-x64-AllOS-ENU.exe"

[Setup]
AppId={{A2F5A46B-B930-4E38-875A-3A7D77E9869A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\YueXinMiaoPet
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=YueXinMiaoPet_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=6.1sp1
UninstallDisplayName={#MyAppName}
SetupIconFile=..\src\YueXinMiaoPet\Assets\Icons\app.ico
WizardStyle=modern

[Languages]
Name: "default"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："; Flags: unchecked

[Files]
Source: "..\src\YueXinMiaoPet\bin\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\YueXinMiaoPet\bin\Release\PetAssets\*"; DestDir: "{app}\PetAssets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\YueXinMiaoPet\bin\Release\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\YueXinMiaoPet\bin\Release\Data\*"; DestDir: "{app}\Data"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#DotNet48Installer}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Icons\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Icons\app.ico"; Tasks: desktopicon

[Run]
Filename: "{tmp}\NDP48-x86-x64-AllOS-ENU.exe"; Parameters: "/passive /norestart"; StatusMsg: "正在安装 .NET Framework 4.8..."; Check: NeedsDotNet48
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent; Check: IsDotNet48Installed

[Code]
const
  DotNet48Release = 528040;
  DotNet48RegKey = 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full';

function HasDotNet48InRoot(RootKey: Integer): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(RootKey, DotNet48RegKey, 'Release', Release)
    and (Release >= DotNet48Release);
end;

function IsDotNet48Installed(): Boolean;
begin
  Result := HasDotNet48InRoot(HKLM);

  if (not Result) and IsWin64 then
  begin
    Result := HasDotNet48InRoot(HKLM64);
  end;
end;

function NeedsDotNet48(): Boolean;
begin
  Result := not IsDotNet48Installed();
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not IsDotNet48Installed() then
  begin
    MsgBox('月薪喵桌宠需要 .NET Framework 4.8。安装程序已内置离线安装包 NDP48-x86-x64-AllOS-ENU.exe，将先尝试安装 .NET Framework 4.8。' + #13#10#13#10 +
           '如果系统要求管理员权限或重启，请按提示完成后再启动月薪喵桌宠。', mbInformation, MB_OK);
  end;
end;
