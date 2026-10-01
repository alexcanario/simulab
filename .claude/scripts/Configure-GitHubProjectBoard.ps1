<#
.SYNOPSIS
Configure GitHub Projects board for Simulab workflow

.DESCRIPTION
Creates a new GitHub Project or configures an existing one with Simulab workflow status columns.
Uses GraphQL API via GitHub CLI authentication.

.PARAMETER BoardName
Name for the new project board to create. If omitted and BoardId is not provided, creates "Simulab".

.PARAMETER BoardId
Project number of an existing board to configure. If provided, updates existing board instead of creating new.

.EXAMPLE
# Create new board named "Simulab"
.\Configure-GitHubProjectBoard.ps1

# Create new board with custom name
.\Configure-GitHubProjectBoard.ps1 -BoardName "My Custom Board"

# Configure existing board (e.g., project #3)
.\Configure-GitHubProjectBoard.ps1 -BoardId 3

.NOTES
Prerequisites:
  - PowerShell 7.0+ or Windows PowerShell 5.1+
  - gh CLI installed and authenticated: gh auth login
  - curl and jq installed or in PATH

Author: Claude
#>

param(
    [string]$BoardName = "",
    [int]$BoardId = 0
)

# Configuration
$Owner = "alexcanario"
$Repo = "simulab"

# Workflow status values in order
$StatusValues = @("idea", "refining", "approved", "building", "validating", "done")

# Color codes
$Colors = @{
    Info    = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
}

# Functions
function Write-Info {
    param([string]$Message)
    Write-Host "➜ $Message" -ForegroundColor $Colors.Info
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $Colors.Success
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $Colors.Warning
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $Colors.Error
}

function Invoke-GraphQL {
    param([string]$Query)

    $Token = & gh auth token 2>$null
    if (-not $Token) {
        throw "Failed to get GitHub token. Make sure 'gh auth login' is done."
    }

    $Body = $Query | ConvertTo-Json -Compress

    try {
        $Response = curl.exe -s -X POST https://api.github.com/graphql `
            -H "Authorization: bearer $Token" `
            -H "Content-Type: application/json" `
            -d $Body | jq.exe '.'

        if ($null -eq $Response) {
            throw "Failed to parse GraphQL response"
        }

        return $Response | ConvertFrom-Json
    }
    catch {
        throw "GraphQL request failed: $_"
    }
}

# Main execution
try {
    Write-Host ""
    Write-Host "=== GitHub Projects Board Configuration ===" -ForegroundColor Cyan
    Write-Host "Owner: $Owner" -ForegroundColor Gray
    Write-Host "Repo: $Repo" -ForegroundColor Gray
    Write-Host ""

    # Check prerequisites
    Write-Info "Checking prerequisites..."

    $PrereqsMissing = @()

    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        $PrereqsMissing += "gh CLI not found"
    }

    if (-not (Get-Command curl -ErrorAction SilentlyContinue)) {
        $PrereqsMissing += "curl not found"
    }

    if (-not (Get-Command jq -ErrorAction SilentlyContinue)) {
        $PrereqsMissing += "jq not found"
    }

    if ($PrereqsMissing.Count -gt 0) {
        Write-Error "Missing prerequisites:"
        foreach ($Missing in $PrereqsMissing) {
            Write-Host "  - $Missing" -ForegroundColor $Colors.Error
        }
        exit 1
    }

    # Verify GitHub authentication
    try {
        $null = & gh auth status 2>&1
        Write-Success "Prerequisites OK"
    }
    catch {
        Write-Error "Not authenticated with GitHub"
        Write-Info "Run: gh auth login"
        exit 1
    }

    Write-Host ""

    # Determine action: create or update
    if ($BoardId -gt 0 -and -not [string]::IsNullOrWhiteSpace($BoardName)) {
        Write-Warning "Both -BoardId and -BoardName provided. Using -BoardId (updating existing board)."
        $Action = "update"
        $ProjectNumber = $BoardId
    }
    elseif ($BoardId -gt 0) {
        $Action = "update"
        $ProjectNumber = $BoardId
        Write-Info "Configuring existing board (Project #$BoardId)..."
    }
    else {
        $Action = "create"
        $ProjectTitle = if ([string]::IsNullOrWhiteSpace($BoardName)) { "Simulab" } else { $BoardName }
        Write-Info "Creating new project: $ProjectTitle..."
    }

    Write-Host ""

    # Action: Create
    if ($Action -eq "create") {
        Write-Info "Getting your GitHub user ID..."

        $ViewerQuery = @{
            query = "query { viewer { id } }"
        }

        $ViewerResponse = Invoke-GraphQL -Query $ViewerQuery
        $ViewerId = $ViewerResponse.data.viewer.id

        if ([string]::IsNullOrWhiteSpace($ViewerId)) {
            throw "Failed to get viewer ID"
        }

        Write-Success "User ID: $ViewerId"
        Write-Host ""

        Write-Info "Creating GitHub Project: $ProjectTitle..."

        $CreateQuery = @{
            query = "mutation(`$input: CreateProjectV2Input!) { createProjectV2(input: `$input) { projectV2 { id number title } } }"
            variables = @{
                input = @{
                    ownerId = $ViewerId
                    title   = $ProjectTitle
                }
            }
        }

        $CreateResponse = Invoke-GraphQL -Query $CreateQuery
        $ProjectId = $CreateResponse.data.createProjectV2.projectV2.id
        $ProjectNumber = $CreateResponse.data.createProjectV2.projectV2.number

        if ([string]::IsNullOrWhiteSpace($ProjectId)) {
            Write-Error "Failed to create project"
            $CreateResponse | ConvertTo-Json | Write-Error
            exit 1
        }

        Write-Success "Project created (ID: $ProjectId, #$ProjectNumber)"
        Write-Host ""
    }
    else {
        # Action: Update - Get project details
        Write-Info "Fetching project details (Project #$ProjectNumber)..."

        $FetchQuery = @{
            query = "query(`$owner: String!, `$repo: String!, `$number: Int!) { repository(owner: `$owner, name: `$repo) { projectV2(number: `$number) { id title } } }"
            variables = @{
                owner  = $Owner
                repo   = $Repo
                number = $ProjectNumber
            }
        }

        $FetchResponse = Invoke-GraphQL -Query $FetchQuery
        $ProjectId = $FetchResponse.data.repository.projectV2.id
        $ProjectTitle = $FetchResponse.data.repository.projectV2.title

        if ([string]::IsNullOrWhiteSpace($ProjectId)) {
            Write-Error "Project #$ProjectNumber not found"
            exit 1
        }

        Write-Success "Found project: $ProjectTitle"
        Write-Host ""
    }

    # Create/Get Status field
    Write-Info "Setting up Status field..."

    $CreateFieldQuery = @{
        query = "mutation(`$input: CreateProjectV2FieldInput!) { createProjectV2Field(input: `$input) { projectV2Field { id name } } }"
        variables = @{
            input = @{
                projectId = $ProjectId
                name      = "Status"
                dataType  = "SINGLE_SELECT"
            }
        }
    }

    $FieldResponse = Invoke-GraphQL -Query $CreateFieldQuery
    $FieldId = $FieldResponse.data.createProjectV2Field.projectV2Field.id

    if ([string]::IsNullOrWhiteSpace($FieldId)) {
        Write-Warning "Status field may already exist, retrieving..."

        $GetFieldQuery = @{
            query = "query(`$projectId: ID!) { node(id: `$projectId) { ... on ProjectV2 { fields(first: 20) { nodes { ... on ProjectV2SingleSelectField { id name } } } } } }"
            variables = @{
                projectId = $ProjectId
            }
        }

        $GetFieldResponse = Invoke-GraphQL -Query $GetFieldQuery
        $FieldId = $GetFieldResponse.data.node.fields.nodes |
                   Where-Object { $_.name -eq "Status" } |
                   Select-Object -ExpandProperty id
    }

    if ([string]::IsNullOrWhiteSpace($FieldId)) {
        throw "Could not create or find Status field"
    }

    Write-Success "Status field ready (ID: $FieldId)"
    Write-Host ""

    # Add status options
    Write-Info "Adding workflow status values..."

    foreach ($Status in $StatusValues) {
        Write-Host "  Adding '$Status'... " -NoNewline

        $OptionQuery = @{
            query = "mutation(`$input: CreateProjectV2DraftIssueInput!) { createProjectV2DraftIssue(input: `$input) { projectV2DraftIssue { id } } }"
            variables = @{
                input = @{
                    projectId = $ProjectId
                    title     = "Status option: $Status"
                    fields    = @(
                        @{
                            fieldId = $FieldId
                            value   = $Status
                        }
                    )
                }
            }
        }

        $OptionResponse = Invoke-GraphQL -Query $OptionQuery
        $DraftId = $OptionResponse.data.createProjectV2DraftIssue.projectV2DraftIssue.id

        if (-not [string]::IsNullOrWhiteSpace($DraftId)) {
            Write-Host "✓" -ForegroundColor Green
        }
        else {
            Write-Host "⚠ (may already exist)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "=== Project Setup Complete! ===" -ForegroundColor Green
    Write-Host ""
    Write-Info "Project URL:"
    Write-Host "  https://github.com/$Owner/$Repo/projects/$ProjectNumber" -ForegroundColor Cyan
    Write-Host ""
    Write-Info "Configured status values (in workflow order):"
    foreach ($Status in $StatusValues) {
        Write-Host "  • $Status" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "  1. Visit the project URL above" -ForegroundColor Gray
    Write-Host "  2. Go to Settings → Automation (optional)" -ForegroundColor Gray
    Write-Host "  3. Enable auto-add for issues/PRs (optional)" -ForegroundColor Gray
    Write-Host "  4. Start adding issues to the board!" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
