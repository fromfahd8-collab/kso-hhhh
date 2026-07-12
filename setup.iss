[Setup]
AppName=KSO Download Turbo Ultra
AppVersion=1.0 PRO
AppPublisher=KSO - Abdullah & Abdelrahman Hany
DefaultDirName={autopf}\KSO
OutputDir=Output
OutputBaseFilename=KSO_Download_Turbo_Ultra_Setup
SetupIconFile=setup_files\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء أيقونة على سطح المكتب"

[Files]
Source: "setup_files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\KSO Download Turbo Ultra"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"
Name: "{commondesktop}\KSO Download Turbo Ultra"; Filename: "{app}\KSO.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\KSO.exe"; Description: "تشغيل KSO الآن"; Flags: nowait postinstall skipifsilent
