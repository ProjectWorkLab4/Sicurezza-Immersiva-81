<#
.SYNOPSIS
Unified Unity batchmode and Quest 3 ADB runner for Sicurezza Immersiva 81.

.DESCRIPTION
Wraps Windows Start-Process -Wait -PassThru, resolves the exact Unity Editor and ADB binaries
for ProjectSettings/ProjectVersion.txt, enforces no -nographics for PlayMode/preview/APK runs
on Windows OpenXR, emits compact summaries, and auto-restores known Unity URP -nographics
auto-upgrade noise on untouched Shared materials.

.EXAMPLE
.\scripts\unity-run.ps1 validate
.\scripts\unity-run.ps1 playmode
.\scripts\unity-run.ps1 capture-views
.\scripts\unity-run.ps1 deploy-apk -Serial 2G0YC5ZG1402NV
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet(
        'build-scene', 'validate', 'score-tests',
        'editmode', 'playmode', 'rig-tests',
        'inspect-scene', 'capture-views',
        'build-apk', 'deploy-apk', 'hygiene'
    )]
    [string]$Command,

    [string]$Serial = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
$logsDir = Join-Path $repoRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

if ($Command -eq 'hygiene') {
    & (Join-Path $repoRoot 'scripts\check-hygiene.ps1')
    exit $LASTEXITCODE
}

function Resolve-UnityVersion {
    $verFile = Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt'
    $match = Select-String -Path $verFile -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if (-not $match) { throw "Could not read m_EditorVersion from $verFile" }
    return $match.Matches[0].Groups[1].Value
}

function Resolve-UnityExe([string]$version) {
    if ($env:UNITY_EXE -and (Test-Path -LiteralPath $env:UNITY_EXE)) { return $env:UNITY_EXE }
    $candidates = @(
        "D:\$version\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\Program Files\Unity\Editor\Unity.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { return $c }
    }
    throw "Unity $version not found. Set UNITY_EXE environment variable."
}

function Resolve-AdbExe([string]$version) {
    if ($env:ADB_EXE -and (Test-Path -LiteralPath $env:ADB_EXE)) { return $env:ADB_EXE }
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
        "D:\$version\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { return $c }
    }
    $onPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    throw "adb.exe not found. Set ADB_EXE environment variable."
}

$unityVersion = Resolve-UnityVersion

if ($Command -eq 'deploy-apk') {
    $adb = Resolve-AdbExe $unityVersion
    $apk = Join-Path $repoRoot 'Builds\SI81_MachineLock_Quest3.apk'
    if (-not (Test-Path -LiteralPath $apk)) { throw "APK not found: $apk (run build-apk first)" }
    if ([string]::IsNullOrWhiteSpace($Serial)) {
        $devLines = @(& $adb devices | Select-String '\tdevice$')
        if ($devLines.Count -eq 0) { throw "No connected Quest device found via adb devices." }
        $Serial = ($devLines[0].Line -split '\s+')[0]
    }
    Write-Host "[SI81 Deploy] Installing $apk to $Serial..."
    & $adb -s $Serial install -r $apk
    exit $LASTEXITCODE
}

$unityExe = Resolve-UnityExe $unityVersion

# Track whether known URP-auto-touched Shared materials were clean before Unity runs
$noisySharedMats = @(
    'Assets/_Project/Shared/Art/Materials/MAT_PoufBody.mat',
    'Assets/_Project/Shared/Art/Materials/MAT_RoadSurface.mat',
    'Assets/_Project/Shared/Art/Materials/MAT_SofaBody.mat'
)
$cleanBefore = [System.Collections.Generic.List[string]]::new()
Push-Location $repoRoot
try {
    foreach ($m in $noisySharedMats) {
        $st = git status --porcelain -- $m 2>$null
        if ([string]::IsNullOrWhiteSpace($st)) { $cleanBefore.Add($m) }
    }
}
finally { Pop-Location }

function Invoke-UnityBatch {
    param(
        [string[]]$ExtraArgs,
        [string]$LogName,
        [bool]$UseNoGraphics = $true
    )
    $lockFile = Join-Path $repoRoot 'Temp\UnityLockfile'
    if (Test-Path -LiteralPath $lockFile) {
        $running = @(Get-Process Unity -ErrorAction SilentlyContinue)
        if ($running.Count -gt 0) {
            Write-Host "[SI81 Unity] Project is locked by a running Unity Editor (PID $($running[0].Id): '$($running[0].MainWindowTitle)'). Close the Editor before running batchmode." -ForegroundColor Yellow
        }
    }
    $logPath = Join-Path $logsDir $LogName
    $argList = [System.Collections.Generic.List[string]]::new()
    $argList.Add('-batchmode')
    if ($UseNoGraphics) { $argList.Add('-nographics') }
    $argList.Add('-projectPath')
    $argList.Add($repoRoot)
    foreach ($a in $ExtraArgs) { $argList.Add($a) }
    $argList.Add('-logFile')
    $argList.Add($logPath)

    $proc = Start-Process -FilePath $unityExe -ArgumentList $argList.ToArray() -Wait -PassThru
    return @{ ExitCode = $proc.ExitCode; LogPath = $logPath }
}

function Print-FilteredOrTail([string]$logPath, [string]$pattern, [int]$exitCode, [int]$lastN = 40) {
    $hits = @(Select-String -Path $logPath -Pattern $pattern -ErrorAction SilentlyContinue | Select-Object -Last $lastN)
    foreach ($h in $hits) { Write-Host $h.Line }
    if ($exitCode -ne 0 -and $hits.Count -eq 0 -and (Test-Path -LiteralPath $logPath)) {
        Write-Host "[SI81 Unity] Exited with code $exitCode. Tail of ${logPath}:" -ForegroundColor Red
        Get-Content -LiteralPath $logPath -Tail 15 | ForEach-Object { Write-Host "  $_" }
    }
}

function Print-NUnitSummary([string]$xmlPath, [string]$label) {
    if (-not (Test-Path -LiteralPath $xmlPath)) {
        Write-Host "[$label] XML results missing: $xmlPath" -ForegroundColor Red
        return
    }
    [xml]$doc = Get-Content -LiteralPath $xmlPath
    $run = $doc.'test-run'
    Write-Host "[$label] total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped) duration=$($run.duration)s"
    if ([int]$run.failed -gt 0) {
        $doc.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object {
            Write-Host "  FAIL: $($_.fullname) -> $($_.failure.message.'#cdata-section')" -ForegroundColor Red
        }
    }
}

$exitCode = 0
try {
    switch ($Command) {
        'build-scene' {
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockSceneBuilder.BuildFullScene') 'unity-run-build.log' $true
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[MachineLock\]|error CS|Exception' $exitCode 20
        }
        'validate' {
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockValidatorRunner.ValidateScene') 'unity-run-validate.log' $true
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[MachineLockValidator\]|error CS|Exception' $exitCode 40
        }
        'score-tests' {
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockDebugMenu.RunScoreTests') 'unity-run-score.log' $true
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[MachineLockDebug\] (PASS:|FAIL:)|error CS|Exception' $exitCode 20
        }
        'editmode' {
            $xml = Join-Path $logsDir 'unity-run-editmode.xml'
            $res = Invoke-UnityBatch @('-runTests', '-testPlatform', 'EditMode', '-testFilter', 'SI81.MachineLock.Tests', '-testResults', $xml) 'unity-run-editmode.log' $true
            $exitCode = $res.ExitCode
            Print-NUnitSummary $xml 'EditMode'
        }
        'playmode' {
            $xml = Join-Path $logsDir 'unity-run-playmode.xml'
            # Omit -nographics: Windows Meta Horizon OpenXR throws XR_ERROR_GRAPHICS_DEVICE_INVALID under -nographics
            $res = Invoke-UnityBatch @('-runTests', '-testPlatform', 'PlayMode', '-assemblyNames', 'SI81.MachineLock.PlayModeTests', '-testResults', $xml) 'unity-run-playmode.log' $false
            $exitCode = $res.ExitCode
            Print-NUnitSummary $xml 'PlayMode'
        }
        'rig-tests' {
            $xml = Join-Path $logsDir 'unity-run-rig.xml'
            $res = Invoke-UnityBatch @('-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'SI81.Rig.Tests', '-testResults', $xml) 'unity-run-rig.log' $false
            $exitCode = $res.ExitCode
            Print-NUnitSummary $xml 'RigTests'
        }
        'inspect-scene' {
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockAuditCli.InspectScene') 'unity-run-inspect.log' $true
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[MachineLockAudit\]|error CS|Exception' $exitCode 40
        }
        'capture-views' {
            # Omit -nographics: offscreen camera preview rendering requires GPU device
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockAuditCli.CaptureSceneViews') 'unity-run-previews.log' $false
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[MachineLockAudit\]|error CS|Exception' $exitCode 20
        }
        'build-apk' {
            $res = Invoke-UnityBatch @('-quit', '-executeMethod', 'SI81.MachineLock.EditorTools.MachineLockDebugMenu.BuildQuest3Apk') 'unity-run-apk.log' $false
            $exitCode = $res.ExitCode
            Print-FilteredOrTail $res.LogPath '\[SI81 Build\]|AndroidBuild|BuildFailedException|error CS' $exitCode 20
        }
    }
}
finally {
    if ($cleanBefore.Count -gt 0) {
        Push-Location $repoRoot
        try {
            git checkout -- @($cleanBefore.ToArray()) 2>$null
        }
        finally { Pop-Location }
    }
}

exit $exitCode
