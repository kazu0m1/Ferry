param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "SafetyTestArchives")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function New-TestZip {
    param(
        [string]$Name,
        [scriptblock]$Populate
    )

    $path = Join-Path $OutputDirectory $Name
    if (Test-Path $path) { Remove-Item -Force $path }

    $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $zip = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            & $Populate $zip
        }
        finally {
            $zip.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "Created $path"
}

function Add-TextEntry {
    param($Zip, [string]$EntryName, [string]$Text)
    $entry = $Zip.CreateEntry($EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $writer = New-Object System.IO.StreamWriter($entry.Open(), [System.Text.Encoding]::UTF8)
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}

New-TestZip "01-normal.zip" {
    param($zip)
    Add-TextEntry $zip "folder/hello.txt" "normal"
}

New-TestZip "02-path-traversal.zip" {
    param($zip)
    Add-TextEntry $zip "../outside.txt" "must be blocked"
}

New-TestZip "03-absolute-drive.zip" {
    param($zip)
    Add-TextEntry $zip "C:/outside.txt" "must be blocked"
}

New-TestZip "04-absolute-root.zip" {
    param($zip)
    Add-TextEntry $zip "/outside.txt" "must be blocked"
}

New-TestZip "05-ads-name.zip" {
    param($zip)
    Add-TextEntry $zip "safe.txt:stream" "must be blocked"
}

New-TestZip "06-device-name.zip" {
    param($zip)
    Add-TextEntry $zip "CON.txt" "must be blocked"
}

New-TestZip "07-high-compression-ratio.zip" {
    param($zip)
    $entry = $zip.CreateEntry("highly-compressible.bin", [System.IO.Compression.CompressionLevel]::Optimal)
    $out = $entry.Open()
    try {
        $buffer = New-Object byte[] (1024 * 1024)
        for ($i = 0; $i -lt 8; $i++) { $out.Write($buffer, 0, $buffer.Length) }
    }
    finally { $out.Dispose() }
}

Write-Host "Done."
