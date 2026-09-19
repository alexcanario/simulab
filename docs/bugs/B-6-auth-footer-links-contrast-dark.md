---
bug: B-6
feature: F-4
status: idea
board: 722
severity: medium
---
# Auth footer links are low contrast in the dark theme

## What happens
1. Switch to the dark theme and open `/sign-in`.
2. The "Terms of use" and "Privacy policy" links in the `AuthLayout` footer are primary blue (#2478C5) on the dark background: about 3.5:1, under WCAG 2.2 AA (4.5:1).

## Expected
The footer links meet AA in both themes, like the in-card links fixed in F-7 (underlined, text colour).

## Cause
<!-- Confirmed in code during refinement. -->

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
