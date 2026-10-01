---
page: two-factor
locale: en
features: [F-11]
updated: 2026-09-21
---
# Two-factor sign-in

Two-factor sign-in asks for a six-digit code from an authenticator app on your phone after your password. Someone who learns your password still cannot sign in without your phone.

## Who can use it
Anyone signed in, for their own account. It is optional. If **My account** has no **Security** link, two-factor sign-in is not available yet.

## How to
### Turn it on
1. Install an authenticator app on your phone, such as Google Authenticator, Microsoft Authenticator or 2FAS.
2. Open **My account** and select **Security**.
3. Select **Turn on two-factor sign-in**.
4. In the app, add an account and scan the QR code. If you cannot scan it, type the key shown below the code into the app.
5. Type the six-digit code the app shows and select **Confirm and turn on**.
6. Ten recovery codes appear. Select **Download** or **Copy** and keep them somewhere safe, away from your phone. You will not see them again.
7. Tick **I saved my recovery codes** and select **Done**.

### Sign in with two-factor on
1. Open **Sign in** and enter your email and password as usual.
2. The page asks for the six-digit code. Type the code the app shows now and select **Sign in**.

### Sign in without your phone
1. After your password, select **Use a recovery code**.
2. Type one of your recovery codes and select **Sign in**. Each code works only once; **Security** shows how many are left.

### Create new recovery codes
1. On **Security**, select **Create new recovery codes**.
2. Type a code from the app (or a recovery code) and select **Create new codes**.
3. Save the ten new codes. The old ones stop working at once.

### Turn it off
1. On **Security**, select **Turn off two-factor sign-in** in the red section.
2. Enter your current password and a code from the app (or a recovery code).
3. Select **Turn off two-factor sign-in**. Your password alone signs you in again; the app's key and your recovery codes stop working.

Turning two-factor sign-in on or off does not sign you out anywhere: it applies from the next sign-in.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Six-digit code | The code the authenticator app shows | Required; it changes every 30 seconds and each code works once |
| Recovery code | One of the ten codes you saved, such as ABCDE-FGHJK | Required when used; upper or lower case, with or without the dash; works once |
| Current password | The password you sign in with | Required to turn two-factor sign-in off |

## Messages
| Message | What it means | What to do |
|---|---|---|
| The code is not correct or was already used. | The code is wrong, expired, or was already accepted | Wait for the next code in the app and try again |
| The code was not accepted. Enter your password again to try once more. | At sign-in, a wrong code ends the attempt | Enter your password again, then a new code |
| The sign-in took too long. Enter your password again. | More than 5 minutes passed between the password and the code | Start again from the password |
| Too many attempts. Try again in {0}. | Five wrong codes or passwords in a row locked the account for 15 minutes | Wait for the time shown |
| Too many attempts from this network. Try again in {0}. | 30 different account names failed to sign in from the same network in 15 minutes (a wrong code counts too); you are taken back to the password step | Wait for the time shown, or try from another network |
| The current password is not correct. | The password typed to turn two-factor sign-in off is wrong | Type it again |

## Related pages
- [Sign in and sign out](sign-in-and-sign-out.md)
- [My account](my-account.md)
- [Password](password.md)
