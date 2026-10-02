# Store checklist v{{version}}

Written by `/agile:publish` for this release. The plugin uploads nothing to either store: every step below is yours.

## Android, this release
- The signed bundle: `{{aab}}` (application id `{{applicationId}}`).
- Version shown to people: `ApplicationDisplayVersion` {{displayVersion}}. Version Play compares: `ApplicationVersion` (versionCode) {{applicationVersion}}. Play rejects a versionCode it has already seen; the ship raises it by 1 at every release.
- Play Console: Testing > Internal testing > Create new release > upload the `.aab`, add the release notes from `docs/releases/v{{version}}.md`, save, roll out to testers.
- Install from the testers' link on a real phone and run the main flow once.
- Then promote that same release to Closed testing or Production (Promote release); never rebuild for the promotion.

## Android, first release only
- Create the app in Play Console with the id `{{applicationId}}`: the id is permanent once uploaded.
- Play App Signing: accept it. The key in `ANDROID_SIGNING_KEYSTORE` is the **upload key**; Google holds the app signing key. A lost upload key is reset through Play support, so keep a backup of the keystore and of its passwords outside this machine.
- Store listing: name, short and full description, icon, feature graphic, phone screenshots. The plugin writes no listing text; the brand and tone come from `product/brief.md`.
- Privacy policy URL (a public page).
- Data safety form: from the data the app collects (quiz 9b of the bootstrap).
- Content rating questionnaire and target audience.

## iOS, on a Mac (not run by the plugin)
- Apple Developer Program membership, then the bundle id (same as `{{applicationId}}` unless chosen otherwise) registered in the developer account.
- A distribution certificate and an App Store provisioning profile for that bundle id.
- On the Mac: `dotnet publish {{app}}.Mobile.csproj -f {{iosTfm}} -c Release -p:ArchiveOnBuild=true -p:CodesignKey="<distribution certificate name>" -p:CodesignProvision="<provisioning profile name>"`.
- Upload the resulting `.ipa` with Transporter, then TestFlight to test it.
- App Store Connect: App Privacy answers, screenshots, then submit for review.
