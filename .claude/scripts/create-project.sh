#!/bin/bash
# Create and configure GitHub Projects board for Simulab
# Uses GraphQL API via curl with gh CLI authentication
#
# Prerequisites:
#   - gh CLI installed and authenticated: gh auth login
#   - curl and jq installed
#
# Usage: ./create-project.sh

set -euo pipefail

OWNER="alexcanario"
REPO="simulab"
PROJECT_TITLE="Simulab"

# Workflow status in order
STATUS_VALUES=("idea" "refining" "approved" "building" "validating" "done")

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Functions
log_info() { echo -e "${BLUE}➜${NC} $*"; }
log_success() { echo -e "${GREEN}✓${NC} $*"; }
log_error() { echo -e "${RED}✗${NC} $*"; }
log_warn() { echo -e "${YELLOW}⚠${NC} $*"; }

# Check prerequisites
log_info "Checking prerequisites..."

if ! command -v gh &> /dev/null; then
    log_error "gh CLI not found. Install it: https://cli.github.com"
    exit 1
fi

if ! command -v curl &> /dev/null; then
    log_error "curl not found"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    log_error "jq not found"
    exit 1
fi

# Verify authentication
if ! gh auth status > /dev/null 2>&1; then
    log_error "Not authenticated with GitHub"
    log_info "Run: gh auth login"
    exit 1
fi

log_success "Prerequisites OK"
echo ""

# Get token
TOKEN=$(gh auth token)

# GraphQL helper
graphql() {
    local query=$1
    curl -s -X POST https://api.github.com/graphql \
      -H "Authorization: bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "$query"
}

# Step 1: Get viewer ID
log_info "Getting your GitHub user ID..."
VIEWER_ID=$(graphql '{"query":"query{viewer{id}}"}' | jq -r '.data.viewer.id')
log_success "User ID: $VIEWER_ID"
echo ""

# Step 2: Create project
log_info "Creating GitHub Project: $PROJECT_TITLE..."
CREATE_QUERY=$(cat <<EOF
{
  "query": "mutation(\$input: CreateProjectV2Input!) { createProjectV2(input: \$input) { projectV2 { id number title } } }",
  "variables": {
    "input": {
      "ownerId": "$VIEWER_ID",
      "title": "$PROJECT_TITLE"
    }
  }
}
EOF
)

PROJECT_RESPONSE=$(graphql "$CREATE_QUERY")
PROJECT_ID=$(echo "$PROJECT_RESPONSE" | jq -r '.data.createProjectV2.projectV2.id // empty')
PROJECT_NUM=$(echo "$PROJECT_RESPONSE" | jq -r '.data.createProjectV2.projectV2.number // empty')

if [ -z "$PROJECT_ID" ]; then
    log_error "Failed to create project"
    echo "$PROJECT_RESPONSE" | jq '.'
    exit 1
fi

log_success "Project created (ID: $PROJECT_ID, #$PROJECT_NUM)"
echo ""

# Step 3: Create Status field
log_info "Creating Status field..."
FIELD_QUERY=$(cat <<EOF
{
  "query": "mutation(\$input: CreateProjectV2FieldInput!) { createProjectV2Field(input: \$input) { projectV2Field { id name } } }",
  "variables": {
    "input": {
      "projectId": "$PROJECT_ID",
      "name": "Status",
      "dataType": "SINGLE_SELECT"
    }
  }
}
EOF
)

FIELD_RESPONSE=$(graphql "$FIELD_QUERY")
FIELD_ID=$(echo "$FIELD_RESPONSE" | jq -r '.data.createProjectV2Field.projectV2Field.id // empty')

if [ -z "$FIELD_ID" ]; then
    log_warn "Status field may already exist, retrieving..."

    GET_FIELD_QUERY=$(cat <<EOF
{
  "query": "query(\$projectId: ID!) { node(id: \$projectId) { ... on ProjectV2 { fields(first: 20) { nodes { ... on ProjectV2SingleSelectField { id name } } } } } }",
  "variables": {
    "projectId": "$PROJECT_ID"
  }
}
EOF
)

    FIELD_ID=$(graphql "$GET_FIELD_QUERY" | jq -r '.data.node.fields.nodes[] | select(.name=="Status") | .id // empty')
fi

if [ -z "$FIELD_ID" ]; then
    log_error "Could not create or find Status field"
    exit 1
fi

log_success "Status field created (ID: $FIELD_ID)"
echo ""

# Step 4: Add status options
log_info "Adding workflow status values..."
for status in "${STATUS_VALUES[@]}"; do
    OPTION_QUERY=$(cat <<EOF
{
  "query": "mutation(\$input: CreateProjectV2DraftIssueInput!) { createProjectV2DraftIssue(input: \$input) { projectV2DraftIssue { id } } }",
  "variables": {
    "input": {
      "projectId": "$PROJECT_ID",
      "title": "Status option: $status",
      "fields": [
        {
          "fieldId": "$FIELD_ID",
          "value": "$status"
        }
      ]
    }
  }
}
EOF
)

    OPTION_RESPONSE=$(graphql "$OPTION_QUERY")
    DRAFT_ID=$(echo "$OPTION_RESPONSE" | jq -r '.data.createProjectV2DraftIssue.projectV2DraftIssue.id // empty')

    if [ -n "$DRAFT_ID" ]; then
        log_success "Added: $status"
    else
        log_warn "Option '$status' (may already exist)"
    fi
done

echo ""
echo -e "${GREEN}=== Project Setup Complete! ===${NC}"
echo ""
log_info "Project URL:"
echo "  https://github.com/$OWNER/$REPO/projects/$PROJECT_NUM"
echo ""
log_info "Configured status values (in workflow order):"
for status in "${STATUS_VALUES[@]}"; do
    echo "  • $status"
done
echo ""
log_info "Next steps:"
echo "  1. Visit the project URL above"
echo "  2. Go to Settings → Automation (optional)"
echo "  3. Enable auto-add for issues/PRs (optional)"
echo "  4. Start adding issues to the board!"
