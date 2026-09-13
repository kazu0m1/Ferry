@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo Ferry ZIP safety test archive generator
echo.
echo This launches the bundled PowerShell script with ExecutionPolicy=Bypass
echo for this process only. It does NOT change your Windows policy.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Create-Safety-TestArchives.ps1" %*
set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
  echo FAILED. Exit code: %RC%
) else (
  echo Completed successfully.
  echo Test ZIPs are in:
  echo %~dp0SafetyTestArchives
)
echo.
pause
exit /b %RC%
