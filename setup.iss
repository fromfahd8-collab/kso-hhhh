[Setup]
AppName=KSO Download Turbo Ultra
AppVersion=1.0 PRO
AppPublisher=KSO - Abdullah & Abdelrahman Hany
DefaultDirName={autopf}\KSO
DefaultGroupName=KSO Download Turbo
OutputDir=Output
OutputBaseFilename=KSO_Download_Turbo_Ultra_Setup
SetupIconFile=app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء أيقونة على سطح المكتب"; GroupDescription: "أيقونات إضافية:"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\KSO Download Turbo Ultra"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"
Name: "{commondesktop}\KSO Download Turbo Ultra"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon
Name: "{group}\إلغاء التثبيت"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\KSO.exe"; Description: "تشغيل KSO الآن"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\KSO"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"