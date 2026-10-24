param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $projectRoot 'src/ReleaseDeck.Portal/wwwroot/samples'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$clips = @(
    @{ File='sample-overview.mp4'; Title='Gather your clips'; Step='01'; Seconds=24; Accent='0x91c9c3' },
    @{ File='sample-demonstration.mp4'; Title='Check the details'; Step='02'; Seconds=28; Accent='0xf0bd87' },
    @{ File='sample-wrap-up.mp4'; Title='Review the series'; Step='03'; Seconds=32; Accent='0xb8b1e0' }
)
foreach ($clip in $clips) {
    $output = Join-Path $destination $clip.File
    $fadeStart = $clip.Seconds - 1
    $filter = "drawbox=x=48:y=54:w=8:h=252:color=$($clip.Accent):t=fill,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='SAMPLE SERIES':fontsize=17:fontcolor=0xadb9c9:x=82:y=68,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='$($clip.Step)':fontsize=40:fontcolor=$($clip.Accent):x=82:y=116,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='$($clip.Title)':fontsize=32:fontcolor=white:x=82:y=186,drawtext=fontfile='C\:/Windows/Fonts/arial.ttf':text='A small clip for trying Release Deck':fontsize=17:fontcolor=0xadb9c9:x=82:y=266,fade=t=in:st=0:d=0.5,fade=t=out:st=$fadeStart`:d=0.5"
    & ffmpeg -n -hide_banner -loglevel error -f lavfi -i 'color=c=0x152133:s=640x360:r=12' -t $clip.Seconds -vf $filter -an -c:v libx264 -preset medium -crf 25 -pix_fmt yuv420p -movflags +faststart $output
    if ($LASTEXITCODE -ne 0) { throw "Sample generation failed: $($clip.File)" }
    Write-Output "$($clip.File): $($clip.Seconds) seconds"
}
