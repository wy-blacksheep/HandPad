$ErrorActionPreference = 'Stop'
# Run in a fresh PowerShell process; no Unity or licensed PMML installation needed.
$releaseRoot = Split-Path -Parent $PSScriptRoot
$classifierPath = Join-Path $releaseRoot 'unity/Assets/Scripts/HandInteraction/TouchGestureInteraction/TouchClassifier.cs'
$harnessPath = Join-Path $PSScriptRoot 'TouchClassifierBufferTests.cs'
Add-Type -Path @($classifierPath, $harnessPath)
[TouchClassifierBufferTests]::Run()
