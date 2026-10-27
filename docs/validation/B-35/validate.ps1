# B-35 validation: starts an attempt as a Student with no edition, the empty id and an unknown id, and prints one line per check.
# Needs: PowerShell 7, the app host running, and these variables: STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287).
$ErrorActionPreference = 'Stop'
$Api = if ($env:API) { $env:API } else { 'https://localhost:7287' }
$Base = "$Api/api/v1/exam-engine/attempts"
$script:Failed = $false

function Get-Token($user, $password) {
    $form = @{ grant_type = 'password'; username = $user; password = $password; client_id = 'simulab-web' # agile:allow-secret
               client_secret = 'dev-only-simulab-web-secret-do-not-use-outside-local-dev' } # agile:allow-secret (local development client)
    (Invoke-RestMethod -SkipCertificateCheck -Method Post -Uri "$Api/connect/token" -Body $form).access_token
}

# Start <json body>: sets $script:Status, $script:Code and $script:Errors ("field/code" entries joined by a space).
function Start-Attempt($json) {
    $params = @{ SkipCertificateCheck = $true; SkipHttpErrorCheck = $true; Method = 'Post'; Uri = $Base
                 Headers = @{ Authorization = "Bearer $st" }; Body = $json; ContentType = 'application/json' }
    $response = Invoke-WebRequest @params
    $script:Status = [int]$response.StatusCode
    # application/problem+json comes back as bytes, so read the stream as text.
    $raw = [Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray())
    $body = if ($raw) { try { $raw | ConvertFrom-Json } catch { $null } } else { $null }
    $script:Code = if ($body.code) { $body.code } else { '' }
    $script:Errors = (@($body.errors | Where-Object { $_ } | ForEach-Object { "$($_.field)/$($_.code)" })) -join ' '
}

function Expect($label, $status, $code, $errors) {
    $shown = if ($script:Errors) { $script:Errors } else { '(no errors)' }
    if ($script:Status -eq $status -and $script:Code -eq $code -and $script:Errors -eq $errors) {
        "OK   $label -> $($script:Status) $($script:Code) $shown"
    }
    else {
        $wanted = if ($errors) { $errors } else { '(no errors)' }
        "FAIL $label -> $($script:Status) $($script:Code) $shown (expected $status $code $wanted)"; $script:Failed = $true
    }
}

$st = Get-Token $env:STUDENT_EMAIL $env:STUDENT_PASSWORD
if (-not $st) { 'FAIL student sign-in'; exit 1 }

Start-Attempt '{}'
Expect 'body {}' 400 'exam_attempt.edition_required' 'examEditionId/exam_attempt.edition_required'
Start-Attempt '{"examEditionId":"00000000-0000-0000-0000-000000000000"}'
Expect 'empty Guid' 400 'exam_attempt.edition_required' 'examEditionId/exam_attempt.edition_required'
Start-Attempt '{"examEditionId":"00000000-0000-7000-8000-000000000000"}'
Expect 'random Guid' 404 'exam_attempt.edition_not_found' ''

if ($script:Failed) { 'SOME CHECKS FAILED'; exit 1 } else { 'ALL OK' }
