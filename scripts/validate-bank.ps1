param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference = 'Stop'
$parsed = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
if ($parsed -isnot [System.Array]) { throw 'Question bank root must be a JSON array.' }
$items = @($parsed)
if ($items.Count -eq 0) { throw 'Question bank is empty.' }
$ids = @{}
$validTypes = @('单选题','多选题','判断题','填空题','阅读理解')
foreach ($q in $items) {
    if ([string]::IsNullOrWhiteSpace($q.id) -or $ids.ContainsKey($q.id)) { throw 'Missing or duplicate question ID.' }
    $ids[$q.id] = $true
    foreach ($field in @('prompt','answer','category','source','verification')) {
        if ([string]::IsNullOrWhiteSpace($q.$field)) { throw "Missing $field in $($q.id)" }
    }
    if ($q.type -notin $validTypes) { throw "Unsupported question type: $($q.id)" }
    if ($null -eq $q.banks -or $q.banks -is [string] -or @($q.banks).Count -eq 0) { throw "Missing banks array: $($q.id)" }
    if (@($q.banks | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) { throw "Empty bank name: $($q.id)" }
    if ($q.review -isnot [bool]) { throw "review must be Boolean: $($q.id)" }
    if ($q.type -in @('单选题','多选题','阅读理解')) {
        if ($null -eq $q.options -or $q.options -is [string] -or @($q.options).Count -lt 2 -or @($q.options).Count -gt 26) { throw "Invalid options: $($q.id)" }
        if (@($q.options | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) { throw "Empty option: $($q.id)" }
        if ($q.answer -cnotmatch '^[A-Z]+$') { throw "Invalid answer: $($q.id)" }
        $letters = @($q.answer.ToCharArray())
        if (@($letters | Select-Object -Unique).Count -ne $letters.Count) { throw "Duplicate answer letter: $($q.id)" }
        if ($q.type -ne '多选题' -and $letters.Count -ne 1) { throw "Single-choice answer required: $($q.id)" }
        foreach ($letter in $letters) { if (([int]$letter - [int][char]'A') -ge @($q.options).Count) { throw "Answer outside options: $($q.id)" } }
    }
    if ($q.type -eq '判断题' -and $q.answer -notin @('正确','错误')) { throw "Invalid judgment answer: $($q.id)" }
    if ($q.type -eq '阅读理解' -and [string]::IsNullOrWhiteSpace($q.passage)) { throw "Missing reading passage: $($q.id)" }
}
Write-Output "Question bank validated: $($items.Count) questions."
