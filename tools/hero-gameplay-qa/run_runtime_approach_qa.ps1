param()
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sourceProject = Join-Path $repoRoot 'UnityProject/BigBonusBlitz'
$stamp = Get-Date -Format 'yyyy-MM-dd'
$baseOutputDir = Join-Path $PSScriptRoot "$stamp/runtime-approach"
$outputDir = $baseOutputDir
$attempt = 2
while(Test-Path $outputDir) { $outputDir = "$baseOutputDir-attempt$('{0:D2}' -f $attempt)"; $attempt++ }
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$qaRunId = [Guid]::NewGuid().ToString('N')
$qaProject = Join-Path $env:TEMP "bbb-runtime-approach-$PID-$qaRunId/work/shop-qa"
New-Item -ItemType Directory -Force -Path $qaProject,(Join-Path $qaProject 'Assets'),(Join-Path $qaProject 'Packages') | Out-Null
foreach($folder in @('Scripts','Resources','Editor','Tests')) {
    Copy-Item -LiteralPath (Join-Path $sourceProject "Assets/$folder") -Destination (Join-Path $qaProject 'Assets') -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $sourceProject 'ProjectSettings') -Destination $qaProject -Recurse
$settings = Join-Path $qaProject 'ProjectSettings/ProjectSettings.asset'
$settingsText = Get-Content -Raw $settings
$settingsText = [regex]::Replace($settingsText,'(?m)^  productName: .*$', '  productName: shop-qa', 1)
[IO.File]::WriteAllText($settings,$settingsText,(New-Object Text.UTF8Encoding($false)))
$dependencies = [ordered]@{}
$sourceManifest = Get-Content -Raw (Join-Path $sourceProject 'Packages/manifest.json') | ConvertFrom-Json
foreach($entry in $sourceManifest.dependencies.PSObject.Properties) {
    if($entry.Name.StartsWith('com.unity.modules.')) { $dependencies[$entry.Name]=$entry.Value }
}
foreach($packageName in @('com.unity.ugui','com.unity.inputsystem','com.unity.nuget.newtonsoft-json','com.unity.test-framework','com.unity.ext.nunit')) {
    $cachedPackage = Get-ChildItem (Join-Path $sourceProject 'Library/PackageCache') -Directory | Where-Object { $_.Name.StartsWith($packageName+'@') } | Select-Object -First 1
    if(-not $cachedPackage) { throw "Missing local package $packageName" }
    $dependencies[$packageName]='file:'+($cachedPackage.FullName.Replace('\','/'))
}
$manifestText = @{dependencies=$dependencies} | ConvertTo-Json -Depth 10
[IO.File]::WriteAllText((Join-Path $qaProject 'Packages/manifest.json'),$manifestText,(New-Object Text.UTF8Encoding($false)))
Set-Content -LiteralPath (Join-Path $qaProject '.bbb-isolated-qa') -Value 'Source-isolated P2 approach validation; no player saves copied.'
$unityCandidates = @(
    'C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe',
    'D:/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe'
)
$unity = $unityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if(-not $unity) { throw 'Unity 6000.3.15f1 is required.' }
$env:HERO_GAMEPLAY_OUTPUT = $outputDir
$env:HERO_ENEMY_HEIGHT_RULE = 'runtime-data'
$env:ENEMY_REEL_OUTPUT = Join-Path $outputDir 'real-enemy-reel'
$env:ENEMY_GAMEPLAY_OUTPUT = Join-Path $outputDir 'enemy-gameplay'
$env:HERO_JOURNEY_OUTPUT = Join-Path $outputDir 'hero-journey'
foreach($path in @($env:ENEMY_REEL_OUTPUT,$env:ENEMY_GAMEPLAY_OUTPUT,$env:HERO_JOURNEY_OUTPUT)) { New-Item -ItemType Directory -Force -Path $path | Out-Null }
$manifestFiles = @(
    'Assets/Editor/AdventureHeroValidation.cs',
    'Assets/Scripts/BBB.Runtime/MarqueeBand.cs',
    'Assets/Scripts/BBB.Runtime/AdventureHeroPresenter.cs',
    'Assets/Scripts/BBB.Runtime/AdventurePresentation.cs',
    'Assets/Scripts/BBB.Runtime/GameController.cs',
    'Assets/Tests/EditMode/AdventureEnemyReelTests.cs',
    'Assets/Resources/Data/hero_presentation_prototype.json',
    'Assets/Resources/Data/enemy_presentation_prototype.json'
)
$manifest = [ordered]@{ generatedAt=(Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK'); isolatedProductName='shop-qa'; files=@() }
foreach($relative in $manifestFiles) {
    $source = Join-Path $sourceProject $relative
    $isolated = Join-Path $qaProject $relative
    $manifest.files += [ordered]@{ path=$relative.Replace('\','/'); sourceSha256=(Get-FileHash -Algorithm SHA256 $source).Hash.ToLowerInvariant(); isolatedSha256=(Get-FileHash -Algorithm SHA256 $isolated).Hash.ToLowerInvariant(); identical=((Get-FileHash -Algorithm SHA256 $source).Hash -eq (Get-FileHash -Algorithm SHA256 $isolated).Hash) }
}
$manifest.passed = @($manifest.files | Where-Object { -not $_.identical }).Count -eq 0
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputDir 'source-manifest.json')
if(-not $manifest.passed) { throw "Source/isolated manifest mismatch; see $(Join-Path $outputDir 'source-manifest.json')" }
$approachLog = Join-Path $outputDir 'approach-unity.log'
$approachArgs = @('-batchmode','-quit','-projectPath',('"'+$qaProject+'"'),'-executeMethod','AdventureHeroValidation.RunApproachComparison','-logFile',('"'+$approachLog+'"'))
$p = Start-Process -FilePath $unity -ArgumentList $approachArgs -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0) { throw "Approach comparison Unity exit=$($p.ExitCode); see $approachLog" }
$attackSequenceLog = Join-Path $outputDir 'attack-sequence-unity.log'
$attackSequenceArgs = @('-batchmode','-quit','-projectPath',('"'+$qaProject+'"'),'-executeMethod','AdventureHeroValidation.RunAttackSequenceComparison','-logFile',('"'+$attackSequenceLog+'"'))
$p = Start-Process -FilePath $unity -ArgumentList $attackSequenceArgs -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0) { throw "Full BET attack sequence Unity exit=$($p.ExitCode); see $attackSequenceLog" }
$intensityOutput = Join-Path $outputDir 'combat-intensity'
New-Item -ItemType Directory -Force -Path $intensityOutput | Out-Null
$env:HERO_GAMEPLAY_OUTPUT = $intensityOutput
$intensityLog = Join-Path $outputDir 'combat-intensity-unity.log'
$intensityArgs = @('-batchmode','-quit','-projectPath',('"'+$qaProject+'"'),'-executeMethod','AdventureHeroValidation.RunCombatIntensityValidation','-logFile',('"'+$intensityLog+'"'))
$p = Start-Process -FilePath $unity -ArgumentList $intensityArgs -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0) { throw "Combat intensity Unity exit=$($p.ExitCode); see $intensityLog" }
$env:HERO_GAMEPLAY_OUTPUT = $outputDir
$testLog = Join-Path $outputDir 'editmode-unity.log'
$testXml = Join-Path $outputDir 'editmode-results.xml'
$testArgs = @('-batchmode','-projectPath',('"'+$qaProject+'"'),'-runTests','-testPlatform','EditMode','-testFilter','AdventureEnemyReelTests.ReelResultsOwnBattleActions','-testResults',('"'+$testXml+'"'),'-logFile',('"'+$testLog+'"'))
$p = Start-Process -FilePath $unity -ArgumentList $testArgs -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0) { throw "Runtime reel regression Unity exit=$($p.ExitCode); see $testLog" }
$timeline = Join-Path $env:ENEMY_REEL_OUTPUT 'battle-timeline.csv'
$timelineSummary = Join-Path $env:ENEMY_REEL_OUTPUT 'battle-timing-summary.json'
& py -3 (Join-Path $PSScriptRoot 'summarize_battle_timing.py') $timeline
if($LASTEXITCODE -ne 0) { throw "Battle timeline summary failed; see $timeline" }
@{passed=$true;isolatedProject=$qaProject;approachExit=0;attackSequenceExit=0;combatIntensityExit=0;editModeExit=0;sourceManifestPassed=$manifest.passed;approachValidation=(Test-Path (Join-Path $outputDir 'validation.json'));attackSequenceValidation=(Test-Path (Join-Path $outputDir 'attack-sequence-validation.json'));combatIntensityValidation=(Test-Path (Join-Path $outputDir 'combat-intensity/validation.json'));battleTimeline=(Test-Path $timeline);battleTimingSummary=(Test-Path $timelineSummary);editModeResults=$testXml} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDir 'run-summary.json')
Get-Content (Join-Path $outputDir 'validation.json')
Write-Output "Isolated P2 approach QA passed; evidence: $outputDir"
