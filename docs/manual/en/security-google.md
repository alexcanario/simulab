---
page: security-google
locale: en
features: [F-29]
updated: 2026-09-27
---
# Connect and disconnect Google

Connect a Google account to sign in with it besides your password. It does not have to be the address you signed up with. If **My account** has no **Security** link, or the page has no **Google** card, connecting Google is not available yet.

## Who can use it
Anyone signed in to their own account. It only ever changes your own account.

## How to
### Connect Google
1. Open **My account → Security**.
2. On the **Google** card, select **Connect Google**.
3. Choose your Google account and allow Simulab to see your name and email address.
4. You come back to **Security**, which confirms the connection and shows the address.

### Disconnect Google
1. On the **Google** card, select **Disconnect Google**.
2. Type your current password and confirm.
3. The card goes back to **Connect Google**. Your password still signs you in.

Disconnecting is not available while your account has no password: Google would be the only way in. The card says so, and **Create a password** takes you there.

After disconnecting, **Continue with Google** no longer reaches this account, even with the same address. Connect it again from this page.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Current password | Your password, to confirm the disconnection | Required; too many wrong tries lock the account for a while |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Wrong password. | The password typed to disconnect is not correct | Type it again; **Forgot your password?** on **Sign in** sends a new one |
| Too many attempts. Try again in mm:ss. | Too many wrong passwords in a row | Wait for the time shown |
| Your account was created with Google and has no password yet. Create one to confirm this action. | There is no password to confirm with, so Google cannot be disconnected | Select **Create a password** |
| This Simulab account is already connected to another Google account. | The account already has a different Google account connected | Disconnect that one first |
| An account with this email address already exists. Sign in to it instead. | That Google account is already connected to another Simulab account | Use a different Google account |
| The sign-in with Google took too long or was interrupted. Start again. | The attempt expired, or the page was reopened later | Select **Connect Google** again |
| Google has not verified this email address. Verify it with Google, then try again. | Google has not confirmed that the address is yours | Verify the address in your Google account |

## Related pages
- [Sign in with Google](google-sign-in.md)
- [My account](my-account.md)
- [Password](password.md)
- [Two-factor sign-in](two-factor.md)
