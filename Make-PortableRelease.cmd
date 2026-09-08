@echo off
setlocal EnableExtensions
cd /d "%~dp0"

call Build.cmd
if errorlevel 1 exit /b 1

set "VERSION=1.0.0"
set "DIST=%~dp0dist"
set "STAGE=%DIST%\Ferry-v%VERSION%-win-portable"
set "ZIP=%DIST%\Ferry-v%VERSION%-win-portable.zip"

if exist "%STAGE%" rmdir /s /q "%STAGE%"
if exist "%ZIP%" del /q "%ZIP%"
mkdir "%STAGE%"
mkdir "%STAGE%\config"

copy /y "Portable\Ferry.exe" "%STAGE%\Ferry.exe" >nul
copy /y "LICENSE.txt" "%STAGE%\LICENSE.txt" >nul
copy /y "Portable\README.txt" "%STAGE%\README.txt" >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "Compress-Archive -Path '%STAGE%\*' -DestinationPath '%ZIP%' -CompressionLevel Optimal -Force"
if errorlevel 1 (
  echo.
  echo ERROR: Could not create release ZIP.
  exit /b 1
)

for /f "tokens=*" %%H in ('powershell.exe -NoProfile -Command "(Get-FileHash -Algorithm SHA256 '%ZIP%').Hash.ToLower()"') do set "HASH=%%H"

echo.
echo Created:
echo   %ZIP%
echo SHA-256:
echo   %HASH%
echo.
exit /b 0
