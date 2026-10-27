# F-167 validation: a Student answers two attempts, downloads their data, erases the account, and an Admin reads the
# attempt afterwards. Prints one line per check. The Student account is ERASED at the end: use one you do not need.
# Needs: PowerShell 7, the app host running, and these variables: ADMIN_PASSWORD, STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287). It makes its own published edition with three questions, as an admin.
$ErrorActionPreference = 'Stop'
$Api = if ($env:API) { $env:API } else { 'https://localhost:7287' }
$Attempts = "$Api/api/v1/exam-engine/attempts"
$script:Failed = 0
$Mark = "F167-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"

function Get-Token([string]$User, [string]$Password) {
    $form = @{ grant_type = 'password'; username = $User; password = $Password; client_id = 'simulab-web'; client_secret = 'dev-only-simulab-web-secret-do-not-use-outside-local-dev' } # agile:allow-secret (local development client)
    try { (Invoke-RestMethod -Method Post -Uri "$Api/connect/token" -Body $form -SkipCertificateCheck).access_token } catch { $null }
}

# Invoke-Call <token or -> <method> <url> [json body]: sets $script:Code and $script:Body.
function Invoke-Call([string]$Token, [string]$Method, [string]$Url, [string]$Json) {
    $headers = @{}
    if ($Token -ne '-') { $headers['Authorization'] = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = $Url; Headers = $headers; SkipCertificateCheck = $true; SkipHttpErrorCheck = $true }
    # A route that reads a body answers 415 without the content type, even when the body is empty (as curl -d '' sends it).
    if ($PSBoundParameters.ContainsKey('Json')) { $params['ContentType'] = 'application/json'; $params['Body'] = $Json }
    $response = Invoke-WebRequest @params
    $script:Code = [int]$response.StatusCode
    $script:Body = if ($response.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($response.Content) } else { [string]$response.Content }
}

function Test-Code([string]$Label, [int]$Expected) {
    if ($script:Code -eq $Expected) { "OK   $Label -> $($script:Code)" }
    else { "FAIL $Label -> $($script:Code) (expected $Expected)"; "     $($script:Body)".Substring(0, [Math]::Min(300, $script:Body.Length + 5)); $script:Failed = 1 }
}

function Test-True([string]$Label, [bool]$Condition) {
    if ($Condition) { "OK   $Label" } else { "FAIL $Label"; $script:Failed = 1 }
}

function New-Thing([string]$Label, [string]$Method, [string]$Url, [string]$Json) {
    Invoke-Call $script:AT $Method $Url $Json
    if ($script:Code -ne 201 -and $script:Code -ne 200) { "FAIL setup: $Label -> $($script:Code)"; $script:Body; exit 1 }
    ($script:Body | ConvertFrom-Json).id
}

function Compact($Value) { $Value | ConvertTo-Json -Depth 10 -Compress }

$script:AT = Get-Token 'admin@simulab.local' $env:ADMIN_PASSWORD
$ST = Get-Token $env:STUDENT_EMAIL $env:STUDENT_PASSWORD
if (-not $script:AT) { 'FAIL admin sign-in'; exit 1 }
if (-not $ST) { 'FAIL student sign-in'; exit 1 }

'== Step 3: a published edition with a short answer, a fill-in-the-blanks and a single choice question'
$Subject = New-Thing 'subject' Post "$Api/api/v1/catalog/subjects" "{`"name`":`"Subject $Mark`"}"
$Topic = New-Thing 'topic' Post "$Api/api/v1/catalog/subjects/$Subject/topics" "{`"name`":`"Topic $Mark`"}"
$Authority = New-Thing 'issuing authority' Post "$Api/api/v1/catalog/issuing-authorities" "{`"name`":`"Authority $Mark`"}"
$Exam = New-Thing 'exam' Post "$Api/api/v1/catalog/exams" "{`"issuingAuthorityId`":`"$Authority`",`"name`":`"Exam $Mark`",`"assessmentType`":`"PublicServiceExam`",`"scope`":`"National`",`"contentLanguage`":`"pt-BR`"}"
$Organizer = New-Thing 'organizer' Post "$Api/api/v1/catalog/organizers" "{`"name`":`"Board $Mark`",`"acronym`":`"B$($Mark.Substring($Mark.Length - 8))`",`"kind`":`"ExamBoard`"}"
$Edition = New-Thing 'edition' Post "$Api/api/v1/catalog/exams/$Exam/editions" "{`"organizerId`":`"$Organizer`",`"noticeYear`":2024,`"status`":`"Draft`"}"
$Q = "`"topicId`":`"$Topic`",`"contentLanguage`":`"pt-BR`",`"examEditionId`":`"$Edition`""
$Q1 = New-Thing 'short answer question' Post "$Api/api/v1/question-bank/questions" "{$Q,`"paperNumber`":1,`"statementHtml`":`"<p>Capital?</p>`",`"type`":`"ShortAnswer`",`"blanks`":[{`"number`":1,`"acceptedAnswers`":[`"Brasilia`"]}]}"
$Q2 = New-Thing 'fill-in-the-blanks question' Post "$Api/api/v1/question-bank/questions" "{$Q,`"paperNumber`":2,`"statementHtml`":`"<p>[[1]] e [[2]]</p>`",`"type`":`"FillInTheBlanks`",`"blanks`":[{`"number`":1,`"acceptedAnswers`":[`"a`"]},{`"number`":2,`"acceptedAnswers`":[`"b`"]}]}"
$Q3 = New-Thing 'single choice question' Post "$Api/api/v1/question-bank/questions" "{$Q,`"paperNumber`":3,`"statementHtml`":`"<p>Escolha</p>`",`"explanationHtml`":`"<p>SECRET-EXPLANATION</p>`",`"options`":[{`"textHtml`":`"<p>A</p>`",`"isCorrect`":true},{`"textHtml`":`"<p>B</p>`"}]}"
foreach ($question in @($Q1, $Q2, $Q3)) { $null = New-Thing 'publish question' Post "$Api/api/v1/question-bank/questions/$question/publish" '' }
$null = New-Thing 'publish edition' Put "$Api/api/v1/catalog/exams/$Exam/editions/$Edition" "{`"organizerId`":`"$Organizer`",`"noticeYear`":2024,`"status`":`"Published`"}"
"OK   edition $Edition published with 3 questions"

'== Step 4: two attempts with texts'
Invoke-Call $ST Post $Attempts "{`"examEditionId`":`"$Edition`"}"; Test-Code 'start the first attempt' 201
$A1 = ($script:Body | ConvertFrom-Json).id
Invoke-Call $ST Put "$Attempts/$A1/answers/$Q1" "{`"type`":`"ShortAnswer`",`"text`":`"TEXT-$Mark-SHORT`"}"; Test-Code 'save a short answer' 200
Invoke-Call $ST Put "$Attempts/$A1/answers/$Q2" "{`"type`":`"FillInTheBlanks`",`"blanks`":[{`"number`":1,`"text`":`"TEXT-$Mark-B1`"},{`"number`":2,`"text`":`"TEXT-$Mark-B2`"}]}"; Test-Code 'save the blanks' 200
Invoke-Call $ST Put "$Attempts/$A1/answers/$Q3" '{"type":"SingleChoice","optionPosition":1}'; Test-Code 'save a single choice' 200
Invoke-Call $ST Post "$Attempts/$A1/submit" ''; Test-Code 'submit the first attempt' 200
Invoke-Call $ST Post $Attempts "{`"examEditionId`":`"$Edition`"}"; Test-Code 'start the second attempt' 201
$A2 = ($script:Body | ConvertFrom-Json).id
Invoke-Call $ST Put "$Attempts/$A2/answers/$Q1" "{`"type`":`"ShortAnswer`",`"text`":`"TEXT-$Mark-SECOND`"}"; Test-Code 'save a short answer in the second' 200

'== Step 5: download my data'
Invoke-Call $ST Post "$Api/api/v1/identity/data-exports" "{`"currentPassword`":`"$env:STUDENT_PASSWORD`"}"; Test-Code 'export with the right password' 200
$FileText = $script:Body
$File = $FileText | ConvertFrom-Json
"     members: $(($File.PSObject.Properties.Name) -join ' ')"
$Mine = @($File.examEngine.attempts | Where-Object { $_.examEditionId -eq $Edition } | ForEach-Object { $_.id }) -join ' '
Test-True 'the two attempts, oldest first' ($Mine -eq "$A1 $A2")
$Second = $File.examEngine.attempts | Where-Object { $_.id -eq $A2 }
$SubmittedAt = if ($null -eq $Second.submittedAt) { 'null' } else { $Second.submittedAt }
"     second attempt: status $($Second.status), submittedAt $SubmittedAt"
"     first attempt answers: $(Compact @(($File.examEngine.attempts | Where-Object { $_.id -eq $A1 }).questions.answer))"
$Section = Compact $File.examEngine
Test-True 'no statement, explanation or option text in the section' (-not ($Section -match 'SECRET-EXPLANATION|statement|<p>|isCorrect|tenantId'))

'== Step 6: erase the account and wait for the job'
Invoke-Call $ST Post "$Api/api/v1/identity/account-erasures" "{`"currentPassword`":`"$env:STUDENT_PASSWORD`"}"; Test-Code 'erase the account' 204
$i = 0
do {
    $i++
    Invoke-Call $script:AT Get "$Attempts/$A1"
    if ($script:Body -notmatch "TEXT-$Mark") { break }
    Start-Sleep -Seconds 2
} while ($i -lt 60)
Test-True "the job erased the texts (waited $($i * 2) s at most)" ($script:Body -notmatch "TEXT-$Mark")

'== Step 7: an Admin reads the attempt after the erasure'
Invoke-Call $script:AT Get "$Attempts/$A1"; Test-Code 'admin reads the first attempt' 200
$Read = $script:Body | ConvertFrom-Json
"     status $($Read.status), answers: $(Compact @($Read.questions | ForEach-Object { $_.answer }))"
Invoke-Call $script:AT Get "$Attempts/$A2"; Test-Code 'admin reads the second attempt' 200
$Read = $script:Body | ConvertFrom-Json
"     status $($Read.status), answers: $(Compact @($Read.questions | ForEach-Object { $_.answer }))"
Invoke-Call $ST Post "$Api/api/v1/identity/data-exports" "{`"currentPassword`":`"$env:STUDENT_PASSWORD`"}"; Test-Code "the erased student's old token" 401

if ($script:Failed -eq 0) { 'ALL OK' } else { 'SOME CHECKS FAILED' }
exit $script:Failed
