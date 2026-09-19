---
page: password
locale: en
features: [F-7, F-8]
updated: 2026-09-19
---
# Password: forgot, reset and change

Get back into your account when you forget the password, or change it while you are signed in.

## Who can use it
Anyone with a Simulab account. Changing the password needs you to be signed in; resetting it only needs access to your email.

## How to
### Reset a forgotten password
1. On **Sign in**, select **Forgot your password?**.
2. Enter the email of your account and select **Send link**. The page gives the same answer whether or not the address has an account.
3. Open the email "Reset your password" and select **Choose a new password**. The link works once, for 1 hour.
4. Type the new password twice and select **Save new password**.
5. You are taken to **Sign in** with "Your password was changed". Sign in with the new password.

If your account was still waiting for its email confirmation, resetting the password also confirms it. If the account was locked after too many attempts, the reset unlocks it at once.

### Change your password
1. Select the account icon in the top-right corner, then **My account**, then **Change password**.
2. Type your current password, then the new one twice, and select **Save**.
3. "Password changed. Other devices were signed out." You stay signed in on this device.

After a reset or a change, every other device is signed out within a minute, and you get an email "Your password was changed". If you did not make the change, use the link in that email to reset the password at once.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Email | The address of your account | Required, a valid email |
| Current password | The password you use today | Required (change only) |
| New password | The password you want to use | Required; at least 12 characters, with an uppercase letter, a digit and a symbol; different from the current one |
| Confirm new password | The new password again | Must be the same as the new password |

## Messages
| Message | What it means | What to do |
|---|---|---|
| If this email belongs to an account, a link to reset the password is on its way. | The request was received | Check your inbox and spam folder; you can ask again after 60 seconds |
| This link expired | Reset links last 1 hour | Enter your email on the same page to get a new link |
| This link is not valid | It was used already, replaced by a newer link, or copied only in part | Select **Ask for a new link** |
| Choose a password different from the current one. | The new password is the one you already have | Choose another one |
| The current password is not correct. | The current password does not match | Type it again; five wrong tries lock the account for 15 minutes |
| Too many attempts. Try again in {0}. | Five wrong current passwords locked the account | Wait for the time shown, or reset the password by email |
| Too many attempts from this device. Try again in an hour. | This device asked for too many links or resets | Wait an hour |

## Related pages
- [Sign in and sign out](sign-in-and-sign-out.md)
- [Create an account](create-account.md)
