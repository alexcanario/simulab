#!/usr/bin/env bash
# B-35 validation: starts an attempt as a Student with no edition, the empty id and an unknown id, and prints one line per check.
# Needs: the app host running, curl and jq, and these variables: STUDENT_EMAIL, STUDENT_PASSWORD.
# Optional: API (default https://localhost:7287).
set -u
API="${API:-https://localhost:7287}"
BASE="$API/api/v1/exam-engine/attempts"
FAILED=0

# jq on Windows ends lines with CR; drop it so values compare cleanly.
jqr() { jq -r "$@" | tr -d '\r'; }

token() {
  curl -sk -X POST "$API/connect/token" -d grant_type=password -d "username=$1" --data-urlencode "password=$2" \
    -d client_id=simulab-web -d client_secret=dev-only-simulab-web-secret-do-not-use-outside-local-dev | jqr .access_token # agile:allow-secret (local development client)
}

# start <json body>: sets STATUS, CODE and ERRORS ("field/code" entries joined by a space).
start() {
  local out
  out=$(curl -sk -X POST "$BASE" -H "Authorization: Bearer $ST" -H 'Content-Type: application/json' -d "$1" -w '\n%{http_code}')
  STATUS="${out##*$'\n'}"
  BODY="${out%$'\n'*}"
  CODE=$(echo "$BODY" | jqr '.code // ""')
  ERRORS=$(echo "$BODY" | jqr '[(.errors // [])[] | "\(.field)/\(.code)"] | join(" ")')
}

# expect <label> <status> <code> <errors>
expect() {
  if [ "$STATUS" = "$2" ] && [ "$CODE" = "$3" ] && [ "$ERRORS" = "$4" ]; then
    echo "OK   $1 -> $STATUS $CODE ${ERRORS:-(no errors)}"
  else
    echo "FAIL $1 -> $STATUS $CODE ${ERRORS:-(no errors)} (expected $2 $3 ${4:-(no errors)})"; FAILED=1
  fi
}

ST=$(token "$STUDENT_EMAIL" "$STUDENT_PASSWORD")
[ -z "$ST" ] || [ "$ST" = null ] && { echo "FAIL student sign-in"; exit 1; }

start '{}'
expect "body {}" 400 exam_attempt.edition_required "examEditionId/exam_attempt.edition_required"
start '{"examEditionId":"00000000-0000-0000-0000-000000000000"}'
expect "empty Guid" 400 exam_attempt.edition_required "examEditionId/exam_attempt.edition_required"
start '{"examEditionId":"00000000-0000-7000-8000-000000000000"}'
expect "random Guid" 404 exam_attempt.edition_not_found ""

[ "$FAILED" = 0 ] && echo "ALL OK" || echo "SOME CHECKS FAILED"
exit "$FAILED"
