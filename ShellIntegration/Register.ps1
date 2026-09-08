$ErrorActionPreference = 'Stop'
$exe = (Resolve-Path (Join-Path $PSScriptRoot '..\Portable\Ferry.exe')).Path

function Set-FerryVerb([string]$keyPath, [string]$argument) {
    New-Item -Path $keyPath -Force | Out-Null
    Set-Item -Path $keyPath -Value 'Open with Ferry'
    Set-ItemProperty -Path $keyPath -Name 'Icon' -Value ('"' + $exe + '"')
    $commandKey = Join-Path $keyPath 'command'
    New-Item -Path $commandKey -Force | Out-Null
    Set-Item -Path $commandKey -Value ('"' + $exe + '" "' + $argument + '"')
}

Set-FerryVerb 'HKCU:\Software\Classes\Directory\shell\Ferry' '%1'
Set-FerryVerb 'HKCU:\Software\Classes\Directory\Background\shell\Ferry' '%V'

Write-Host ''
Write-Host 'Ferry Explorer integration registered for the current user.'
Write-Host 'No administrator rights were required.'
