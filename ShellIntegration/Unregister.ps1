$ErrorActionPreference = 'SilentlyContinue'
Remove-Item 'HKCU:\Software\Classes\Directory\shell\Ferry' -Recurse -Force
Remove-Item 'HKCU:\Software\Classes\Directory\Background\shell\Ferry' -Recurse -Force
Write-Host ''
Write-Host 'Ferry Explorer integration removed for the current user.'
