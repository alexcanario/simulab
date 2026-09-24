---
page: roles
locale: en
features: [F-6, F-9, F-14, F-33]
updated: 2026-09-24
---
# Roles and permissions

Every account holds one or more roles, and each role carries the permissions that decide what the account can see and do in Simulab.

## Who can use it
Everyone has a role. Only an account with the permission "Manage roles and user roles" — the Admin role has it — can open the Roles page.

## How to
### Know your role
1. Every new account starts as a **Student**: you can practice and follow your progress.
2. **Curators** import and publish content; **Admins** manage the catalog, accounts, roles and plans.
3. An Admin gives you another role on the [Users](users.md) page.

### See the roles (Admins)
1. Choose **Roles**, in the **Administration** section of the side menu.
2. Student, Curator and Admin carry the **System** badge: their names never change and they cannot be deleted.
3. Each line shows how many permissions the role carries and how many accounts hold it. Click the number of accounts to see them on the Users page.

### Add or change a role
1. Choose **Add**, or **Edit** on the role's line.
2. Type a name of 2 to 50 characters. A system role keeps its name, so the field is read-only for it.
3. Tick the permissions the role carries. Each one shows what it allows. They are grouped by area: "Identity and access" for roles and accounts, "Catalog" for the exam catalog.
4. Choose **Save**. The people who hold that role get the new permissions within seconds; their menu changes the next time they open a page.

### Delete a role
1. **Delete** is offered only for roles you created, and only while no account holds them. When an account still holds it, the button is off and says how many.
2. Take the role away from those accounts on the [Users](users.md) page, then delete it.
3. A deleted role disappears everywhere and grants nothing, but its name stays taken.

## Fields
| Field | Meaning | Rules |
|---|---|---|
| Name | How the role is shown everywhere | Required, 2 to 50 characters, cannot repeat another role's name, including deleted ones |
| Permissions | What the role allows | Any number, from the list shown; saving replaces the whole set |

## Messages
| Message | What it means | What to do |
|---|---|---|
| A role with this name already exists (deleted roles included). | Another role, sometimes a deleted one, already uses that name. | Pick a different name. |
| Use 2 to 50 characters. | The name is too short or too long. | Shorten or lengthen it. |
| System roles cannot be renamed or deleted. | Student, Curator and Admin are fixed. | Change their permissions instead, or create a role of your own. |
| The Admin role must keep "Manage roles and user roles". | Without it nobody could manage roles again. | Leave that permission ticked. |
| Remove this role from every user before deleting it. | Someone got the role while the page was open. | Reload the page and take the role away first. |
| This would leave nobody able to manage roles. | The change would remove the last account that manages roles. | Give that permission to another active account first. |
| Page not found | Your account does not manage roles. | Ask an Admin if you believe you should. |

## Related pages
- [Users](users.md): find an account and change its roles
- [Role history](role-history.md): every change of a role — choose **History** on its line
- [Getting around](getting-around.md): menu, light and dark mode, language and keyboard
- [Simulab](index.md)
