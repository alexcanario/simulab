# F-166 validation: calls the attempts API as a Student and as an Admin and prints one line per check.
# Needs: PowerShell 7, the app host running, and these variables: ADMIN_PASSWORD, STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287), EDITION_ID (a published edition with published questions).
$ErrorActionPreference = 'Stop'
$Api = if ($env:API) { $env:API } else { 'https://localhost:7287' }
$EditionId = if ($env:EDITION_ID) { $env:EDITION_ID } else { '0198f370-0004-7000-8000-000000000003' }
$Base = "$Api/api/v1/exam-engine/attempts"
$script:Failed = $false

function Get-Token($user, $password) {
    $form = @{ grant_type = 'password'; username = $user; password = $password; client_id = 'simulab-web' # agile:allow-secret
               client_secret = 'dev-only-simulab-web-secret-do-not-use-outside-local-dev' } # agile:allow-secret (local development client)
    (Invoke-RestMethod -SkipCertificateCheck -Method Post -Uri "$Api/connect/token" -Body $form).access_token
}

# Call <token or $null> <method> <url> [json body]: sets $script:Code and $script:Body (parsed JSON or $null).
function Call($token, $method, $url, $json = $null) {
    $headers = @{}
    if ($token) { $headers['Authorization'] = "Bearer $token" }
    $params = @{ SkipCertificateCheck = $true; SkipHttpErrorCheck = $true; Method = $method; Uri = $url; Headers = $headers }
    if ($json) { $params['Body'] = $json; $params['ContentType'] = 'application/json' }
    $response = Invoke-WebRequest @params
    $script:Code = [int]$response.StatusCode
    # application/problem+json comes back as bytes, so read the stream as text.
    $script:Raw = [Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray())
    $script:Body = if ($script:Raw) { try { $script:Raw | ConvertFrom-Json -DateKind String } catch { $null } } else { $null }
}

function Check($label, $expected, $alsoAccepted = 0) {
    if ($script:Code -eq $expected -or ($alsoAccepted -ne 0 -and $script:Code -eq $alsoAccepted)) { "OK   $label -> $($script:Code)" }
    else { "FAIL $label -> $($script:Code) (expected $expected)"; "     $($script:Raw)".Substring(0, [Math]::Min(300, "     $($script:Raw)".Length)); $script:Failed = $true }
}

$st = Get-Token $env:STUDENT_EMAIL $env:STUDENT_PASSWORD
$at = Get-Token 'admin@simulab.local' $env:ADMIN_PASSWORD
$start = @{ examEditionId = $EditionId } | ConvertTo-Json -Compress

'== Step 2: start'
Call $null Post $Base $start; Check 'anonymous start' 401
Call $st Post $Base $start; Check 'student start' 201 200
$att = $script:Body.id
"     attempt $att, questions: $(($script:Body.questions | ForEach-Object { "$($_.paperNumber):$($_.type)" }) -join ' ')"
Call $st Post $Base $start; Check 'start again returns the same attempt' 200
if ($script:Body.id -eq $att) { 'OK   same attempt id' } else { 'FAIL different attempt id'; $script:Failed = $true }
Call $st Post $Base '{"examEditionId":"00000000-0000-7000-8000-000000000000"}'; Check 'unknown edition' 404

'== Step 3: save and read back'
Call $st Get "$Base/$att"
$questions = $script:Body.questions
foreach ($question in $questions) {
    $json = switch ($question.type) {
        'TrueFalse' { '{"type":"TrueFalse","value":true}' }
        'SingleChoice' { '{"type":"SingleChoice","optionPosition":1}' }
        'MultipleAnswer' { '{"type":"MultipleAnswer","optionPositions":[0,1]}' }
        default { $null }
    }
    if ($json) { Call $st Put "$Base/$att/answers/$($question.questionId)" $json; Check "save $($question.type)" 200 }
}
Call $st Get "$Base/$att"; Check 'read the attempt' 200
"     answers read back: $(($script:Body.questions | ForEach-Object { $_.answer | ConvertTo-Json -Compress -Depth 5 }) -join ',')"

'== Step 4: invalid, other order, replace, clear'
$first = $questions[0].questionId
Call $st Put "$Base/$att/answers/$first" '{"type":"ShortAnswer","text":"x"}'; Check 'answer of another type' 400
"     code: $($script:Body.code)"
Call $st Put "$Base/$att/answers/$first" '{"value":false,"type":"TrueFalse"}'; Check 'type member after the others (true/false)' 200
Call $st Put "$Base/$att/answers/00000000-0000-7000-8000-000000000000" '{"type":"TrueFalse","value":true}'; Check 'question not in the attempt' 404
Call $st Delete "$Base/$att/answers/$first"; Check 'clear the answer' 204

'== Step 5: who may read and change'
Call $at Get "$Base/$att"; Check "admin reads the student's attempt" 200
Call $at Put "$Base/$att/answers/$first" '{"type":"TrueFalse","value":true}'; Check 'admin saves in it' 404
Call $at Post "$Base/$att/submit"; Check 'admin submits it' 404
Call $null Get "$Base/$att"; Check 'anonymous reads' 401
Call $st Get "$Base/$att"
if ($script:Raw -match 'explanation|answerKey|modelAnswer|isCorrect') { 'FAIL the response holds a key or explanation'; $script:Failed = $true }
else { 'OK   no key, explanation or model answer in the response' }

'== Step 6: submit'
Call $st Post "$Base/$att/submit"; Check 'submit' 200
$t1 = $script:Body.submittedAt
"     status $($script:Body.status), submittedAt $t1"
Call $st Post "$Base/$att/submit"; Check 'submit again' 200
if ($script:Body.submittedAt -eq $t1) { 'OK   same submission time' } else { 'FAIL the time changed'; $script:Failed = $true }
Call $st Put "$Base/$att/answers/$first" '{"type":"TrueFalse","value":true}'; Check 'save after submit' 409
"     code: $($script:Body.code)"
Call $st Delete "$Base/$att/answers/$first"; Check 'clear after submit' 409

if ($script:Failed) { 'SOME CHECKS FAILED'; exit 1 } else { 'ALL OK' }
