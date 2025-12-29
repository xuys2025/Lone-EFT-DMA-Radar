param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutFile = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'Resources\lang\zh-CN.todo.json'),
    [string]$SourceDir = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'src')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Decode-CSharpString([string]$s, [bool]$isVerbatim) {
    if ($isVerbatim) {
        return $s
    }

    # Basic C# escape decoding for common sequences used in UI strings.
    $s = $s -replace '\\"', '"'
    $s = $s -replace '\\\\', '\\'
    $s = $s -replace '\\n', "`n"
    $s = $s -replace '\\r', "`r"
    $s = $s -replace '\\t', "`t"
    return $s
}

function Get-VisibleImGuiLabel([string]$label) {
    if ([string]::IsNullOrEmpty($label)) { return $label }

    $triple = $label.IndexOf('###', [System.StringComparison]::Ordinal)
    if ($triple -ge 0) {
        return $label.Substring(0, $triple)
    }

    $dbl = $label.IndexOf('##', [System.StringComparison]::Ordinal)
    if ($dbl -ge 0) {
        return $label.Substring(0, $dbl)
    }

    return $label
}

function Extract-LocCalls([string]$text) {
    $results = New-Object System.Collections.Generic.List[object]

    $pattern = [regex]::new('Loc\.(T|WithId|Title)\s*\(', [System.Text.RegularExpressions.RegexOptions]::Compiled)
    $matches = $pattern.Matches($text)

    foreach ($m in $matches) {
        $callType = $m.Groups[1].Value

        # Start scanning at the '(' character.
        $i = $m.Index + $m.Length - 1
        $depth = 0
        $inNormal = $false
        $inVerbatim = $false
        $current = ''

        while ($i -lt $text.Length) {
            $ch = $text[$i]

            if ($inNormal) {
                if ($ch -eq '\\') {
                    # Keep escape sequence as-is for decoding later.
                    $current += $ch
                    $i++
                    if ($i -lt $text.Length) {
                        $current += $text[$i]
                    }
                }
                elseif ($ch -eq '"') {
                    $decoded = Decode-CSharpString $current $false
                    $results.Add([pscustomobject]@{ Type = $callType; Value = $decoded })
                    $inNormal = $false
                    $current = ''
                }
                else {
                    $current += $ch
                }
            }
            elseif ($inVerbatim) {
                if ($ch -eq '"') {
                    # "" inside verbatim strings becomes a literal quote.
                    if (($i + 1) -lt $text.Length -and $text[$i + 1] -eq '"') {
                        $current += '"'
                        $i++
                    }
                    else {
                        $decoded = Decode-CSharpString $current $true
                        $results.Add([pscustomobject]@{ Type = $callType; Value = $decoded })
                        $inVerbatim = $false
                        $current = ''
                    }
                }
                else {
                    $current += $ch
                }
            }
            else {
                if ($ch -eq '(') {
                    $depth++
                }
                elseif ($ch -eq ')') {
                    $depth--
                    if ($depth -eq 0) {
                        break
                    }
                }
                elseif ($ch -eq '@' -and ($i + 1) -lt $text.Length -and $text[$i + 1] -eq '"') {
                    $inVerbatim = $true
                    $current = ''
                    $i++ # skip the opening quote
                }
                elseif ($ch -eq '"') {
                    $inNormal = $true
                    $current = ''
                }
            }

            $i++
        }
    }

    return $results
}

Write-Host "Scanning: $SourceDir" -ForegroundColor Cyan

$allKeys = New-Object System.Collections.Generic.List[string]

Get-ChildItem -Path $SourceDir -Recurse -Filter *.cs | ForEach-Object {
    $content = Get-Content -LiteralPath $_.FullName -Raw
    $calls = Extract-LocCalls $content

    foreach ($c in $calls) {
        $val = [string]$c.Value
        if ([string]::IsNullOrWhiteSpace($val)) { continue }

        if ($c.Type -eq 'WithId') {
            $val = Get-VisibleImGuiLabel $val
            if ([string]::IsNullOrWhiteSpace($val)) { continue }
        }

        $allKeys.Add($val)
    }
}

$keys = $allKeys | Sort-Object -Unique

$map = [ordered]@{}
foreach ($k in $keys) {
    $map[$k] = ''
}

$dir = Split-Path -Parent $OutFile
if (!(Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}

$json = $map | ConvertTo-Json -Depth 3
$json = $json + "`n"
Set-Content -LiteralPath $OutFile -Value $json -Encoding utf8

Write-Host "Wrote: $OutFile" -ForegroundColor Green
Write-Host ("Keys: {0}" -f $keys.Count) -ForegroundColor Green
