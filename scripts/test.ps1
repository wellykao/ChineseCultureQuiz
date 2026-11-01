$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$binary = Join-Path $repoRoot 'dist\ChineseCultureQuiz.exe'
$testRoot = Join-Path $repoRoot 'test-output'
[void](New-Item -ItemType Directory -Force -Path $testRoot)
$testFolder = Join-Path $testRoot ([Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testFolder)
$process = Start-Process -FilePath $binary -ArgumentList @('--self-test',('"' + $testFolder + '"')) -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw "Self-test timed out: $testFolder" }
if ($process.ExitCode -ne 0) { throw "Self-test failed: $testFolder" }
$resultPath = Join-Path $testFolder 'test-results.json'
if (-not (Test-Path -LiteralPath $resultPath)) { throw 'Self-test produced no result.' }
$result = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $result.passed -or $result.questions -ne 2394 -or @($result.checks).Count -lt 76) { throw 'Self-test result is incomplete.' }
Write-Output "Passed $(@($result.checks).Count) checks against $($result.questions) full-bank questions."
Write-Output "Results: $resultPath"
