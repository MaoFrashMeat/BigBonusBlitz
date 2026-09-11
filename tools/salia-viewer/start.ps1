$ErrorActionPreference = 'Stop'
$saliaUrl = 'http://127.0.0.1:4178/tools/salia-viewer/'
$saliaReady = $false
try { $saliaResponse = Invoke-WebRequest -Uri $saliaUrl -UseBasicParsing -TimeoutSec 2; $saliaReady = $saliaResponse.StatusCode -eq 200 -and $saliaResponse.Content.Contains('Motion Atelier') } catch {}
if (-not $saliaReady) {
    $saliaNode = (Get-Command node -ErrorAction Stop).Source
    Start-Process -FilePath $saliaNode -ArgumentList ('"' + (Join-Path $PSScriptRoot 'server.cjs') + '"') -WorkingDirectory $PSScriptRoot -WindowStyle Hidden
    for ($saliaAttempt=0; $saliaAttempt -lt 20; $saliaAttempt++) {
        Start-Sleep -Milliseconds 250
        try { $saliaResponse = Invoke-WebRequest -Uri $saliaUrl -UseBasicParsing -TimeoutSec 1; if ($saliaResponse.StatusCode -eq 200 -and $saliaResponse.Content.Contains('Motion Atelier')) { $saliaReady=$true; break } } catch {}
    }
}
if (-not $saliaReady) { throw 'Salia viewer could not start. Check whether port 4178 is in use.' }
Start-Process $saliaUrl
