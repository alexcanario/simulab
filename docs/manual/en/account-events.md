---
page: account-events
locale: en
features: [F-21]
updated: 2026-09-23
---
# Account events

The Account events page shows what has happened to accounts: who signed in and from where, who failed to sign in, which accounts were locked, and who changed a password or turned two-factor off. It answers questions such as "did someone get into this account?".

## Who can use it
Only an account with the permission "Manage roles and user roles" — the Admin role has it.

## What is recorded
- **Signed in** and **Sign-in failed**, with how it was attempted (password, Google, the app code or a recovery code) or why it failed.
- **Account locked**, when a run of failures crosses the limit. The attempts that follow, while the lockout lasts, are recorded as failures.
- **Signed out**, **Password changed**, **Password reset requested**, **Password reset**.
- **Two-factor turned on**, **Two-factor turned off**, **Recovery codes regenerated**.
- **Account erased**.

A sign-in attempt on an email address that belongs to no account is recorded as "Unknown account": the address that was typed is never stored. A silent renewal of your session is not a sign-in and is not recorded. Entries are kept and never changed.

## How to
### See the events
1. Choose **Account events**, in the **Administration** section of the side menu.
2. The newest events come first. Choose the **When** column header to see the oldest first.

### See the events of one account
1. On the [Users](users.md) page, choose **Security events** on the line of the account.
2. The page opens already filtered, and a label "Account: …" shows the filter; choose its × to see everything again.

### See everything that came from one address
Choose the address in the **From** column. A label "From: …" appears and the list shows every event from that address, whatever the account. Choose its × to remove it.

### Narrow the list
- **Event**: only that kind of event.
- **Period**: the last 7, 30 or 90 days, or all time.
- The search box finds the account by email or name.

The filters stay in the page address, so a reload or a shared link keeps them.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| When | Date and time of the event | Shown in your time zone |
| Account | The account the event is about | "Erased account" if it was erased since; "Unknown account" if the attempt matched none |
| Event | What happened | |
| Details | How a sign-in was passed, or why it failed | Empty when neither applies |
| From | The address the request came from | Choose it to see every event from it; empty when it is not known, or when the account was erased |

## What is not kept
An event holds no email address, no name and no information about the browser or device. The email shown is read from the account at the moment you look at the page, so an account erased since shows as "Erased account". When an account is erased, the addresses on all of its events are cleared; the events themselves stay, so the erasure can still be traced.

## Messages
| Message | What it means | What to do |
|---|---|---|
| No account event has been recorded yet. | Nothing has happened since this page existed. | Nothing; events appear here as they happen. |
| No event matches these filters. | The filters or the search leave nothing. | Clear a filter or the search. |
| Erased account | The account was erased after the event; its email is no longer kept. | Nothing; the entry stays. |
| Unknown account | Someone tried to sign in with an address that belongs to no account. | Watch for many of these from one address. |
| Page not found | Your account does not manage roles. | Ask an Admin if you believe you should. |

## Related pages
- [Users](users.md): find an account and change its roles
- [Role history](role-history.md): who changed which role or whose roles, and when
- [Sign in and sign out](sign-in-and-sign-out.md): sign in, sign out, lockout
- [Two-factor sign-in](two-factor.md): a code from your phone after your password, and recovery codes
- [Simulab](index.md)
