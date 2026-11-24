param([Parameter(Mandatory=$true)][string]$PortableFolder)
$ErrorActionPreference='Stop'
$ff = Join-Path $PortableFolder 'FFmpeg/ffmpeg.exe'
$probe = Join-Path $PortableFolder 'FFmpeg/ffprobe.exe'
$work = Join-Path ([IO.Path]::GetTempPath()) ('qc-smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
Push-Location $work
function Check { if ($LASTEXITCODE -ne 0) { throw "Encoder smoke test failed ($LASTEXITCODE)" } }
try {
    & $ff -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=320x240:rate=30' -frames:v 3 -threads 1 'frame%02d.png'; Check
    @('ffconcat version 1.0','file frame01.png','option framerate 1000','duration 0.033','file frame02.png','option framerate 1000','duration 0.034','file frame03.png','option framerate 1000','duration 0.033','file frame03.png','option framerate 1000') | Set-Content -Encoding ascii frames.ffconcat
    & $ff -hide_banner -loglevel error -f concat -safe 0 -i frames.ffconcat -vf 'fps=30,pad=ceil(iw/2)*2:ceil(ih/2)*2' -c:v libx264 -crf 23 -pix_fmt yuv420p -an -t 0.1 silent.mp4; Check
    & $ff -hide_banner -loglevel error -f concat -safe 0 -i frames.ffconcat -filter_complex '[0:v]fps=15,split[a][b];[a]palettegen=max_colors=256[p];[b][p]paletteuse=dither=sierra2_4a' -loop 0 -an -t 0.1 test.gif; Check
    & $ff -hide_banner -loglevel error -f lavfi -i 'sine=frequency=440:sample_rate=48000' -t 0.1 audio.wav; Check
    & $ff -hide_banner -loglevel error -f concat -safe 0 -i frames.ffconcat -i audio.wav -map 0:v:0 -map 1:a:0 -vf 'fps=30,pad=ceil(iw/2)*2:ceil(ih/2)*2' -c:v libx264 -pix_fmt yuv420p -c:a aac -af apad -t 0.1 audio.mp4; Check
    foreach ($file in @('silent.mp4','test.gif','audio.mp4')) {
        if (!(Test-Path $file) -or (Get-Item $file).Length -eq 0) { throw "Missing or empty output $file" }
        $json = & $probe -v error -show_streams -of json $file | Out-String; Check
        $streams = ($json | ConvertFrom-Json).streams
        if (!($streams | Where-Object codec_type -eq 'video')) { throw "No video: $file" }
        $hasAudio = [bool]($streams | Where-Object codec_type -eq 'audio')
        if ($hasAudio -ne ($file -eq 'audio.mp4')) { throw "Incorrect audio: $file" }
    }
    Write-Host 'Synthetic MP4/GIF/audio encoder smoke tests passed.'
} finally { Pop-Location; Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
