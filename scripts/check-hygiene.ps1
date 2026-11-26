<#
.SYNOPSIS
Deterministic repository hygiene guardrail for Sicurezza Immersiva 81.

.DESCRIPTION
Checks Git-only rules (no collab-proxy / Plastic), banned VR APIs in scenario scripts,
Unity .meta pairing, and accidental edits to protected Shared / XR / ProjectSettings files.
Used by .githooks/pre-commit (-StagedOnly) and standalone verification.
#>
[CmdletBinding()]
param(
    [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
Push-Location $repoRoot
try {
    $failures = [System.Collections.Generic.List[string]]::new()

    # 1. Git-only: no Unity Version Control (com.unity.collab-proxy or Plastic workspace files)
    $collabHits = @(git grep -n "collab-proxy" -- Packages/manifest.json Packages/packages-lock.json 2>$null)
    if ($collabHits.Count -gt 0) {
        $failures.Add("Unity Version Control (collab-proxy) detected in Packages/: $($collabHits -join '; ')")
    }

    $plasticFiles = @(git ls-files -- '.plastic/*' 'plastic.workspace*' 'ignore.conf' 2>$null)
    if ($plasticFiles.Count -gt 0) {
        $failures.Add("Plastic/UVCS workspace files tracked: $($plasticFiles -join ', ')")
    }

    # 2. No OVRInput or XR Interaction Toolkit in scenario runtime/editor scripts
    $bannedPattern = '\b(OVRInput|XRGrabInteractable|XRRayInteractable|XRInteractionManager)\b|UnityEngine\.XR\.Interaction'
    $scriptFiles = @(git ls-files -- 'Assets/_Project/Groups/*/Code/Scripts/*.cs' 'Assets/_Project/Groups/*/Code/Scripts/**/*.cs' 2>$null)
    foreach ($file in $scriptFiles) {
        if (-not (Test-Path -LiteralPath $file)) { continue }
        $lineNum = 0
        foreach ($line in [System.IO.File]::ReadLines((Join-Path $repoRoot $file))) {
            $lineNum++
            $trimmed = $line.TrimStart()
            if ($trimmed.StartsWith('//') -or $trimmed.StartsWith('*')) { continue }
            if ($line -match $bannedPattern) {
                $failures.Add("Banned VR API in ${file}:${lineNum}: $($trimmed)")
            }
        }
    }

    # 3. No auto-generated Meta XR overlay settings committed
    $ovrOverlay = @(git ls-files -- 'Assets/Resources/OVROverlayCanvasSettings.asset*' 2>$null)
    if ($ovrOverlay.Count -gt 0) {
        $failures.Add("Auto-generated Meta XR file must not be tracked: $($ovrOverlay -join ', ')")
    }

    if ($StagedOnly) {
        $stagedEntries = @(git diff --cached --name-status 2>$null)
        $stagedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $stagedEntries) {
            if ([string]::IsNullOrWhiteSpace($entry)) { continue }
            $parts = $entry -split "`t"
            if ($parts.Length -ge 2) { [void]$stagedPaths.Add($parts[-1]) }
        }

        $allowProtected = ($env:SI81_ALLOW_PROTECTED_EDIT -eq '1')

        foreach ($entry in $stagedEntries) {
            if ([string]::IsNullOrWhiteSpace($entry)) { continue }
            $parts = $entry -split "`t"
            $status = $parts[0]
            $path = $parts[-1]

            # 3a. Every staged added asset under Assets/ must have a companion .meta file
            if ($status -eq 'A' -and $path -like 'Assets/*' -and $path -notlike '*.meta') {
                $metaPath = "$path.meta"
                if (-not (Test-Path -LiteralPath $metaPath) -and -not $stagedPaths.Contains($metaPath)) {
                    $failures.Add("Missing .meta file for newly staged asset: $path")
                }
            }

            if ($allowProtected) { continue }

            # 3b. Guard against silent Unity/Meta XR auto-edits outside scenario scope
            if ($path -eq 'Assets/Resources/OVROverlayCanvasSettings.asset' -or
                $path -eq 'Assets/Resources/OVROverlayCanvasSettings.asset.meta') {
                $failures.Add("Auto-generated Meta XR file staged: $path (unstage and delete)")
            }
            if ($path -eq 'ProjectSettings/ProjectSettings.asset' -or $path -like 'Assets/XR/Settings/*') {
                $failures.Add("Protected project/XR settings modified: $path (set SI81_ALLOW_PROTECTED_EDIT=1 if intentional)")
            }
            if ($path -like 'Assets/_Project/Shared/*' -and $status -ne 'A') {
                $failures.Add("Pre-existing Shared asset modified ($status): $path (copy into Groups/ first, or set SI81_ALLOW_PROTECTED_EDIT=1)")
            }
            if ($path -like 'Assets/_Project/Home/*' -and $path -notlike 'Assets/_Project/Home/UI/Data/Missions/SO_Mission_*.asset*') {
                $failures.Add("Shared Home asset modified ($status): $path (set SI81_ALLOW_PROTECTED_EDIT=1 if intentional)")
            }
        }
    }

    if ($failures.Count -gt 0) {
        Write-Host "[SI81 Hygiene] FAILED ($($failures.Count) issue(s)):" -ForegroundColor Red
        foreach ($f in $failures) { Write-Host "  - $f" -ForegroundColor Red }
        exit 1
    }

    Write-Host "[SI81 Hygiene] PASS (0 issues)" -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
