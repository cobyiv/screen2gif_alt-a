[CmdletBinding()]
param([string]$Output = (Join-Path $PSScriptRoot 'dist/QuickCapture-win-x64'))
$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne 'Win32NT') { throw 'Windows and .NET 9 SDK required.' }
Get-Command dotnet -ErrorAction Stop | Out-Null
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Use a fresh output directory.' }
$version = '8.1.2'
$url = "https://github.com/GyanD/codexffmpeg/releases/download/$version/ffmpeg-$version-full_build.zip"
$hash = 'B8CDEFAB5F50590A076C27C2B56B0294A0E6154FADED28BA1BA05EBC4F801F57'
$work = Join-Path ([IO.Path]::GetTempPath()) ('qc-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    $archive = Join-Path $work 'ffmpeg.zip'
    Invoke-WebRequest -Uri $url -OutFile $archive
    if ((Get-FileHash -Algorithm SHA256 $archive).Hash -ne $hash) { throw 'FFmpeg archive SHA256 mismatch.' }
    Expand-Archive -LiteralPath $archive -DestinationPath $work
    $ffroot = Join-Path $work "ffmpeg-$version-full_build"
    $ff = Join-Path $ffroot 'bin/ffmpeg.exe'
    if (!(Test-Path $ff)) { throw 'FFmpeg archive structure changed.' }
    dotnet publish (Join-Path $PSScriptRoot 'ScreenToGif/ScreenToGif.csproj') -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=false -p:PostBuildEvent= -o $Output
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
    $ffDest = Join-Path $Output 'FFmpeg'
    New-Item -ItemType Directory -Path $ffDest | Out-Null
    Copy-Item $ff (Join-Path $ffDest 'ffmpeg.exe')
    Copy-Item (Join-Path $ffroot 'bin/ffprobe.exe') $ffDest
    $notices = Get-ChildItem -LiteralPath $ffroot -File | Where-Object { $_.Name -match 'license|copying|readme|source' }
    if (!$notices) { throw 'FFmpeg license/source notices missing.' }
    $notices | Copy-Item -Destination $ffDest
    @("FFmpeg $version", "Binary: $url", "Archive SHA256: $hash", 'Upstream source: https://github.com/FFmpeg/FFmpeg/tree/n8.1.2', 'Build distributor: https://www.gyan.dev/ffmpeg/builds/', 'Review corresponding-source and license obligations before redistribution.') | Set-Content (Join-Path $ffDest 'SOURCE-NOTICE.txt')
    Copy-Item (Join-Path $PSScriptRoot 'LICENSE.txt') (Join-Path $Output 'ScreenToGif-LICENSE.txt')
    Copy-Item (Join-Path $PSScriptRoot 'QuickCapture/quickcapture.example.json') (Join-Path $Output 'quickcapture.json')
    Copy-Item (Join-Path $PSScriptRoot 'QuickCapture/README.md') $Output
    Copy-Item (Join-Path $PSScriptRoot 'QuickCapture/Remove-Startup.cmd') $Output
    Copy-Item (Join-Path $PSScriptRoot 'QuickCapture/TEST-PLAN.md') $Output
    '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"></ResourceDictionary>' | Set-Content (Join-Path $Output 'Settings.xaml')
    $exe = Join-Path $Output 'ScreenToGif.exe'
    if (!(Test-Path $exe)) { throw 'Application executable missing.' }
    & (Join-Path $ffDest 'ffmpeg.exe') -version
    if ($LASTEXITCODE -ne 0) { throw 'FFmpeg failed to run.' }
    & (Join-Path $PSScriptRoot 'QuickCapture/Test-Encoding.ps1') -PortableFolder $Output
    Write-Host "Portable output: $Output"
} finally { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
