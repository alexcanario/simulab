---
feature: F-66
epic: Foundation and identity
status: approved
board: 105
version: 1
---
# Emails leave the cloud environments

Technical terms: [glossary](../glossary.md)

## Summary
Staging and production send real emails (sign-up verification, password reset, password changed, farewell) through Azure Communication Services Email, called over its HTTP API with the Api's managed identity. Locally Mailpit stays, over SMTP. Testers in Brazil cannot finish sign-up on staging without it. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: nothing to build. F-64 (staging on Azure, `refining`) to validate.
- Waits on (to start): nothing. The provider is chosen (Decisions, 2026-10-04) and lives in the Azure subscription of ADR-0002.
- Needed to validate: the staging environment created by F-64 — owner; a real mailbox outside the company (for example Gmail) to receive the emails — owner.
- Suggested path: `/agile:refine` → `/agile:build` (with the `system-design` and `architect` passes: it adds an Azure resource and a role assignment).
- Parallel with: none recommended. F-64 also edits `src/Hosts/Simulab.AppHost/AzureDeployment.cs` and `docs/infra.md`: build F-66 after F-64 merges, or expect conflicts there.

## Goal
A tester on staging receives the sign-up verification email in their real mailbox and finishes the sign-up, with no secret to create, store or rotate for the email.

## What already exists (verified 2026-10-04)
- `IEmailSender` with one implementation, `SmtpEmailSender` (MailKit), in `src/BuildingBlocks/Simulab.Email/`. `EmailOptions` (section `Email`) has `Host`, `Port`, `FromAddress`, `FromName`, `UserName`, `Password`, `UseStartTls`. `AddEmailSender` always registers the SMTP sender (`src/Hosts/Simulab.Api/Program.cs:30`).
- Every identity email leaves through the job worker (F-13): `SendEmailJobHandler` calls `IEmailSender`; a failure is retried 5 times (1-2-4-8-16 minutes).
- Senders of identity emails: `VerificationMailer` and `PasswordMailer` (Identity module), plus the farewell email of F-10.
- App host: Mailpit only in run mode (`AppHost.cs:35`); in publish mode `AzureDeployment` adds the Container Apps environment, Key Vault, PostgreSQL and Redis (F-62), and nothing for email. A deployed Api today would try `localhost:1025` and every email job would fail after 5 retries.
- `docs/infra.md` lists `Email:SendGrid:ApiKey` and ADR-0001 #24 says "SendGrid in the cloud"; no SendGrid code exists.
- No Aspire hosting package exists for Azure Communication Services (NuGet search, 2026-10-04): the resource comes from a Bicep template in the app host.

## Users and use cases
- UC1 A person signs up on staging (or production) with their real address and receives the verification email in their mailbox.
- UC2 A user on staging asks for a password reset and receives the reset email; the password-changed and farewell emails arrive the same way.
- UC3 The owner deploys staging with `aspire deploy` and the email service is created and wired with no manual step and no secret.
- UC4 A developer runs the app locally and every email still lands in Mailpit.

## Business rules
- BR1 Staging and Production send through Azure Communication Services Email, over its HTTP API, authenticated by the Api's managed identity. No API key, connection string with a key or client secret exists for email in any environment.
- BR2 Development (and the test hosts) keep SMTP to Mailpit, unchanged.
- BR3 The provider is chosen by configuration (`Email:Provider`: `Smtp`, the default, or `AzureCommunicationServices`), never by environment name inside the sender.
- BR4 In Staging and Production the Api refuses to start when the provider is not `AzureCommunicationServices`, or when its endpoint or sender address is missing; the message names the missing keys. A misconfiguration shows at deploy, not at the first sign-up.
- BR5 Staging sends from the Azure-managed domain of the email service (`DoNotReply@<id>.azurecomm.net`), display name `Simulab`. Production uses the same until F-87 brings Simulab's own domain.
- BR6 Any recipient address is allowed in staging; there is no allow-list.
- BR7 A message the service refuses or reports as failed makes `SendAsync` throw with the service's error code and the endpoint, never a token or a recipient's address in the log beyond what the job already records; the job worker retries it as today (F-13).
- BR8 The email service keeps its data in Brazil when the service offers that data location (ADR-0003). If it does not, the build stops and asks (false premise).
- BR9 The app host creates, at deploy, the Email Communication Service, its Azure-managed domain, the Communication Service linked to that domain, and a role assignment that lets only the Api's identity send. The Web gets no email permission.

## Screens and API
- No screen and no endpoint changes. No new UI text.
- Configuration (section `Email`): `Provider`, `AzureCommunicationServices:Endpoint`; `FromAddress` and `FromName` as today. Set by the app host in publish mode from the Bicep outputs.
- `docs/infra.md`: the `Email:SendGrid:ApiKey` row is replaced by the new keys; the deploy section says what the deploy creates for email.

## Acceptance criteria
- AC1 Given `Email:Provider` is `AzureCommunicationServices`, when an email job runs, then the ACS sender sends one message with the configured sender and display name, the recipient, the subject, the HTML body and the text body (BR1, BR3).
- AC2 Given `Email:Provider` is absent or `Smtp`, when the Api starts in Development, then the SMTP sender is registered and Mailpit receives the sign-up email as today (BR2).
- AC3 Given the service refuses a message or reports it as failed, when the sender sends it, then `SendAsync` throws with the error code and the endpoint, and the job is retried (BR7).
- AC4 Given the environment is Staging or Production, when the provider is `Smtp`, or the endpoint or the sender address is missing, then the Api refuses to start with a message naming the missing keys (BR4).
- AC5 Given `aspire publish -e Staging`, when the model is built, then it contains the Email Communication Service, the Azure-managed domain, the Communication Service with the Brazil data location, a role assignment for the Api's identity only, and the Api's `Email__Provider`, `Email__AzureCommunicationServices__Endpoint` and `Email__FromAddress` settings; no email secret or parameter exists (BR1, BR5, BR8, BR9).
- AC6 Given staging is deployed, when a tester signs up with a real address outside the company, then the verification email arrives in that mailbox from the `azurecomm.net` sender, and the link finishes the sign-up (UC1, UC3; validation script).
- AC7 Given staging is deployed, when the tester asks for a password reset, then the reset email arrives in the same mailbox (UC2; validation script).
- AC8 No new UI text is added; the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-10-04 — Provider: Azure Communication Services Email, not SendGrid (owner). Reason: SendGrid retired its free plan on 2025-05-27 (60-day trial, then from US$ 19.95/month); ACS lives in the same Azure subscription and bill as ADR-0002 and is paid per email (US$ 0.00025 per email plus US$ 0.00012 per MB, Microsoft pricing page), cents per month at our volume. The build amends ADR-0001 #24 and `docs/infra.md`.
- 2026-10-04 — HTTP API with managed identity instead of SMTP (owner, follow-up round). Reason: ACS SMTP needs a Microsoft Entra application and its client secret, which the deploy cannot create: one manual step per environment and a secret that expires. The HTTP API with the Api's managed identity needs no secret. First answer was "keep the current SMTP"; changed after this premise came out.
- 2026-10-04 — Staging sends from the Azure-managed domain; Simulab's own domain is F-87 (owner). Reason: no DNS work now; production needs its own domain at the first release.
- 2026-10-04 — The app host creates the email resources at deploy (owner). Reason: same as PostgreSQL and Key Vault in F-62; no manual step.
- 2026-10-04 — Any recipient in staging (owner). Reason: testers are real people signing up; an allow-list would block them.
- 2026-10-04 — Missing configuration in the cloud stops the Api at start (owner). Reason: the error shows at deploy, not when a tester complains.
- 2026-10-04 — New packages approved (owner, follow-up round): `Azure.Communication.Email` 1.1.0 (MIT) and `Azure.Identity` 1.21.0 (MIT), latest stable on nuget.org on 2026-10-04. Tests need no new package: the ACS client is faked through its public virtual members.
- 2026-10-04 — `SendAsync` waits until the service accepts or rejects the message (`WaitUntil.Completed`), not only until the request is sent. Reason: a rejected message must throw so the job retries (BR7); delivery to the mailbox (bounces, spam) stays out of scope (F-88). Technical choice, Claude.
- 2026-10-04 — The ACS resources come from one Bicep template added by `AzureDeployment` (`AddBicepTemplate`), with the Api's identity passed in for the role assignment. Reason: no Aspire hosting package for ACS exists; `Aspire.Hosting.Azure` is already referenced. The exact role (built-in or custom with only the send permission) is settled in the `system-design` pass. Technical choice, Claude.
- 2026-10-04 — The email processor is now Microsoft (ACS). F-71 (privacy policy, `refining`) names the processors; it is told in the report, not edited from here.

## Out of scope
- Simulab's own sending domain, SPF/DKIM and DNS: F-87.
- Bounced and spam-flagged emails, delivery reports: F-88.
- A recipient allow-list in staging.
- SendGrid code of any kind.
- Changing the email templates or their languages.
- Creating the staging environment itself: F-64.

## Open questions
