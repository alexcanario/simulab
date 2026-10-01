#!/usr/bin/env python3
"""
Configure GitHub Projects board for Simulab workflow.

Creates a new GitHub Project or configures an existing one with Simulab workflow
status columns using GitHub CLI and GraphQL API.

Usage:
    # Create new board named "Simulab"
    python configure_github_project_board.py

    # Create new board with custom name
    python configure_github_project_board.py -n "My Custom Board"
    python configure_github_project_board.py --name "My Custom Board"

    # Configure existing board (e.g., project #3)
    python configure_github_project_board.py -i 3
    python configure_github_project_board.py --id 3

Prerequisites:
    - Python 3.7+
    - GitHub CLI installed and authenticated: gh auth login
    - curl and jq installed or in PATH
"""

import sys
import json
import subprocess
import argparse
from typing import Dict, Optional, Tuple
from dataclasses import dataclass

# Configuration
OWNER = "alexcanario"
REPO = "simulab"
STATUS_VALUES = ["idea", "refining", "approved", "building", "validating", "done"]

# Colors
class Colors:
    INFO = "\033[0;36m"      # Cyan
    SUCCESS = "\033[0;32m"   # Green
    WARNING = "\033[1;33m"   # Yellow
    ERROR = "\033[0;31m"     # Red
    RESET = "\033[0m"        # Reset

    @staticmethod
    def disable():
        """Disable colors (for CI/CD environments)"""
        for attr in dir(Colors):
            if not attr.startswith("_") and attr != "disable":
                setattr(Colors, attr, "")


# Output functions
def info(message: str) -> None:
    print(f"{Colors.INFO}➜{Colors.RESET} {message}")


def success(message: str) -> None:
    print(f"{Colors.SUCCESS}✓{Colors.RESET} {message}")


def warning(message: str) -> None:
    print(f"{Colors.WARNING}⚠{Colors.RESET} {message}")


def error(message: str) -> None:
    print(f"{Colors.ERROR}✗{Colors.RESET} {message}", file=sys.stderr)


def fatal(message: str, exit_code: int = 1) -> None:
    error(message)
    sys.exit(exit_code)


# GitHub CLI functions
def get_gh_token() -> str:
    """Get GitHub CLI authentication token."""
    try:
        result = subprocess.run(
            ["gh", "auth", "token"],
            capture_output=True,
            text=True,
            check=True,
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError:
        fatal("Failed to get GitHub token. Make sure 'gh auth login' is done.")
    except FileNotFoundError:
        fatal("GitHub CLI (gh) not found. Install it: https://cli.github.com")


def check_gh_authenticated() -> bool:
    """Check if GitHub CLI is authenticated."""
    try:
        subprocess.run(
            ["gh", "auth", "status"],
            capture_output=True,
            check=True,
        )
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        return False


def check_prerequisites() -> None:
    """Check if all prerequisites are installed."""
    missing = []

    # Check gh
    try:
        subprocess.run(
            ["gh", "--version"],
            capture_output=True,
            check=True,
        )
    except (subprocess.CalledProcessError, FileNotFoundError):
        missing.append("gh CLI not found")

    # Check curl
    try:
        subprocess.run(
            ["curl", "--version"],
            capture_output=True,
            check=True,
        )
    except (subprocess.CalledProcessError, FileNotFoundError):
        missing.append("curl not found")

    # Check jq
    try:
        subprocess.run(
            ["jq", "--version"],
            capture_output=True,
            check=True,
        )
    except (subprocess.CalledProcessError, FileNotFoundError):
        missing.append("jq not found")

    if missing:
        error("Missing prerequisites:")
        for m in missing:
            print(f"  - {m}", file=sys.stderr)
        sys.exit(1)


def run_graphql(query: Dict) -> Dict:
    """Execute a GraphQL query via curl."""
    token = get_gh_token()
    query_json = json.dumps(query)

    try:
        result = subprocess.run(
            [
                "curl",
                "-s",
                "-X",
                "POST",
                "https://api.github.com/graphql",
                "-H",
                f"Authorization: bearer {token}",
                "-H",
                "Content-Type: application/json",
                "-d",
                query_json,
            ],
            capture_output=True,
            text=True,
            check=True,
        )

        return json.loads(result.stdout)
    except subprocess.CalledProcessError as e:
        fatal(f"GraphQL request failed: {e.stderr}")
    except json.JSONDecodeError:
        fatal("Failed to parse GraphQL response")


# Main logic
def get_viewer_id() -> str:
    """Get the authenticated user's viewer ID."""
    info("Getting your GitHub user ID...")

    query = {"query": "query { viewer { id } }"}
    response = run_graphql(query)

    viewer_id = response.get("data", {}).get("viewer", {}).get("id")
    if not viewer_id:
        fatal("Failed to get viewer ID")

    success(f"User ID: {viewer_id}")
    return viewer_id


def create_project(title: str) -> Tuple[str, int]:
    """Create a new GitHub Project."""
    info(f"Creating GitHub Project: {title}...")

    viewer_id = get_viewer_id()

    query = {
        "query": "mutation($input: CreateProjectV2Input!) { createProjectV2(input: $input) { projectV2 { id number title } } }",
        "variables": {
            "input": {
                "ownerId": viewer_id,
                "title": title,
            }
        },
    }

    response = run_graphql(query)

    project_v2 = response.get("data", {}).get("createProjectV2", {}).get("projectV2")
    if not project_v2:
        error("Failed to create project")
        print(json.dumps(response, indent=2), file=sys.stderr)
        sys.exit(1)

    project_id = project_v2.get("id")
    project_num = project_v2.get("number")

    success(f"Project created (ID: {project_id}, #{project_num})")
    return project_id, project_num


def get_project(project_number: int) -> Tuple[str, str]:
    """Get existing project details."""
    info(f"Fetching project details (Project #{project_number})...")

    query = {
        "query": "query($owner: String!, $repo: String!, $number: Int!) { repository(owner: $owner, name: $repo) { projectV2(number: $number) { id title } } }",
        "variables": {
            "owner": OWNER,
            "repo": REPO,
            "number": project_number,
        },
    }

    response = run_graphql(query)

    project_v2 = response.get("data", {}).get("repository", {}).get("projectV2")
    if not project_v2:
        fatal(f"Project #{project_number} not found")

    project_id = project_v2.get("id")
    project_title = project_v2.get("title")

    success(f"Found project: {project_title}")
    return project_id, project_title


def create_status_field(project_id: str) -> str:
    """Create or get the Status field."""
    info("Setting up Status field...")

    query = {
        "query": "mutation($input: CreateProjectV2FieldInput!) { createProjectV2Field(input: $input) { projectV2Field { id name } } }",
        "variables": {
            "input": {
                "projectId": project_id,
                "name": "Status",
                "dataType": "SINGLE_SELECT",
            }
        },
    }

    response = run_graphql(query)

    field = response.get("data", {}).get("createProjectV2Field", {}).get("projectV2Field")
    field_id = field.get("id") if field else None

    if not field_id:
        warning("Status field may already exist, retrieving...")

        query = {
            "query": "query($projectId: ID!) { node(id: $projectId) { ... on ProjectV2 { fields(first: 20) { nodes { ... on ProjectV2SingleSelectField { id name } } } } } }",
            "variables": {"projectId": project_id},
        }

        response = run_graphql(query)

        fields = response.get("data", {}).get("node", {}).get("fields", {}).get("nodes", [])
        for field in fields:
            if field.get("name") == "Status":
                field_id = field.get("id")
                break

    if not field_id:
        fatal("Could not create or find Status field")

    success(f"Status field ready (ID: {field_id})")
    return field_id


def add_status_values(project_id: str, field_id: str) -> None:
    """Add status values to the Status field."""
    info("Adding workflow status values...")

    for status in STATUS_VALUES:
        print(f"  Adding '{status}'... ", end="", flush=True)

        query = {
            "query": "mutation($input: CreateProjectV2DraftIssueInput!) { createProjectV2DraftIssue(input: $input) { projectV2DraftIssue { id } } }",
            "variables": {
                "input": {
                    "projectId": project_id,
                    "title": f"Status option: {status}",
                    "fields": [
                        {
                            "fieldId": field_id,
                            "value": status,
                        }
                    ],
                }
            },
        }

        response = run_graphql(query)

        draft_id = (
            response.get("data", {})
            .get("createProjectV2DraftIssue", {})
            .get("projectV2DraftIssue", {})
            .get("id")
        )

        if draft_id:
            print(f"{Colors.SUCCESS}✓{Colors.RESET}")
        else:
            print(f"{Colors.WARNING}⚠ (may already exist){Colors.RESET}")


def print_summary(project_number: int) -> None:
    """Print setup summary."""
    print()
    print(f"{Colors.SUCCESS}=== Project Setup Complete! ==={Colors.RESET}")
    print()
    info("Project URL:")
    print(f"  https://github.com/{OWNER}/{REPO}/projects/{project_number}")
    print()
    info("Configured status values (in workflow order):")
    for status in STATUS_VALUES:
        print(f"  • {status}")
    print()
    info("Next steps:")
    print("  1. Visit the project URL above")
    print("  2. Go to Settings → Automation (optional)")
    print("  3. Enable auto-add for issues/PRs (optional)")
    print("  4. Start adding issues to the board!")
    print()


def main() -> None:
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Configure GitHub Projects board for Simulab workflow",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  Create new board named "Simulab" (default):
    python configure_github_project_board.py

  Create new board with custom name:
    python configure_github_project_board.py -n "My Board"
    python configure_github_project_board.py --name "My Board"

  Configure existing board (project #3):
    python configure_github_project_board.py -i 3
    python configure_github_project_board.py --id 3
        """,
    )

    parser.add_argument(
        "-n",
        "--name",
        type=str,
        default="",
        help="Name for the new project to create",
    )

    parser.add_argument(
        "-i",
        "--id",
        type=int,
        default=0,
        help="Project number of an existing board to configure",
    )

    parser.add_argument(
        "--no-color",
        action="store_true",
        help="Disable colored output",
    )

    args = parser.parse_args()

    # Handle colors
    if args.no_color or not sys.stdout.isatty():
        Colors.disable()

    # Validate arguments
    if args.id > 0 and args.name:
        warning("Both -n/--name and -i/--id provided. Using -i/--id (updating existing board).")

    print()
    print(f"{Colors.INFO}=== GitHub Projects Board Configuration ==={Colors.RESET}")
    print(f"Owner: {OWNER}")
    print(f"Repo: {REPO}")
    print()

    # Check prerequisites
    info("Checking prerequisites...")
    check_prerequisites()

    # Check authentication
    if not check_gh_authenticated():
        fatal("Not authenticated with GitHub. Run: gh auth login")

    success("Prerequisites OK")
    print()

    try:
        # Determine action: create or update
        if args.id > 0:
            # Update existing project
            project_id, project_title = get_project(args.id)
            project_number = args.id
        else:
            # Create new project
            board_name = args.name if args.name else "Simulab"
            project_id, project_number = create_project(board_name)
            project_title = board_name

        print()

        # Configure Status field
        field_id = create_status_field(project_id)
        print()

        # Add status values
        add_status_values(project_id, field_id)

        # Print summary
        print_summary(project_number)

    except KeyboardInterrupt:
        print()
        warning("Interrupted by user")
        sys.exit(1)
    except Exception as e:
        fatal(f"Unexpected error: {e}")


if __name__ == "__main__":
    main()
