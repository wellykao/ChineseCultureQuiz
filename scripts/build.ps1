param([string]$QuestionBank)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $QuestionBank) { $QuestionBank = Join-Path $repoRoot 'data\questions.json' }
$QuestionBank = (Resolve-Path -LiteralPath $QuestionBank).Path
& (Join-Path $PSScriptRoot 'validate-bank.ps1') -Path $QuestionBank
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$outputFolder = Join-Path $repoRoot 'dist'
[void](New-Item -ItemType Directory -Force -Path $outputFolder)
$outputFile = Join-Path $outputFolder 'ChineseCultureQuiz.exe'
$sourceFolder = Join-Path $repoRoot 'src'
$sources = @('CultureQuiz.cs','QuizCore.cs','QuizExtras.cs','QuizNotebook.cs','StudyOperations.cs') | ForEach-Object { Join-Path $sourceFolder $_ }
$manifest = Join-Path $sourceFolder 'app.manifest'
& $compiler /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/win32manifest:$manifest" "/resource:$QuestionBank,questions.json" "/out:$outputFile" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Get-Item -LiteralPath $outputFile | Select-Object FullName,Length
