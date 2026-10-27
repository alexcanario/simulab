#!/usr/bin/env bash
# F-167 validation: a Student answers two attempts, downloads their data, erases the account, and an Admin reads the
# attempt afterwards. Prints one line per check. The Student account is ERASED at the end: use one you do not need.
# Needs: the app host running, curl and jq, and these variables: ADMIN_PASSWORD, STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287). It makes its own published edition with three questions, as an admin.
set -u
API="${API:-https://localhost:7287}"
ATTEMPTS="$API/api/v1/exam-engine/attempts"
FAILED=0
MARK="F167-$(date +%s)"

# jq on Windows ends lines with CR; drop it so values compare and word-split cleanly.
jqr() { jq -r "$@" | tr -d '\r'; }

token() {
  curl -sk -X POST "$API/connect/token" -d grant_type=password -d "username=$1" --data-urlencode "password=$2" \
    -d client_id=simulab-web -d client_secret=dev-only-simulab-web-secret-do-not-use-outside-local-dev | jqr .access_token
}

# call <token or -> <method> <url> [json body]: sets CODE and BODY.
call() {
  local auth=() data=()
  [ "$1" != "-" ] && auth=(-H "Authorization: Bearer $1")
  [ $# -ge 4 ] && data=(-H 'Content-Type: application/json' -d "$4")
  local out
  out=$(curl -sk -X "$2" "$3" "${auth[@]}" "${data[@]}" -w '\n%{http_code}')
  CODE="${out##*$'\n'}"
  BODY="${out%$'\n'*}"
}

check() { # check <label> <expected code>
  if [ "$CODE" = "$2" ]; then echo "OK   $1 -> $CODE"; else echo "FAIL $1 -> $CODE (expected $2)"; echo "     $BODY" | cut -c1-300; FAILED=1; fi
}

ok() { # ok <label> <condition result 0|1>
  if [ "$2" = 0 ]; then echo "OK   $1"; else echo "FAIL $1"; FAILED=1; fi
}

made() { # made <label> <method> <url> <json>: expects 201 or 200 and prints nothing else; sets ID
  call "$AT" "$2" "$3" "$4"
  if [ "$CODE" != 201 ] && [ "$CODE" != 200 ]; then echo "FAIL setup: $1 -> $CODE"; echo "     $BODY" | cut -c1-300; exit 1; fi
  ID=$(echo "$BODY" | jqr .id)
}

AT=$(token admin@simulab.local "$ADMIN_PASSWORD")
ST=$(token "$STUDENT_EMAIL" "$STUDENT_PASSWORD")
[ -z "$AT" ] || [ "$AT" = null ] && { echo "FAIL admin sign-in"; exit 1; }
[ -z "$ST" ] || [ "$ST" = null ] && { echo "FAIL student sign-in"; exit 1; }

echo "== Step 3: a published edition with a short answer, a fill-in-the-blanks and a single choice question"
made "subject" POST "$API/api/v1/catalog/subjects" "{\"name\":\"Subject $MARK\"}"; SUBJECT=$ID
made "topic" POST "$API/api/v1/catalog/subjects/$SUBJECT/topics" "{\"name\":\"Topic $MARK\"}"; TOPIC=$ID
made "issuing authority" POST "$API/api/v1/catalog/issuing-authorities" "{\"name\":\"Authority $MARK\"}"; AUTH=$ID
made "exam" POST "$API/api/v1/catalog/exams" "{\"issuingAuthorityId\":\"$AUTH\",\"name\":\"Exam $MARK\",\"assessmentType\":\"PublicServiceExam\",\"scope\":\"National\",\"contentLanguage\":\"pt-BR\"}"; EXAM=$ID
made "organizer" POST "$API/api/v1/catalog/organizers" "{\"name\":\"Board $MARK\",\"acronym\":\"B${MARK: -8}\",\"kind\":\"ExamBoard\"}"; ORG=$ID
made "edition" POST "$API/api/v1/catalog/exams/$EXAM/editions" "{\"organizerId\":\"$ORG\",\"noticeYear\":2024,\"status\":\"Draft\"}"; EDITION=$ID
Q="\"topicId\":\"$TOPIC\",\"contentLanguage\":\"pt-BR\",\"examEditionId\":\"$EDITION\""
made "short answer question" POST "$API/api/v1/question-bank/questions" "{$Q,\"paperNumber\":1,\"statementHtml\":\"<p>Capital?</p>\",\"type\":\"ShortAnswer\",\"blanks\":[{\"number\":1,\"acceptedAnswers\":[\"Brasilia\"]}]}"; Q1=$ID
made "fill-in-the-blanks question" POST "$API/api/v1/question-bank/questions" "{$Q,\"paperNumber\":2,\"statementHtml\":\"<p>[[1]] e [[2]]</p>\",\"type\":\"FillInTheBlanks\",\"blanks\":[{\"number\":1,\"acceptedAnswers\":[\"a\"]},{\"number\":2,\"acceptedAnswers\":[\"b\"]}]}"; Q2=$ID
made "single choice question" POST "$API/api/v1/question-bank/questions" "{$Q,\"paperNumber\":3,\"statementHtml\":\"<p>Escolha</p>\",\"explanationHtml\":\"<p>SECRET-EXPLANATION</p>\",\"options\":[{\"textHtml\":\"<p>A</p>\",\"isCorrect\":true},{\"textHtml\":\"<p>B</p>\"}]}"; Q3=$ID
for q in "$Q1" "$Q2" "$Q3"; do made "publish question" POST "$API/api/v1/question-bank/questions/$q/publish" ""; done
call "$AT" GET "$API/api/v1/catalog/exams/$EXAM/editions/$EDITION"
made "publish edition" PUT "$API/api/v1/catalog/exams/$EXAM/editions/$EDITION" "{\"organizerId\":\"$ORG\",\"noticeYear\":2024,\"status\":\"Published\"}"
echo "OK   edition $EDITION published with 3 questions"

echo "== Step 4: two attempts with texts"
call "$ST" POST "$ATTEMPTS" "{\"examEditionId\":\"$EDITION\"}"; check "start the first attempt" 201
A1=$(echo "$BODY" | jqr .id)
call "$ST" PUT "$ATTEMPTS/$A1/answers/$Q1" "{\"type\":\"ShortAnswer\",\"text\":\"TEXT-$MARK-SHORT\"}"; check "save a short answer" 200
call "$ST" PUT "$ATTEMPTS/$A1/answers/$Q2" "{\"type\":\"FillInTheBlanks\",\"blanks\":[{\"number\":1,\"text\":\"TEXT-$MARK-B1\"},{\"number\":2,\"text\":\"TEXT-$MARK-B2\"}]}"; check "save the blanks" 200
call "$ST" PUT "$ATTEMPTS/$A1/answers/$Q3" '{"type":"SingleChoice","optionPosition":1}'; check "save a single choice" 200
call "$ST" POST "$ATTEMPTS/$A1/submit"; check "submit the first attempt" 200
call "$ST" POST "$ATTEMPTS" "{\"examEditionId\":\"$EDITION\"}"; check "start the second attempt" 201
A2=$(echo "$BODY" | jqr .id)
call "$ST" PUT "$ATTEMPTS/$A2/answers/$Q1" "{\"type\":\"ShortAnswer\",\"text\":\"TEXT-$MARK-SECOND\"}"; check "save a short answer in the second" 200

echo "== Step 5: download my data"
call "$ST" POST "$API/api/v1/identity/data-exports" "{\"currentPassword\":\"$STUDENT_PASSWORD\"}"; check "export with the right password" 200
FILE="$BODY"
echo "     members: $(echo "$FILE" | jqr '[keys_unsorted[]] | join(" ")')"
MINE=$(echo "$FILE" | jqr "[.examEngine.attempts[] | select(.examEditionId == \"$EDITION\") | .id] | join(\" \")")
ok "the two attempts, oldest first" "$([ "$MINE" = "$A1 $A2" ] && echo 0 || echo 1)"
echo "     second attempt: status $(echo "$FILE" | jqr ".examEngine.attempts[] | select(.id == \"$A2\") | .status"), submittedAt $(echo "$FILE" | jqr ".examEngine.attempts[] | select(.id == \"$A2\") | .submittedAt")"
echo "     first attempt answers: $(echo "$FILE" | jq -c ".examEngine.attempts[] | select(.id == \"$A1\") | [.questions[] | .answer]")"
ok "no statement, explanation or option text in the section" "$(echo "$FILE" | jq -c .examEngine | grep -qi 'SECRET-EXPLANATION\|statement\|<p>\|isCorrect\|tenantId' && echo 1 || echo 0)"

echo "== Step 6: erase the account and wait for the job"
call "$ST" POST "$API/api/v1/identity/account-erasures" "{\"currentPassword\":\"$STUDENT_PASSWORD\"}"; check "erase the account" 204
for i in $(seq 1 60); do
  call "$AT" GET "$ATTEMPTS/$A1"
  echo "$BODY" | grep -q "TEXT-$MARK" || break
  sleep 2
done
ok "the job erased the texts (waited $((i * 2)) s at most)" "$(echo "$BODY" | grep -q "TEXT-$MARK" && echo 1 || echo 0)"

echo "== Step 7: an Admin reads the attempt after the erasure"
call "$AT" GET "$ATTEMPTS/$A1"; check "admin reads the first attempt" 200
echo "     status $(echo "$BODY" | jqr .status), answers: $(echo "$BODY" | jq -c '[.questions[] | .answer]')"
call "$AT" GET "$ATTEMPTS/$A2"; check "admin reads the second attempt" 200
echo "     status $(echo "$BODY" | jqr .status), answers: $(echo "$BODY" | jq -c '[.questions[] | .answer]')"
call "$ST" POST "$API/api/v1/identity/data-exports" "{\"currentPassword\":\"$STUDENT_PASSWORD\"}"; check "the erased student's old token" 401

[ "$FAILED" = 0 ] && echo "ALL OK" || echo "SOME CHECKS FAILED"
exit "$FAILED"
