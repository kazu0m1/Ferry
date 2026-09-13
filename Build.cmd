@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo.
  echo ERROR: The .NET Framework C# compiler was not found.
  echo Enable/install .NET Framework 4.x, then run Build.cmd again.
  echo.
  pause
  exit /b 1
)

for %%I in ("%CSC%") do set "FRAMEWORK_DIR=%%~dpI"
set "WPF_DIR=%FRAMEWORK_DIR%WPF"

if not exist "%WPF_DIR%\PresentationFramework.dll" (
  echo.
  echo ERROR: WPF framework assemblies were not found in:
  echo   %WPF_DIR%
  echo.
  pause
  exit /b 1
)

if not exist "%FRAMEWORK_DIR%System.IO.Compression.dll" (
  echo.
  echo ERROR: System.IO.Compression.dll was not found in:
  echo   %FRAMEWORK_DIR%
  echo.
  pause
  exit /b 1
)

if not exist "%FRAMEWORK_DIR%System.IO.Compression.FileSystem.dll" (
  echo.
  echo ERROR: System.IO.Compression.FileSystem.dll was not found in:
  echo   %FRAMEWORK_DIR%
  echo.
  pause
  exit /b 1
)

if not exist "Portable" mkdir "Portable"
if not exist "Portable\config" mkdir "Portable\config"

echo Building Ferry.exe...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32manifest:"Source\Ferry\app.manifest" /out:"Portable\Ferry.exe" ^
 /reference:"%FRAMEWORK_DIR%System.dll" ^
 /reference:"%FRAMEWORK_DIR%System.Core.dll" ^
 /reference:"%FRAMEWORK_DIR%System.IO.Compression.dll" ^
 /reference:"%FRAMEWORK_DIR%System.IO.Compression.FileSystem.dll" ^
 /reference:"%FRAMEWORK_DIR%System.Data.dll" ^
 /reference:"%FRAMEWORK_DIR%System.Web.Extensions.dll" ^
 /reference:"%FRAMEWORK_DIR%System.Xaml.dll" ^
 /reference:"%FRAMEWORK_DIR%System.Windows.Forms.dll" ^
 /reference:"%WPF_DIR%\WindowsBase.dll" ^
 /reference:"%WPF_DIR%\PresentationCore.dll" ^
 /reference:"%WPF_DIR%\PresentationFramework.dll" ^
 "Source\Ferry\AppSettings.cs" ^
 "Source\Ferry\ArchiveHelper.cs" ^
 "Source\Ferry\ArchiveModels.cs" ^
 "Source\Ferry\ArchiveService.cs" ^
 "Source\Ferry\ArchiveSetupWindows.cs" ^
 "Source\Ferry\AssemblyInfo.cs" ^
 "Source\Ferry\ClipboardHelper.cs" ^
 "Source\Ferry\ChoiceDialog.cs" ^
 "Source\Ferry\FileItem.cs" ^
 "Source\Ferry\FileItemComparer.cs" ^
 "Source\Ferry\KnownFolders.cs" ^
 "Source\Ferry\Logger.cs" ^
 "Source\Ferry\MainWindow.cs" ^
 "Source\Ferry\NaturalStringComparer.cs" ^
 "Source\Ferry\Program.cs" ^
 "Source\Ferry\PromptDialog.cs" ^
 "Source\Ferry\RenameDialog.cs" ^
 "Source\Ferry\RenameEngine.cs" ^
 "Source\Ferry\RenameEntry.cs" ^
 "Source\Ferry\RecycleBinService.cs" ^
 "Source\Ferry\SearchService.cs" ^
 "Source\Ferry\SettingsWindow.cs" ^
 "Source\Ferry\ShellContextMenu.cs" ^
 "Source\Ferry\ShellFileOperations.cs" ^
 "Source\Ferry\ShellFolderPicker.cs" ^
 "Source\Ferry\ShellInterop.cs" ^
 "Source\Ferry\ShortcutHelper.cs" ^
 "Source\Ferry\TabState.cs" ^
 "Source\Ferry\VirtualizingWrapPanel.cs"

if errorlevel 1 (
  echo.
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Built successfully:
echo   Portable\Ferry.exe
echo.
exit /b 0
