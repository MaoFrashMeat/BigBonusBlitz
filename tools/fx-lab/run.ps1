# エフェクト見本を描き出す。本体プロジェクトには触らず、%TEMP%\bbb-fx-lab に作業用の Unity プロジェクトを作る
#   .\tools\fx-lab\run.ps1                 12 本すべて → 連番 PNG・各 mp4・reel.mp4
#   .\tools\fx-lab\run.ps1 -Only slash1,coins
#   .\tools\fx-lab\run.ps1 -Spark          火花の見本（暗い工場の床）だけ
# 本体を Unity で開いたままでも動く（別プロジェクトなので）
param([string[]]$Only = @(), [switch]$Spark, [switch]$NoVideo, [string]$Unity = 'D:/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $here '../..')).Path
$src = Join-Path $repo 'UnityProject/BigBonusBlitz'
$lab = Join-Path $env:TEMP 'bbb-fx-lab'
$work = Join-Path $lab 'work'
New-Item -ItemType Directory -Force -Path $lab, $work, (Join-Path $lab 'Packages') | Out-Null

# プロジェクト設定は本体から（色空間 Linear など）。パッケージは組み込みモジュールだけ
if (-not (Test-Path (Join-Path $lab 'ProjectSettings'))) { Copy-Item -LiteralPath (Join-Path $src 'ProjectSettings') -Destination $lab -Recurse }
$m = Get-Content -Raw (Join-Path $src 'Packages/manifest.json') | ConvertFrom-Json
$deps = [ordered]@{}
foreach ($e in $m.dependencies.PSObject.Properties) { if ($e.Name.StartsWith('com.unity.modules.')) { $deps[$e.Name] = $e.Value } }
[IO.File]::WriteAllText((Join-Path $lab 'Packages/manifest.json'), (@{ dependencies = $deps } | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
Copy-Item -Path (Join-Path $here 'unity/Assets') -Destination $lab -Recurse -Force

py -3 (Join-Path $here 'compose_art.py') (Join-Path $work 'art')
if ($LASTEXITCODE -ne 0) { throw 'compose_art.py failed' }

$unity = $Unity
if (-not (Test-Path $unity)) { throw "Unity 6000.3.15f1 が見つからない: $unity を -Unity で渡す" }
if ($Spark) {
    $env:SPARK_OUT = Join-Path $work 'spark'
    $method = 'SparkLab.Run'
} else {
    $env:LAB_OUT = Join-Path $work 'fx'; $env:LAB_ART = Join-Path $work 'art'; $env:LAB_ONLY = ($Only -join ',')
    $method = 'FxLab.Run'
}
$log = Join-Path $work 'unity.log'
$p = Start-Process -FilePath $unity -ArgumentList @('-batchmode', '-projectPath', ('"' + $lab + '"'), '-executeMethod', $method, '-logFile', ('"' + $log + '"')) -WindowStyle Hidden -PassThru -Wait
if ($p.ExitCode -ne 0) { Get-Content $log -Tail 40; throw "Unity exit $($p.ExitCode)" }
Select-String -Path $log -Pattern 'FxLab: |SparkLab: ' | ForEach-Object Line

if (-not $NoVideo) {
    if ($Spark) {
        ffmpeg -y -loglevel error -framerate 60 -i (Join-Path $work 'spark/f_%04d.png') -c:v libx264 -pix_fmt yuv420p -crf 16 -movflags +faststart (Join-Path $work 'spark.mp4')
    } else {
        py -3 (Join-Path $here 'reel.py') $work
    }
}
"出力: $work"
