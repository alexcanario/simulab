---
feature: F-190
epic: Data type controls
status: validating
board: 260
version: 1
---
# One http(s) address check for the URL fields

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: validating (2026-10-29)
- Stopped at: Built; gate and scan GREEN; waiting for the owner's validation.
- Next action: Owner follows the validation script (3 steps).
- Waiting on: nothing

## Summary
The rule "only `http` and `https`, with a host" now lives in four places: `AppUrlField.IsValid` (the kit) and the private checks of `ExamEditionForm.razor` (around line 708), `IssuingAuthorityDialog.razor` (around line 108) and `OrganizerDialog.razor` (around line 147). Have the three pages call `AppUrlField.IsValid` and keep their own module error codes, so the rule has one copy. The review estimated about 9 lines less. Seen in F-183 review #7; the pages and the control behave the same today, so this is a cleanup with no visible change.

## Start
- Depends on: nothing (F-183 is done).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane assignment of the lanes run; the change is small).
- Risk: medium (no signal; code or rules changed (.claude/agile/sync.json))
- Suggested path: `/agile:autopilot F-190` (small and clear).
- Parallel with: F-189.

## Goal
The rule for a valid web address has one copy, in the kit control, so a change to it (for example a new scheme) is made once and the pages cannot drift from the control.

## Users and use cases
- UC1 An administrator types a website or notice address on the exam edition form, the issuing authority dialog or the organizer dialog and sees the same result as today: a valid address is kept, any other is refused with the module's own message.

## Business rules
- BR1 A web address is valid when it is absolute, its scheme is `http` or `https` and it has a host. The only copy of this rule is `AppUrlField.IsValid`.
- BR2 The three pages call `AppUrlField.IsValid` and keep their own module error codes (`ExamEditionNoticeUrlInvalid`, `IssuingAuthorityWebsiteInvalid`, `OrganizerWebsiteInvalid`) and the exam edition length check (`ExamEditionNoticeUrlMaxLength`).
- BR3 No visible change: for every address the pages accepted or refused before, they accept or refuse it now, with the same message. The Api and the Domain checks (`ExamEdition`, `IssuingAuthority`, `Organizer`) are not touched; the Domain cannot reach the Web project.

## Screens and API
- No new route, endpoint, screen or error code. Three private methods lose their copy of the scheme test: `NoticeUrlError` (`ExamEditionForm.razor`), `WebsiteError` (`IssuingAuthorityDialog.razor`, `OrganizerDialog.razor`).

## Risks
- A page accepts or refuses an address differently after the change — `IsValid` adds only "host not empty", and an absolute `http`/`https` address that `Uri` parses always has a host — proven by the page tests of AC1 to AC3 — low impact — two-way door.

## Docs
- none — internal cleanup with no visible change; `docs/flows/text-kind-controls.md` already describes the rule of `AppUrlField`.

## Acceptance criteria
- AC1 Given the exam edition form, when the notice address is `ftp://x.gov.br`, `mailto:a@b.c` or too long, then the form shows `ExamEditionNoticeUrlInvalid` as before, and a valid `https://x.gov.br/edital` or an empty address shows no error.
- AC2 Given the issuing authority dialog, when the website is `ftp://x.gov.br`, then the dialog shows `IssuingAuthorityWebsiteInvalid` as before, and a valid or empty website shows no error.
- AC3 Given the organizer dialog, when the website is `ftp://x.gov.br`, then the dialog shows `OrganizerWebsiteInvalid` as before, and a valid or empty website shows no error.
- AC4 Given the Catalog pages, when their sources are scanned, then none of them tests `Uri.UriSchemeHttp` or `Uri.UriSchemeHttps` itself; only `AppUrlField.razor` does.
- AC5 No new text is added, so the locale files do not change and the missing-key test stays green.

## Criterion → test

Tests locked: a573b4f0274fbc8c71d9c17e18019879caa81caa (1 file); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |

## Decisions
- 2026-10-29 — Scope is the three pages only; the Domain and Api checks stay — the Domain cannot reference the Web project and a shared copy there is another item — assumed: scope (CLAUDE.md, modular-monolith profile)
- 2026-10-29 — The exam edition form keeps its length check before calling `IsValid` — it is a separate rule (`ExamEditionNoticeUrlMaxLength`) — assumed: scope
- 2026-10-29 — AC4 is a source-scan test in `Simulab.Web.Tests`, so the single copy cannot come back — the cheapest way to hold "one copy" — assumed: tests
- 2026-10-29: plan — 1 `WebAddressRuleTests` red on the three pages → run it; 2 the three pages call `AppUrlField.IsValid` → the new test and the Catalog page tests green; 3 gate, coverage, validation script → `agile gate GREEN`
- 2026-10-29: Reuse: `AppUrlField.IsValid` (Components/Ui/AppUrlField.razor) reused by the three pages
- 2026-10-29: Shortcuts: none
- 2026-10-29: model — item asked for opus, the lane session ran on Sonnet 5.5 (could not switch itself)
- 2026-10-29 — No question reached the owner: no package, permission, schema, money or screen change — assumed by autopilot (`--assume`) as lane nanda of the lanes run

## Out of scope
- The Api and Domain copies of the address check.
- Any new scheme or a stricter address rule.

## Open questions
- (none)

## Change notes

## Events

- 2026-10-29 assumed — 4 answers

## Validation script

### pt-BR
Necessário para validar: o app host local com o administrador da carga inicial `admin@simulab.local` (senha em `src/Hosts/Simulab.Api/appsettings.Development.json`, `Identity:SeedAdmin:Password`) — você fornece; já existe na carga inicial.

#### Verificado pelo Claude
- AC4: o teste `WebAddressRuleTests.Pages_DoNotTestTheHttpSchemeThemselves` falhou antes da mudança (apontava `ExamEditionForm.razor`) e passa agora; as 312 provas de arquitetura estão verdes (em 934a41d3).
- AC1 a AC3: os testes existentes das três telas continuam verdes (Web.Tests, 1234 nos filtros de Catalog, campos de texto e recursos) e AC5: nenhum texto novo (em 934a41d3).
- O gate completo: `agile gate GREEN`, nenhum aviso novo, baseline com 0 entradas; Web 2946, Identity 589, Catalog 706, QuestionBank 872, ExamEngine 99 testes verdes (em 934a41d3).
- O scan de segurança: `agile scan GREEN` (16 falsos positivos, 13 corrigidos, 1 fechado pelo Claude) (em 934a41d3).

#### Só você
1. Passo 1 de 3 (cerca de 4 min): suba o app no banco próprio do item. Feche antes o Visual Studio ou qualquer app host rodando desta pasta.
Git Bash:
```bash
cd /d/wt/simulab/f-190-one-http-address && Database__Name=simulab_f190 dotnet run --project src/Hosts/Simulab.AppHost
```
PowerShell 7:
```powershell
cd D:\wt\simulab\f-190-one-http-address; $env:Database__Name = 'simulab_f190'; dotnet run --project src/Hosts/Simulab.AppHost
```
Esperado: o endereço do painel do Aspire aparece e o Web responde em https://localhost:7125; entre em /sign-in como `admin@simulab.local`.

2. Passo 2 de 3 (cerca de 5 min): confira o site no organizador e na autoridade emissora.
   - [ ] 2.1 No menu lateral abra Conteúdo > Organizadores (/admin/organizers), clique em "Adicionar", preencha o nome e a sigla e digite `ftp://banca.gov.br` no campo do site; clique em Salvar.
   Esperado: o campo mostra a mensagem de endereço inválido e nada é salvo.
   - [ ] 2.2 Troque o site para `banca.gov.br` e salve.
   Esperado: salva e o site fica `https://banca.gov.br`.
   - [ ] 2.3 Repita o 2.1 e o 2.2 em Conteúdo > Autoridades emissoras (/admin/issuing-authorities).
   Esperado: o mesmo resultado, com a mensagem da própria tela.

3. Passo 3 de 3 (cerca de 4 min): confira o endereço do edital na edição de um exame.
   - [ ] 3.1 Em Conteúdo > Exames abra um exame, na seção "Edições" abra uma edição e, no cartão "Edital e inscrição", digite `mailto:edital@banca.gov.br` no campo do endereço do edital e saia do campo.
   Esperado: o campo mostra a mensagem de endereço inválido e o texto digitado fica.
   - [ ] 3.2 Troque por `https://banca.gov.br/edital` e saia do campo.
   Esperado: a mensagem some e aparece o ícone de abrir o endereço em nova aba.
