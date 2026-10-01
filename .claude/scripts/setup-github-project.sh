#!/bin/bash
# Setup GitHub Projects board with Simulab workflow columns
# Usage: ./setup-github-project.sh <project_number>
# Example: ./setup-github-project.sh 1

set -e

if [ $# -ne 1 ]; then
    echo "Usage: $0 <project_number>"
    echo "Example: $0 1"
    exit 1
fi

PROJECT_NUMBER=$1
OWNER="alexcanario"
REPO="simulab"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Setting up GitHub Projects board for Simulab${NC}"
echo "Owner: $OWNER"
echo "Repo: $REPO"
echo "Project Number: $PROJECT_NUMBER"
echo ""

# Get project ID
echo -e "${BLUE}Fetching project details...${NC}"
PROJECT_DATA=$(gh api repos/$OWNER/$REPO/projects/$PROJECT_NUMBER)
PROJECT_ID=$(echo "$PROJECT_DATA" | jq -r '.id')
echo "Project ID: $PROJECT_ID"

# Workflow status values (order matters)
declare -a STATUS_VALUES=("idea" "refining" "approved" "building" "validating" "done")

echo -e "${BLUE}Configuring Status field with workflow values...${NC}"

# Note: GitHub Projects v2 uses a different API structure
# The configuration below is a template for manual setup or future automation
cat > /tmp/project-config.json << 'EOF'
{
  "workflow_status_values": [
    "idea",
    "refining",
    "approved",
    "building",
    "validating",
    "done"
  ],
  "description": "Simulab workflow: idea → refining → approved → building → validating → done",
  "automation": {
    "auto_add_to_projects": true,
    "auto_add_pull_requests": true
  }
}
EOF

echo -e "${GREEN}✓ Configuration template created at /tmp/project-config.json${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Go to: https://github.com/$OWNER/$REPO/projects/$PROJECT_NUMBER"
echo "2. Click the gear icon (Settings)"
echo "3. Click 'Custom fields'"
echo "4. Find or create the 'Status' field"
echo "5. Add these values IN ORDER:"
for status in "${STATUS_VALUES[@]}"; do
    echo "   - $status"
done
echo "6. Save the configuration"
echo ""
echo -e "${GREEN}Board setup will be complete!${NC}"
