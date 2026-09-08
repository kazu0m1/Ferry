@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if not exist "Ferry.exe" (
  echo Ferry.exe has not been built yet.
  echo Building Ferry using the .NET Framework compiler included with Windows...
  call "..\Build.cmd"
  if errorlevel 1 exit /b 1
)

start "" "%~dp0Ferry.exe" %*
