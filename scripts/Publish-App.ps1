param([string]$Destination='', [string]$BasePath='/release-deck/', [switch]$NoRestore)
$ErrorActionPreference='Stop'
if($BasePath -notin @('/','/release-deck/')){throw 'Use / or /release-deck/ as the application base.'}
$root=Split-Path -Parent $PSScriptRoot
$output=Join-Path $root 'publish/app'
$target=if($Destination){[IO.Path]::GetFullPath($Destination)}else{Join-Path $root 'publish/site'}
$arguments=@('publish',(Join-Path $root 'src/ReleaseDeck.Portal/ReleaseDeck.Portal.csproj'),'-c','Release','-o',$output,'-p:WasmEnableWebcil=true')
if($NoRestore){$arguments+='--no-restore'}
& dotnet @arguments
if($LASTEXITCODE -ne 0){throw 'The application did not publish.'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
foreach($item in Get-ChildItem -LiteralPath (Join-Path $output 'wwwroot')){Copy-Item -LiteralPath $item.FullName -Destination $target -Recurse -Force}
$indexPath=Join-Path $target 'index.html'
$index=[IO.File]::ReadAllText($indexPath).Replace('<base href="/release-deck/">',('<base href="'+$BasePath+'">'))
[IO.File]::WriteAllText($indexPath,$index)
$bytes=[Text.Encoding]::UTF8.GetBytes($index)
foreach($encoding in @('gzip','brotli')){
    $memory=[IO.MemoryStream]::new()
    $stream=if($encoding -eq 'gzip'){[IO.Compression.GZipStream]::new($memory,[IO.Compression.CompressionLevel]::SmallestSize,$true)}else{[IO.Compression.BrotliStream]::new($memory,[IO.Compression.CompressionLevel]::SmallestSize,$true)}
    $stream.Write($bytes);$stream.Dispose();[IO.File]::WriteAllBytes($indexPath+$(if($encoding -eq 'gzip'){'.gz'}else{'.br'}),$memory.ToArray());$memory.Dispose()
}
$redirects=[Collections.Generic.List[string]]::new()
if($BasePath -ne '/'){$redirects.Add($BasePath.TrimEnd('/')+' '+$BasePath+' 308')}
# Pages redirects index.html to its canonical folder even when reached by a 200 proxy.
# Keep the browser's deep URL by proxying to that folder, without a self-proxy.
foreach($route in @('workspace','workspace/:id','review','review/:id','operations','operations/:id','help')){
    $redirects.Add($BasePath+$route+' '+$BasePath+' 200')
    $redirects.Add($BasePath+$route+'/ '+$BasePath+' 200')
}
$redirects | Set-Content -LiteralPath (Join-Path $target '_redirects') -Encoding utf8
@"
$BasePath*
  Referrer-Policy: no-referrer
  X-Content-Type-Options: nosniff
  X-Frame-Options: DENY
  Content-Security-Policy: default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; media-src 'self' blob:; connect-src 'self' https://www.googleapis.com https://youtube.googleapis.com; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'
"@ | Set-Content -LiteralPath (Join-Path $target '_headers') -Encoding utf8
$commit=(& git -C $root rev-parse HEAD).Trim()
@{service='Release Deck';commit=$commit;baseHref=$BasePath;apiBase='/api/commerce/release-deck/';builtAtUtc=[DateTimeOffset]::UtcNow.ToString('O');appearance=@{themeAttribute='data-rd-theme';styleMarker='data-rd-appearance=style';scriptMarker='data-rd-appearance=script';auditGlobal='ReleaseDeckAppearance.audit'}} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $target 'build-info.json') -Encoding utf8
Write-Output ('Release Deck published to '+$target+'. Merge its scoped hosting metadata at the main host.')
