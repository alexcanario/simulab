---
page: users
locale: en
features: [F-9, F-14, F-21]
updated: 2026-09-23
---
# Users

The Users page lists the accounts of Simulab and is where you give or take a role.

## Who can use it
Only an account with the permission "Manage roles and user roles" — the Admin role has it.

## How to
### Find an account
1. Choose **Users**, in the **Administration** section of the side menu.
2. Type part of an email address or a name in the search box.
3. Or pick a role in **Role** to see only the accounts that hold it. Clicking the number of accounts on the [Roles](roles.md) page opens this page already filtered.

### Change someone's roles
1. Choose **Edit roles** on the account's line.
2. Tick every role the account should hold, untick the others. An account can hold several roles, and its permissions add up.
3. Choose **Save**. The account gets the new permissions within seconds; its menu changes the next time it opens a page.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Email | The address the account signs in with | Shown as it was registered |
| Name | The display name the person chose | May be empty |
| Status | **Pending** while the email is not confirmed, **Active** afterwards | An account of either status can hold roles; a pending one cannot sign in yet |
| Roles | The roles the account holds | None, one or several |

## Messages
| Message | What it means | What to do |
|---|---|---|
| No user matches "…". | No account has that text in its email or name. | Check the spelling, or clear the search. |
| This would leave nobody able to manage roles. | The change would remove the last account that manages roles. | Give that permission to another active account first. |
| This user no longer exists. | The account was removed while the page was open. | Reload the page. |
| One of the roles no longer exists. Reload the page and try again. | A role was deleted while the dialog was open. | Reload the page. |
| Page not found | Your account does not manage roles. | Ask an Admin if you believe you should. |

## Related pages
- [Roles and permissions](roles.md): what each role allows
- [Role history](role-history.md): every change of an account's roles — choose **History** on its line
- [Account events](account-events.md): sign-ins and account changes — choose **Security events** on its line
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
