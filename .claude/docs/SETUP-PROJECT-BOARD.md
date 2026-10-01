# Setup GitHub Projects Board

Guia para configurar o GitHub Projects do Simulab com as colunas do workflow.

## Workflow Status

O padrão de status esperado é:

```
idea → refining → approved → building → validating → done
```

Essas devem ser as colunas (valores do campo Status) na board.

## Passos para configurar

### 1. Usar o Script Python (Multiplataforma - Recomendado)

#### Pré-requisitos:
- Python 3.7+
- GitHub CLI instalado e autenticado: `gh auth login`
- `curl` e `jq` instalados

#### Criar nova board:

```bash
python .claude/scripts/configure_github_project_board.py
```

Cria uma board chamada "Simulab" com as colunas do workflow.

```bash
python .claude/scripts/configure_github_project_board.py -n "Minha Board"
python .claude/scripts/configure_github_project_board.py --name "Minha Board"
```

Cria uma board com nome customizado.

#### Configurar board existente:

```bash
python .claude/scripts/configure_github_project_board.py -i 3
python .claude/scripts/configure_github_project_board.py --id 3
```

Configura o projeto #3 com as colunas do workflow.

#### Exemplos completos:

```bash
# Criar "Simulab" (padrão)
python .claude/scripts/configure_github_project_board.py

# Criar com nome customizado
python .claude/scripts/configure_github_project_board.py -n "Sprint 1"

# Alterar board existente
python .claude/scripts/configure_github_project_board.py -i 3

# Desabilitar cores (para CI/CD)
python .claude/scripts/configure_github_project_board.py --no-color
```

### 2. Usar o Script PowerShell (Windows)

Se preferir PowerShell no Windows:

#### Pré-requisitos:
- PowerShell 5.1+ (Windows) ou PowerShell 7.0+ (Linux/Mac)
- GitHub CLI instalado e autenticado: `gh auth login`
- `curl` e `jq` instalados

#### Executar (resolver erro de política de execução):

**Opção 1: Desbloquear arquivo (recomendado)**
```powershell
Unblock-File -Path ".\scripts\Configure-GitHubProjectBoard.ps1"
.\scripts\Configure-GitHubProjectBoard.ps1
```

**Opção 2: Bypass de execução única**
```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\Configure-GitHubProjectBoard.ps1"
```

#### Exemplos:

```powershell
# Criar "Simulab" (padrão)
.\scripts\Configure-GitHubProjectBoard.ps1

# Criar com nome customizado
.\scripts\Configure-GitHubProjectBoard.ps1 -BoardName "Sprint 1"

# Alterar board existente
.\scripts\Configure-GitHubProjectBoard.ps1 -BoardId 3
```

### 3. Script Bash (Alternativa)

Se preferir bash/Linux:

```bash
./scripts/create-project.sh
```

### 4. Configurar manualmente via interface web

1. Acesse: https://github.com/alexcanario/simulab/projects/new
2. **Nome**: `Simulab`
3. **Template**: `Table` (ou `Board` se preferir kanban)
4. Clique em **Create project**
5. Clique na engrenagem ⚙️ no canto superior direito → **Settings**
6. Vá em **Custom fields** → **Status**
7. Adicione os valores (na ordem):
   - ☐ `idea`
   - ☐ `refining`
   - ☐ `approved`
   - ☐ `building`
   - ☐ `validating`
   - ☐ `done`
8. Clique em **Save**

### 3. Associar issues ao projeto

1. No repositório, vá em **Projects** → selecione **Simulab**
2. Configure para adicionar automaticamente:
   - Pull requests
   - Issues do repositório

### 4. Usar a board

Ao criar issues/features:
- Use labels seguindo o padrão: `F-<n>` para features, `B-<n>` para bugs
- Defina o Status correspondente ao status do item (ex: `idea`, `approved`, etc.)
- O board espelhará automaticamente o status do arquivo `.md`

## Status mapping

| Status | Significado |
|--------|------------|
| `idea` | Ideiaem discussão, não refinada |
| `refining` | Sendo refinada com o product owner |
| `approved` | Aprovada, pronta para construir |
| `building` | Sendo desenvolvida |
| `validating` | Pronta, aguardando validação em screen |
| `done` | Completa, merged para main |

## Referência

- GitHub Projects docs: https://docs.github.com/en/issues/planning-and-tracking-with-projects
- Workflow: `.claude/rules/agile/workflow.md`
- Decisões: `docs/decisions/ADR-0001-foundation.md`
