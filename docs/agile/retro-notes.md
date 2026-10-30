# Retro notes

## Retro notes
- F-190: a lane session cannot switch its own model, so an item whose Model line says opus ran on the session model (recorded in Decisions).
- F-190: a source-scan architecture test for "one copy of a rule" (WebAddressRuleTests) is cheap and kept the cleanup from regressing; candidate pattern for other duplicated-rule items.
- F-181: a locked bUnit test that calls a component's `EventCallback` straight from the test fails off the renderer's dispatcher, and a source-scan regex built on `[^<>]*` cannot match a tag that holds `=>`; both defects were found only after the lock. Candidate: the test phase runs its tests once against a throwaway implementation before the lock, or the rule `test-lock` lists these two traps.
- F-181: the owner's "autorizo o merge de <id>" without "validado" is not the validation for an item above `Risk: low` (feature-ship precondition 1); the lane asked twice and shipped only after the owner repeated it. Candidate: the lane's owner question says "validado e autorizo o merge" so one answer carries both.
