---
page: my-account
locale: en
features: [F-8, F-10, F-11, F-16]
updated: 2026-09-22
---
# My account

See your email, change the name shown in the account menu, and choose the language Simulab opens in and writes your emails in.

## Who can use it
Anyone who is signed in. Each person sees and changes only their own account.

## How to
### Open My account
1. Select the account icon in the top-right corner, then **My account**.

### Change your display name or preferred language
1. In **Display name**, type the name you want to see in the account menu, or leave it blank to use your email only.
2. In **Preferred language**, choose Português (Brasil), Português (Portugal) or English.
3. Select **Save**. The page reloads in the chosen language with "Your profile was saved.", and the account menu shows the new name.

**Cancel** undoes your changes and brings back the saved values. If you try to leave the page with unsaved changes, Simulab asks whether to discard them.

### How the preferred language works
- Every time you sign in, Simulab opens in your preferred language, on any device, even when the browser is set to another language.
- Changing the language with the globe in the top bar while signed in also changes your preferred language.
- Your emails (password reset, password changed notice) are written in your preferred language.
- A device that was already signed in uses the new language from its next sign-in.

### Change your password
Select **Change password**, just below the fields. See [Password](password.md).

### Protect your account with a code
Select **Security**, next to **Change password**, to turn on two-factor sign-in. The link appears only where two-factor sign-in is available. See [Two-factor sign-in](two-factor.md).

### Download your data
**Your data**, just above **Erase my account**, gives you a copy of everything Simulab holds about you, as one JSON file.
1. Select **Download my data**.
2. Type your current password and select **Download**. The password protects the file: it holds personal data.
3. The browser saves `simulab-my-data-<date>.json`, and a message says the download started.

The file holds your account (email, display name, phone number, preferred language, dates), the roles you hold, the terms and privacy policy you accepted with the IP address they were accepted from, the changes made to your roles, and how many devices are signed in. It never holds your password or any security code. Nothing is stored on the server, and you can download it as often as you like.

Every download sends you an email saying so. If that email arrives and it was not you, someone knows your password: change it at once.

### Erase your account
At the bottom of the page, **Erase my account** removes your personal data for good. See [Erase your account](erase-account.md).

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Email | The address you sign in with | Read only; it cannot be changed here |
| Display name | The name shown in the account menu | Optional; at most 120 characters |
| Preferred language | The language the app opens in when you sign in, and the language of your emails | Required; one of the three languages in the list |

## Messages
| Message | What it means | What to do |
|---|---|---|
| Your profile was saved. | The name and language were stored | Nothing |
| Use at most 120 characters. | The display name is too long; **Save** is disabled | Shorten the name |
| Choose one of the languages in the list. | The language sent is not one of the three available | Choose a language from the list and save again |
| We could not load your profile. | Simulab could not read your data right now | Select **Try again** |
| Your download has started. A confirmation email is on its way. | The file was built and handed to the browser | Nothing; look for the file in your downloads |
| The current password is not correct. | The password typed in the download dialog is wrong | Type it again; too many attempts lock the account for a while |

## Related pages
- [Password](password.md)
- [Two-factor sign-in](two-factor.md)
- [Erase your account](erase-account.md)
- [Getting around](getting-around.md)
