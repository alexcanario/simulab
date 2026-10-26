#!/usr/bin/env bash
# F-166 validation: calls the attempts API as a Student and as an Admin and prints one line per check.
# Needs: the app host running, curl and jq, and these variables: ADMIN_PASSWORD, STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287), EDITION_ID (a published edition with published questions).
set -u
API="${API:-https://localhost:7287}"
EDITION_ID="${EDITION_ID:-0198f370-0004-7000-8000-000000000003}"
BASE="$API/api/v1/exam-engine/attempts"
FAILED=0

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

check() { # check <label> <expected code> [also-accepted code]
  if [ "$CODE" = "$2" ] || { [ $# -ge 3 ] && [ "$CODE" = "$3" ]; }; then echo "OK   $1 -> $CODE"; else echo "FAIL $1 -> $CODE (expected $2)"; echo "     $BODY" | cut -c1-300; FAILED=1; fi
}

ST=$(token "$STUDENT_EMAIL" "$STUDENT_PASSWORD")
AT=$(token admin@simulab.local "$ADMIN_PASSWORD")
[ -z "$ST" ] || [ "$ST" = null ] && { echo "FAIL student sign-in"; exit 1; }
[ -z "$AT" ] || [ "$AT" = null ] && { echo "FAIL admin sign-in"; exit 1; }

echo "== Step 2: start"
call - POST "$BASE" "{\"examEditionId\":\"$EDITION_ID\"}"; check "anonymous start" 401
call "$ST" POST "$BASE" "{\"examEditionId\":\"$EDITION_ID\"}"; check "student start" 201 200
ATT=$(echo "$BODY" | jqr .id)
echo "     attempt $ATT, questions: $(echo "$BODY" | jqr '[.questions[] | "\(.paperNumber):\(.type)"] | join(" ")')"
call "$ST" POST "$BASE" "{\"examEditionId\":\"$EDITION_ID\"}"; check "start again returns the same attempt" 200
[ "$(echo "$BODY" | jqr .id)" = "$ATT" ] && echo "OK   same attempt id" || { echo "FAIL different attempt id"; FAILED=1; }
call "$ST" POST "$BASE" '{"examEditionId":"00000000-0000-7000-8000-000000000000"}'; check "unknown edition" 404

echo "== Step 3: save and read back"
Q=($(call "$ST" GET "$BASE/$ATT" && echo "$BODY" | jqr '.questions[] | .questionId + ":" + .type'))
for entry in "${Q[@]}"; do
  qid="${entry%%:*}"; type="${entry##*:}"
  case "$type" in
    TrueFalse) json='{"type":"TrueFalse","value":true}' ;;
    SingleChoice) json='{"type":"SingleChoice","optionPosition":1}' ;;
    MultipleAnswer) json='{"type":"MultipleAnswer","optionPositions":[0,1]}' ;;
    *) continue ;;
  esac
  call "$ST" PUT "$BASE/$ATT/answers/$qid" "$json"; check "save $type" 200
done
call "$ST" GET "$BASE/$ATT"; check "read the attempt" 200
echo "     answers read back: $(echo "$BODY" | jq -c '[.questions[] | .answer]')"

echo "== Step 4: invalid, other order, replace, clear"
FIRST="${Q[0]%%:*}"
call "$ST" PUT "$BASE/$ATT/answers/$FIRST" '{"type":"ShortAnswer","text":"x"}'; check "answer of another type" 400
echo "     code: $(echo "$BODY" | jqr .code)"
call "$ST" PUT "$BASE/$ATT/answers/$FIRST" '{"value":false,"type":"TrueFalse"}'; check "type member after the others (true/false)" 200
call "$ST" PUT "$BASE/$ATT/answers/00000000-0000-7000-8000-000000000000" '{"type":"TrueFalse","value":true}'; check "question not in the attempt" 404
call "$ST" DELETE "$BASE/$ATT/answers/$FIRST"; check "clear the answer" 204

echo "== Step 5: who may read and change"
call "$AT" GET "$BASE/$ATT"; check "admin reads the student's attempt" 200
call "$AT" PUT "$BASE/$ATT/answers/$FIRST" '{"type":"TrueFalse","value":true}'; check "admin saves in it" 404
call "$AT" POST "$BASE/$ATT/submit"; check "admin submits it" 404
call - GET "$BASE/$ATT"; check "anonymous reads" 401
call "$ST" GET "$BASE/$ATT"; echo "$BODY" | grep -qi 'explanation\|answerKey\|modelAnswer\|isCorrect' && { echo "FAIL the response holds a key or explanation"; FAILED=1; } || echo "OK   no key, explanation or model answer in the response"

echo "== Step 6: submit"
call "$ST" POST "$BASE/$ATT/submit"; check "submit" 200
T1=$(echo "$BODY" | jqr .submittedAt); echo "     status $(echo "$BODY" | jqr .status), submittedAt $T1"
call "$ST" POST "$BASE/$ATT/submit"; check "submit again" 200
[ "$(echo "$BODY" | jqr .submittedAt)" = "$T1" ] && echo "OK   same submission time" || { echo "FAIL the time changed"; FAILED=1; }
call "$ST" PUT "$BASE/$ATT/answers/$FIRST" '{"type":"TrueFalse","value":true}'; check "save after submit" 409
echo "     code: $(echo "$BODY" | jqr .code)"
call "$ST" DELETE "$BASE/$ATT/answers/$FIRST"; check "clear after submit" 409

[ "$FAILED" = 0 ] && echo "ALL OK" || echo "SOME CHECKS FAILED"
exit "$FAILED"
