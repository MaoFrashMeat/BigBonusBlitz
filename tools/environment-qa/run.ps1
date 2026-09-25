param([switch]$Tests)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sourceProject = Join-Path $repoRoot 'UnityProject/BigBonusBlitz'
$qaProject = Join-Path $env:TEMP 'bbb-environment-c1-qa'
$outputDir = Join-Path $PSScriptRoot 'captures'
New-Item -ItemType Directory -Force -Path $qaProject,$outputDir,(Join-Path $qaProject 'Assets'),(Join-Path $qaProject 'Packages') | Out-Null
# A separate copy leaves the open Unity project, its Library, and player saves alone.
foreach($folder in @('Scripts','Resources','Editor','Tests')) {
    Copy-Item -LiteralPath (Join-Path $sourceProject "Assets/$folder") -Destination (Join-Path $qaProject 'Assets') -Recurse -Force
}
if(-not (Test-Path (Join-Path $qaProject 'ProjectSettings'))) {
    Copy-Item -LiteralPath (Join-Path $sourceProject 'ProjectSettings') -Destination $qaProject -Recurse
}
$dependencies = [ordered]@{}
$sourceManifest = Get-Content -Raw (Join-Path $sourceProject 'Packages/manifest.json') | ConvertFrom-Json
foreach($entry in $sourceManifest.dependencies.PSObject.Properties) {
    if($entry.Name.StartsWith('com.unity.modules.')) { $dependencies[$entry.Name]=$entry.Value }
}
foreach($packageName in @('com.unity.ugui','com.unity.inputsystem','com.unity.nuget.newtonsoft-json','com.unity.test-framework','com.unity.ext.nunit')) {
    $cachedPackage = Get-ChildItem (Join-Path $sourceProject 'Library/PackageCache') -Directory | Where-Object { $_.Name.StartsWith($packageName+'@') } | Select-Object -First 1
    if(-not $cachedPackage) { throw "Missing local package $packageName" }
    $dependencies[$packageName]='file:'+($cachedPackage.FullName -replace '\\','/')
}
$manifestText = @{dependencies=$dependencies} | ConvertTo-Json -Depth 10
[IO.File]::WriteAllText((Join-Path $qaProject 'Packages/manifest.json'),$manifestText,(New-Object Text.UTF8Encoding($false)))
Set-Content -LiteralPath (Join-Path $qaProject '.bbb-environment-qa') -Value 'isolated environment validation copy'
$env:ENVIRONMENT_OUTPUT=$outputDir
$unityExecutable = 'D:/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe'
if(-not (Test-Path $unityExecutable)) { throw 'Unity 6000.3.15f1 is required. Update this local executable path.' }
$unityArgs = @('-batchmode','-quit','-projectPath',('"'+$qaProject+'"'),'-executeMethod','AdventureEnvironmentValidation.Run','-logFile',('"'+(Join-Path $outputDir 'unity.log')+'"'))
if($Tests) {
    $unityArgs = @('-batchmode','-projectPath',('"'+$qaProject+'"'),'-runTests','-testPlatform','EditMode','-testResults',('"'+(Join-Path $outputDir 'editmode.xml')+'"'),'-logFile',('"'+(Join-Path $outputDir 'tests.log')+'"'))
}
$unityProcess = Start-Process -FilePath $unityExecutable -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
@{pid=$unityProcess.Id;project=$qaProject;output=$outputDir} | ConvertTo-Json
