---
page: role-history
locale: en
features: [F-14]
updated: 2026-09-21
---
# Role history

The Role history page shows every change made to roles and to the roles of an account: who made it, when, and what changed. It answers questions such as "who gave this person Admin, and when?".

## Who can use it
Only an account with the permission "Manage roles and user roles" — the Admin role has it.

## What is recorded
- A role created, changed (renamed or its permissions changed) or deleted on the [Roles](roles.md) page.
- The roles of an account changed on the [Users](users.md) page.

Changes that Simulab makes by itself are not recorded: the Student role given at sign-up, and the roles removed when someone erases their account. A change that was refused (for example, one that would leave nobody able to manage roles), or a save that changed nothing, leaves no entry. Entries are kept and never changed.

## How to
### See the history
1. Choose **Role history**, in the **Administration** section of the side menu.
2. The newest changes come first. Choose the **When** column header to see the oldest first.

### See the history of one role or one account
1. On the [Roles](roles.md) or [Users](users.md) page, choose **History** on the line of the role or account.
2. The Role history page opens already filtered. For an account, a label "User: …" shows the filter; choose its × to see everything again.

### Narrow the list
- **Role**: changes to that role, and changes that gave it to or took it from an account. Deleted roles are listed as "(deleted)".
- **Changed by**: changes made by that account.
- **Period**: the last 7, 30 or 90 days, or all time.
- The search box finds the account whose roles changed, by email or name.

The filters stay in the page address, so a reload or a shared link keeps them.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| When | Date and time of the change | Shown in your time zone |
| Changed by | The account that made the change | "Erased account" if that account was erased since |
| Action | Role created, Role changed, Role deleted, or User's roles changed | |
| Role or user | The role (with the name it had then) or the account whose roles changed | |
| Changes | The old and new name, what was added and what was removed | Long lists are cut; point at them to read all |

## Messages
| Message | What it means | What to do |
|---|---|---|
| No role change has been recorded yet. | Nobody has changed a role since this page existed. | Nothing; changes appear here as they are made. |
| No change matches these filters. | The filters or the search leave nothing. | Clear a filter or the search. |
| Erased account | The account erased itself after the change; its email is no longer kept. | Nothing; the entry stays. |
| Page not found | Your account does not manage roles. | Ask an Admin if you believe you should. |

## Related pages
- [Roles and permissions](roles.md): what each role allows, and how to change a role
- [Users](users.md): find an account and change its roles
- [Simulab](index.md)
