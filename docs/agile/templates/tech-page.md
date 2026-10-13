---
title: <Page title, as a reader looks it up>
summary: <One line: what this page tells the reader>
items: [F-<number>]
---
<!--
One technical page for the developers. Save as: docs/<folder>/<slug>.md, the folder of its kind (default folders: flows, how-to, reference, architecture; the `- Docs folders:` line of CLAUDE.md renames them).
The folder is the kind; `items` lists the ids that created or changed the page (add yours when you update it). The index file of the folder (docs/<folder>.md) is written by `scripts/techdocs.js index`, never by hand.
Keep only the skeleton of this page's kind and remove the others and these comments. Written in English. Never the words that start a DocGen file's notice (DocGen deletes such a file).
-->
# <Page title>

Technical terms: [glossary](../glossary.md)

Manual page for the user: [<title>](../manual/<locale>/<page>.md) <!-- optional: one line, only when the feature has a manual page -->

<!-- FLOW: a behavior of the app, from the trigger to the end. -->
## Trigger
<!-- What starts it: a user action, a message, a schedule. -->

## Steps
<!-- Numbered, one line each; a Mermaid `flowchart` or `sequenceDiagram` block when the path has branches. -->
1. <Step>

## Rules
- <Rule that decides a branch or a refusal>

## Wireframe
<!-- Only when the feature adds or changes a screen: the `### Wireframe` blocks of the item file, copied whole (a fenced `text` block per screen, 60 columns at most). A complex screen adds the line below; a simple one leaves it out. -->
```text
+------------------------------+
| <Screen title>               |
| [ Field ____________ ] [new] |
| [ Save ]                     |
+------------------------------+
```
Mockup: <relative path of docs/features/mockups/<id>-<slug>.html>

## Errors
- `<area>.<error>` — <when and what the caller sees>

<!-- HOW-TO: a way of working the team runs. -->
## When to use
<!-- The situation, and when not to. -->

## Steps
1. <Command or action> → <what you see>

## Check
<!-- How to know it worked. -->

<!-- REFERENCE: a contract or a setting. -->
## Contract
<!-- The route, message, option or configuration key, and where it lives. -->

## Fields
| Name | Type | Required | Meaning |
|---|---|---|---|
| <name> | <type> | yes/no | <meaning> |

<!-- ARCHITECTURE: a module or an external dependency. -->
## Purpose
<!-- Why the module or dependency exists, in two lines. -->

## Parts
<!-- The projects or components and what each owns. -->

## Dependencies
<!-- What it calls, what calls it, and the contract between them. -->
