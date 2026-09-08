@echo off
setlocal EnableExtensions
cd /d "%~dp0"

if not exist "..\Portable\Ferry.exe" (
  call "..\Build.cmd"
  if errorlevel 1 exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Register.ps1"
