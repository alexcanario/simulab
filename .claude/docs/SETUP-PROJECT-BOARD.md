# Setup GitHub Projects Board

Guia para configurar o GitHub Projects do Simulab com as colunas do workflow.

## Workflow Status

O padrão de status esperado é:

```
idea → refining → approved → building → validating → done
```

Essas devem ser as colunas (valores do campo Status) na board.

## Passos para configurar

### 1. Criar o Projeto (se ainda não existir)

1. Acesse: https://github.com/alexcanario/simulab/projects/new
2. **Nome**: `Simulab`
3. **Template**: `Table` (ou `Board` se preferir kanban)
4. Clique em **Create project**

### 2. Configurar as colunas

#### Via interface web (recomendado):

1. Acesse o projeto criado: https://github.com/alexcanario/simulab/projects/[numero]
2. Clique na engrenagem ⚙️ no canto superior direito → **Settings**
3. Vá em **Custom fields** → **Status**
4. Adicione ou edite os seguintes valores (na ordem):
   - ☐ `idea`
   - ☐ `refining`
   - ☐ `approved`
   - ☐ `building`
   - ☐ `validating`
   - ☐ `done`
5. Clique em **Save**

#### Via script (futuro):

```bash
./scripts/setup-github-project.sh <numero-do-projeto>
```

Exemplo: `./scripts/setup-github-project.sh 1`

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
