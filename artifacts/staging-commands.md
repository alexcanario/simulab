# Staging no Azure: comandos (PowerShell 7)

Arquivo pessoal, fora do git (a pasta `artifacts/` é ignorada). Nenhum segredo está escrito aqui; eles são pedidos em campo oculto.
Rode na raiz do worktree `D:\wt\simulab\f-64-staging-on-azure`. As variáveis (`$env:...`, `$vault`) valem só no terminal onde foram definidas.

Dados fixos:
- Grupo de recursos: `rg-simulab-staging`. Região do ambiente: `centralus` por agora. `centralus` foi tentada e falhou com `AKSCapacityHeavyUsage` (sem capacidade para criar o ambiente dos containers em 07/10/2026); fica para tentar de novo depois.
- O nome do Key Vault, do servidor PostgreSQL e o endereço do `web` são escolhidos pelo Azure: leia-os depois do primeiro deploy, não os invente.
- O grupo atual se chama `rg-simulab-staging` (o antigo era `simulab-staging`, já apagado). Os nomes gerados mudam com o grupo: no grupo novo o vault saiu `keyvault-xgosyjwgusjvs` e o storage do web `storagewebxgosyjwgusjvs` (observado em 08/10/2026), em vez de `...tpxzgrkbfyobo`.
- A limpeza (Parte 0) usa o nome do vault apagado e a região em que ele foi criado. O purge do vault antigo (`keyvault-tpxzgrkbfyobo`, `centralus`) só serve para liberar aquele nome; como o grupo novo gera nomes diferentes, ele não bloqueia o deploy novo. Confirme a região no passo 0.3.

Regras enquanto um deploy roda:
- Não compile nem rode testes no mesmo worktree (o `Simulab.AppHost` do deploy é recompilado e o deploy falha com `Could not load file or assembly`).
- Se um deploy falhou, encerre-o (seção "Processos presos"): o processo fica vivo e trava o build.

---

## Parte 0. Recomeçar do zero (apaga o staging inteiro: banco, containers, vault, contas de teste)

O grupo de recursos é uma caixa: apagá-la joga fora tudo o que está dentro. O Key Vault vai para uma lixeira por 90 dias e o nome precisa ser liberado, porque o deploy novo gera o mesmo nome.

### 0.1 Apagar o grupo (volta na hora; a exclusão leva alguns minutos)
```powershell
az group delete -n rg-simulab-staging --yes --no-wait
```

### 0.2 Esperar terminar (repita até devolver `false`)
```powershell
az group exists -n rg-simulab-staging
```

### 0.3 Esvaziar a lixeira do vault (só depois do `false`)
```powershell
az keyvault list-deleted --query "[].{name:name, location:properties.location}" -o table
az keyvault purge --name keyvault-tpxzgrkbfyobo --location centralus
```
A região do `purge` é a do vault apagado, que a primeira linha mostra na coluna `location` (esperado `centralus`: foi onde o deploy falhou). Use essa região, mesmo que o ambiente novo vá para `centralus`. Se o `purge` for recusado, o vault tem proteção contra purge; cole a mensagem.

---

## Parte 1. Primeira publicação (o grupo e o vault ainda não existem)

Siga na ordem. Use um terminal NOVO e fique nele até o fim da parte (as variáveis ficam nele).

### 1.1 Variáveis do deploy
```powershell
$env:AZURE__SUBSCRIPTIONID = (az account show --query id -o tsv)
$env:AZURE__LOCATION = "centralus"
$env:AZURE__RESOURCEGROUP = "rg-simulab-staging"
```

### 1.2 Segredo do OpenIddict (gerar e digitar)
O valor tem que ser o MESMO em todo deploy: o Api grava o hash dele no banco na primeira partida e nunca o atualiza. Nunca use o texto de um exemplo: gere um valor de verdade e guarde-o num gerenciador de senhas.
```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```
Depois, digite esse valor no campo oculto:
```powershell
${env:Parameters__openiddict-client-secret} = (ConvertFrom-SecureString (Read-Host -AsSecureString "OpenIddict client secret") -AsPlainText)
```
Se este terminal for velho, limpe antes um segredo antigo: `${env:Parameters__openiddict-client-secret} = $null`.

### 1.2b Login e senha do PostgreSQL (gerar uma vez e guardar)
O servidor nunca troca de login, e o `--clear-cache` gerava um login novo a cada deploy (erro `28P01`). Agora os dois são parâmetros fixos (D20): o mesmo valor em todo deploy. Aqui eles são só gerados e entregues ao deploy; o passo 1.7 os guarda no vault. O login começa com letra e não pode ser `admin`, `postgres` etc.
```powershell
$pgUser = "u" + (-join ((97..122) | Get-Random -Count 9 | ForEach-Object { [char]$_ }))
$chars = [char[]]([char]'a'..[char]'z' + [char]'A'..[char]'Z' + [char]'0'..[char]'9')
$pgPassword = -join (1..40 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
${env:Parameters__postgres-admin-user} = $pgUser
${env:Parameters__postgres-admin-password} = $pgPassword
$redisPassword = -join (1..32 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
${env:Parameters__redis-password} = $redisPassword
```
A senha do Redis (D21) também é fixa: gerada, ela mudava a cada deploy e o Redis em execução ficava com a antiga (`NOAUTH` nos logs). Só letras e números, para não quebrar a string de conexão. Não imprima `$pgPassword` nem `$redisPassword`. Fique neste terminal até o passo 1.7.

### 1.3 Criar o grupo de recursos
```powershell
az group create -n rg-simulab-staging -l centralus
az group show -n rg-simulab-staging --query "properties.provisioningState" -o tsv
```

### 1.4 Primeiro deploy (leva uns 7 minutos)
```powershell
aspire --version
aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
```
Termina com o `api` reiniciando em loop (faltam certificado e senha do admin); é esperado até a etapa 1.10. Se o deploy falhar, cole o final da saída.

### 1.5 Ler os nomes que o Azure criou
```powershell
az resource list -g rg-simulab-staging -o table
$vault = (az keyvault list -g rg-simulab-staging --query "[0].name" -o tsv)
$vault
```
O último comando imprime o nome do vault. Se vier vazio, o deploy não criou o vault: veja
```powershell
az deployment group list -g rg-simulab-staging --query "[].{name:name, state:properties.provisioningState, error:properties.error.code}" -o table
```

### 1.6 Seu acesso ao Key Vault (espere 1 a 2 min se der Forbidden depois)
```powershell
az role assignment create --role "Key Vault Administrator" --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" --assignee-principal-type User --scope "$(az keyvault show -n $vault -g rg-simulab-staging --query id -o tsv)"
```

### 1.7 Guardar o segredo do deploy no vault
Digite o MESMO valor da etapa 1.2, no campo oculto. Os próximos deploys o leem de lá.
```powershell
$s = Read-Host -AsSecureString "OpenIddict client secret"
az keyvault secret set --vault-name $vault -n deploy--OpenIddictClientSecret --value (ConvertFrom-SecureString $s -AsPlainText) -o none
```
Guarde também o login e a senha do PostgreSQL da etapa 1.2b (o mesmo terminal):
```powershell
az keyvault secret set --vault-name $vault -n deploy--PostgresAdminUser --value $pgUser -o none
az keyvault secret set --vault-name $vault -n deploy--PostgresAdminPassword --value $pgPassword -o none
az keyvault secret set --vault-name $vault -n deploy--RedisPassword --value $redisPassword -o none
```

### 1.8 Chave do Data Protection (NUNCA recriar nem rodar de novo)
Confira primeiro; só crie se o primeiro comando não imprimir uma URL.
```powershell
az keyvault key show --vault-name $vault -n dataprotection --query "key.kid" -o tsv
```
```powershell
az keyvault key create --vault-name $vault -n dataprotection --kty RSA --size 2048 --ops wrapKey unwrapKey
```

### 1.9 Certificados do OpenIddict (só crie se não existirem)
```powershell
az keyvault certificate list --vault-name $vault --query "[].name" -o tsv
```
Se `OpenIddict--SigningCertificate` e `OpenIddict--EncryptionCertificate` não aparecerem:
```powershell
az keyvault certificate get-default-policy > policy.json
az keyvault certificate create --vault-name $vault -n OpenIddict--SigningCertificate -p "@policy.json"
az keyvault certificate create --vault-name $vault -n OpenIddict--EncryptionCertificate -p "@policy.json"
Remove-Item policy.json
```

### 1.10 Senha do admin@simulab.local
O texto entre aspas do `Read-Host` é só o rótulo. A senha você digita depois, no campo oculto (12+ caracteres, maiúscula, dígito, símbolo).
```powershell
$p = Read-Host -AsSecureString "Admin password"
az keyvault secret set --vault-name $vault -n Identity--SeedAdmin--Password --value (ConvertFrom-SecureString $p -AsPlainText) -o none
az keyvault secret show --vault-name $vault -n Identity--SeedAdmin--Password --query id -o tsv
```
O último comando só confirma que o segredo existe (imprime a URL dele, não o valor).

### 1.11 Endereço do ingress (proxies confiáveis)
Descubra o endereço do `web`, abra-o no navegador uma vez e leia o log:
```powershell
az containerapp show -n web -g rg-simulab-staging --query properties.configuration.ingress.fqdn -o tsv
az containerapp logs show -n web -g rg-simulab-staging --type console --tail 100
```
O aviso `A forwarded header arrived from ENDERECO and is ignored` só aparece quando NENHUM proxy está listado (código: `Program.cs`, ramo `else` de `behindTrustedProxy`). Como o `appsettings.Staging.json` já lista `100.100.0.17` (medido no ambiente antigo), o aviso NÃO aparece mais: não achar a linha é o esperado. O que vale agora é o comportamento: depois do segundo deploy (1.12), criar uma conta e entrar no app. Se funcionar, o endereço está certo. Se o login falhar com "Algo deu errado", o endereço que o ingress usa neste ambiente é outro; aí me avise, e esvaziamos a lista (ou acrescentamos um aviso para endereço não listado) para medir o novo.

### 1.12 Segundo deploy
Repita o comando da etapa 1.4, neste mesmo terminal (a variável do segredo continua definida).
```powershell
aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
```

### 1.13 Conferir
```powershell
az containerapp logs show -n api -g rg-simulab-staging --type console --tail 50
```
Esperado: o `api` sobe sem `no pg_hba.conf entry`, e o endereço do `web` (etapa 1.11) com `/api/v1/system/info` no fim responde 200 com `"environment": "Staging"` (não verifiquei se o Web repassa esse caminho ao Api). O endereço que o `api` vê só aparece depois que ele sobe; se o login do `web` falhar, filtre o log do `api` (seção "Diagnóstico") e me cole o endereço: pode ser preciso um terceiro deploy.

---

## Parte 2. Republicação (o grupo e o vault já existem)

Para repetir o deploy depois de uma mudança de código, ou depois de abrir um terminal novo.

### 2.1 Variáveis do deploy
```powershell
$env:AZURE__SUBSCRIPTIONID = (az account show --query id -o tsv)
$env:AZURE__LOCATION = "centralus"
$env:AZURE__RESOURCEGROUP = "rg-simulab-staging"
```

### 2.2 Nome do vault e os três parâmetros do deploy (lidos do vault)
```powershell
$vault = (az keyvault list -g rg-simulab-staging --query "[0].name" -o tsv)
$vault
${env:Parameters__openiddict-client-secret} = (az keyvault secret show --vault-name $vault -n deploy--OpenIddictClientSecret --query value -o tsv)
${env:Parameters__postgres-admin-user} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminUser --query value -o tsv)
${env:Parameters__postgres-admin-password} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminPassword --query value -o tsv)
${env:Parameters__redis-password} = (az keyvault secret show --vault-name $vault -n deploy--RedisPassword --query value -o tsv)
```
Confira que os quatro não estão vazios (só o tamanho, nunca o valor): `${env:Parameters__postgres-admin-password}.Length`. O login e a senha do PostgreSQL têm que ser os que o servidor já tem; se o vault não os tem, ou se o `api` loga `28P01`, vá para "PostgreSQL desalinhado" na Resolução de problemas.
Se o `$vault` vier vazio, ele não existe: você está na Parte 1. O valor lido tem que ser o mesmo da primeira publicação; se o segredo não está no vault, recupere-o do `api` (imprime o segredo) e guarde-o pela etapa 1.7:
```powershell
az containerapp secret show -n api -g rg-simulab-staging --secret-name authentication--openiddict--clientsecret --query value -o tsv
```

### 2.3 Deploy
```powershell
aspire --version
aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
```
Não compile nem rode testes neste worktree enquanto ele roda.

### 2.4 Conferir
```powershell
az containerapp logs show -n api -g rg-simulab-staging --type console --tail 50
az containerapp show -n web -g rg-simulab-staging --query properties.configuration.ingress.fqdn -o tsv
```
O segundo comando devolve o endereço do `web` (coloque `https://` na frente).

O login e a senha do PostgreSQL agora vêm dos parâmetros do passo 2.2 (D20), então o `--clear-cache` não os troca mais. Se o `api` logar `28P01 password authentication failed`, veja "PostgreSQL desalinhado" na Resolução de problemas. Para achar o endereço do ingress do próprio Api (ainda não medido), leia o aviso depois do deploy:
```powershell
az containerapp logs show -n api -g rg-simulab-staging --type console --tail 300 | Select-String "forwarded header"
```

---

## Parte 3. Primeira republicação depois do D21 (Redis com senha fixa) e do log do 401

Use quando o ambiente já existe e o vault ainda NÃO tem `deploy--RedisPassword`. Terminal novo, na raiz do worktree. Nas republicações seguintes basta a Parte 2 (o passo 2.2 já lê as quatro variáveis do vault).

### 3.1 Variáveis do ambiente e do deploy
```powershell
Set-Location D:\wt\simulab\f-64-staging-on-azure
$rg = "rg-simulab-staging"
$vault = (az keyvault list -g $rg --query "[0].name" -o tsv)
$vault
$env:AZURE__SUBSCRIPTIONID = (az account show --query id -o tsv)
$env:AZURE__LOCATION = (az group show -n $rg --query location -o tsv)
$env:AZURE__RESOURCEGROUP = $rg
```

### 3.2 Criar a senha do Redis e guardá-la no vault (só esta vez)
```powershell
$chars = [char[]]([char]'a'..[char]'z' + [char]'A'..[char]'Z' + [char]'0'..[char]'9')
$redisPassword = -join (1..32 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
az keyvault secret set --vault-name $vault -n deploy--RedisPassword --value $redisPassword -o none
```

### 3.3 Carregar os quatro parâmetros do deploy a partir do vault
```powershell
${env:Parameters__openiddict-client-secret} = (az keyvault secret show --vault-name $vault -n deploy--OpenIddictClientSecret --query value -o tsv)
${env:Parameters__postgres-admin-user} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminUser --query value -o tsv)
${env:Parameters__postgres-admin-password} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminPassword --query value -o tsv)
${env:Parameters__redis-password} = (az keyvault secret show --vault-name $vault -n deploy--RedisPassword --query value -o tsv)
```

### 3.4 Conferir os tamanhos (nunca os valores)
Esperado: OpenIddict mais de 0; usuário do PostgreSQL 10; senha do PostgreSQL 40; senha do Redis 32. Se algum for 0, não rode o deploy.
```powershell
${env:Parameters__openiddict-client-secret}.Length
${env:Parameters__postgres-admin-user}.Length
${env:Parameters__postgres-admin-password}.Length
${env:Parameters__redis-password}.Length
```

### 3.5 Deploy (não compile nem rode testes neste worktree enquanto ele roda)
```powershell
aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
```

### 3.6 Reiniciar o Redis e conferir (depois do `Pipeline succeeded`)
A plataforma atualiza o segredo mas não reinicia o Redis em execução; sem este passo voltam os `NOAUTH`.
```powershell
$rev = az containerapp revision list -n redis -g $rg --query "[?properties.active].name" -o tsv
az containerapp revision restart -n redis -g $rg --revision $rev
Start-Sleep 45
az containerapp logs show -n api -g $rg --type console --tail 40 | Select-String "NOAUTH|AuthenticationFailure|28P01|Unhandled"
```
Vazio é o esperado.

### 3.7 Tentar entrar uma vez e ler o motivo do 401
```powershell
az containerapp logs show -n web -g $rg --type console --tail 100 | Select-String "refused this host's own client credentials"
```

---

## Parte 4. Republicação depois da correção do `client_id` do Web (causa do 401)

Causa confirmada em 08/10/2026: o log do Web dizia `error invalid_client, The mandatory 'client_id' parameter is missing`. O Web na nuvem não recebia `Authentication__OpenIddict__ClientId` (só existia no `appsettings.Development.json`). O `AzureDeployment.cs` agora entrega `simulab-web` ao Web. Não precisa apagar o cliente do banco nem mexer no segredo. O vault já tem os quatro parâmetros (Parte 3 já rodada), então basta a Parte 2.

Terminal novo, na raiz do worktree. Precisa ter o commit da correção no worktree (eu faço antes de você rodar).

### 4.1 Variáveis, parâmetros e deploy
```powershell
Set-Location D:\wt\simulab\f-64-staging-on-azure
$rg = "rg-simulab-staging"
$vault = (az keyvault list -g $rg --query "[0].name" -o tsv)
$env:AZURE__SUBSCRIPTIONID = (az account show --query id -o tsv)
$env:AZURE__LOCATION = (az group show -n $rg --query location -o tsv)
$env:AZURE__RESOURCEGROUP = $rg
${env:Parameters__openiddict-client-secret} = (az keyvault secret show --vault-name $vault -n deploy--OpenIddictClientSecret --query value -o tsv)
${env:Parameters__postgres-admin-user} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminUser --query value -o tsv)
${env:Parameters__postgres-admin-password} = (az keyvault secret show --vault-name $vault -n deploy--PostgresAdminPassword --query value -o tsv)
${env:Parameters__redis-password} = (az keyvault secret show --vault-name $vault -n deploy--RedisPassword --query value -o tsv)
${env:Parameters__openiddict-client-secret}.Length
${env:Parameters__postgres-admin-user}.Length
${env:Parameters__postgres-admin-password}.Length
${env:Parameters__redis-password}.Length
```
Esperado: OpenIddict mais de 0; usuário do PostgreSQL 10; senha do PostgreSQL 40; senha do Redis 32. Se algum for 0, não rode o deploy.
```powershell
aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo
```
Não compile nem rode testes neste worktree enquanto ele roda.

### 4.2 Reiniciar o Redis (depois do `Pipeline succeeded`)
```powershell
$rev = az containerapp revision list -n redis -g $rg --query "[?properties.active].name" -o tsv
az containerapp revision restart -n redis -g $rg --revision $rev
```

### 4.3 Conferir que o Web recebeu o id (só o nome, não é segredo)
```powershell
az containerapp show -n web -g $rg --query "properties.template.containers[0].env[?name=='Authentication__OpenIddict__ClientId'].value" -o tsv
```
Esperado: `simulab-web`.

### 4.4 Entrar uma vez (admin@simulab.local) e, se falhar, ler o motivo
```powershell
az containerapp logs show -n web -g $rg --type console --tail 100 | Select-String "refused this host's own client credentials"
```
Se entrar, siga o roteiro de validação. Se o 401 continuar, cole a linha nova (o texto do erro mudou se a causa mudou).

### 4.5 Fechar o que ficou aberto
```powershell
$server = (az postgres flexible-server list -g $rg --query "[0].name" -o tsv)
az postgres flexible-server firewall-rule list -g $rg --name $server -o table
```
Se aparecer `meu-ip`: `az postgres flexible-server firewall-rule delete -g $rg --name $server --rule-name meu-ip --yes`.

---

## Parte 5. Deploy pelo GitHub Actions (F-65, workflow `deploy.yml`)

A partir da F-65 o staging pode ser publicado sem o seu terminal: o GitHub entra no Azure por OIDC (nenhuma senha do Azure fica guardada) e roda o mesmo `aspire deploy` da Parte 2. O primeiro deploy por esse caminho passou em 09/10/2026 (execução 37951156063, 4 min 23 s, `main` no commit `3df5bac`). O que o workflow usa (já criado, não repita):
- Azure: registro de aplicativo `simulab-deploy` (`e6d8ec88-f4fb-4e21-b1b6-0170348843c7`) com credenciais federadas `github-staging` e `github-production`, e os papéis Contributor e Role Based Access Control Administrator só no grupo `rg-simulab-staging`.
- GitHub: ambientes `staging` (aceita `main` e tags `v*`) e `production` (só tags `v*`, você como aprovador). No `staging`: variáveis `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_LOCATION`, `AZURE_RESOURCE_GROUP`, `POSTGRES_ADMIN_USER`, `CHECK_URL` (a raiz do Web) e segredos `OPENIDDICT_CLIENT_SECRET`, `POSTGRES_ADMIN_PASSWORD`, `REDIS_PASSWORD`.

### 5.1 Publicar o `main` no staging e acompanhar
```powershell
gh workflow run deploy.yml --repo alexcanario/simulab --ref main -f environment=staging
$run = gh run list --repo alexcanario/simulab --workflow deploy.yml --limit 1 --json databaseId -q '.[0].databaseId'
gh run watch $run --repo alexcanario/simulab --exit-status --interval 30
```
Esperado: todos os passos com `✓` (o de produção aparece como `-`, pulado) e, no fim, `✓ main deploy`. O passo "Wait for the check URL" espera até 5 minutos pelo HTTP 200 da raiz do Web. O resumo da execução (aba Summary no GitHub) mostra ambiente, ref e commit. Não rode build nem testes no seu worktree por causa disso: o deploy roda no GitHub, não na sua máquina.

### 5.2 Publicar uma tag no staging ou na produção
```powershell
gh workflow run deploy.yml --repo alexcanario/simulab --ref v0.22.0 -f environment=staging
```
Esperado: o mesmo que 5.1. Para `production`, troque o ambiente: a execução fica em "Waiting" até você aprovar no GitHub (Actions → a execução → Review deployments); se rejeitar, ela termina sem nunca entrar no Azure. Produção só aceita tag `v*` (um `--ref main` é recusado). Uma tag criada antes da F-65 não tem o `deploy.yml` e não pode ser publicada assim.

### 5.3 Conferir os segredos e as variáveis do ambiente (só nomes)
```powershell
gh secret list --env staging --repo alexcanario/simulab
gh variable list --env staging --repo alexcanario/simulab
```
Esperado: três segredos e sete variáveis. Para trocar um segredo (o valor tem de ser o mesmo que o vault guarda, ou o servidor desalinha, ver "PostgreSQL desalinhado"):
```powershell
$vault = (az keyvault list -g rg-simulab-staging --query "[0].name" -o tsv)
gh secret set REDIS_PASSWORD --env staging --repo alexcanario/simulab --body (az keyvault secret show --vault-name $vault -n deploy--RedisPassword --query value -o tsv)
```

### 5.4 Se o login do Azure falhar com `AADSTS700213`
Foi o que aconteceu na primeira tentativa. O GitHub desta conta apresenta o repositório com ids numéricos (`repo:alexcanario@3664703/simulab@1397573907:environment:staging`), e a credencial federada tinha o nome em texto. O comando de configuração de `docs/infra.md` agora pergunta o prefixo ao GitHub (B-25); para corrigir uma credencial já criada, use o mesmo prefixo (o texto exato também aparece na anotação da execução que falhou, `gh run view <id> --repo alexcanario/simulab`):
```powershell
$appId = "e6d8ec88-f4fb-4e21-b1b6-0170348843c7"
$prefix = gh api repos/alexcanario/simulab/actions/oidc/customization/sub --jq .sub_claim_prefix
@{ name = "github-staging"; issuer = "https://token.actions.githubusercontent.com"; subject = "${prefix}:environment:staging"; audiences = @("api://AzureADTokenExchange") } | ConvertTo-Json | Set-Content fc-staging.json
az ad app federated-credential update --id $appId --federated-credential-id github-staging --parameters "@fc-staging.json"
Remove-Item fc-staging.json
az ad app federated-credential list --id $appId --query "[].{name:name,subject:subject}" -o table
```
Esperado: o subject da lista igual ao da mensagem de erro. Repita a execução (5.1). A credencial `github-production` já foi ajustada do mesmo jeito.

---

## Resolução de problemas

Cada linha vem de algo que aconteceu de verdade nesta sessão. "Não verificado" quer dizer que a causa é provável, mas não foi confirmada.

### Antes e durante o deploy
| Sintoma | Causa | O que fazer |
|---|---|---|
| `aspire deploy` não imprime nada | Pasta errada, ou variável faltando neste terminal | Rode na raiz do worktree; confira `$env:AZURE__SUBSCRIPTIONID` e as outras |
| `The GUID for subscription is invalid` / `An Azure subscription id is required` | O id ficou com o texto de um exemplo (`<id da assinatura>`) ou a variável não existe neste terminal | Passo 1.1: `$env:AZURE__SUBSCRIPTIONID = (az account show --query id -o tsv)` |
| `Failed to resolve 'nome_do_vault.vault.azure.net'` | Texto de exemplo (`NOME_DO_VAULT`) colado no lugar do nome real | Leia o nome com o passo 1.5 e use `$vault` |
| `--vault-name: expected one argument` | `$vault` está vazio (o vault ainda não existe, ou terminal novo) | Passo 1.5: `$vault = (az keyvault list -g rg-simulab-staging --query "[0].name" -o tsv)` |
| `Could not load file or assembly ...` no deploy | Um build ou teste rodou no mesmo worktree durante o deploy (provável; não provado) | Não compile durante o deploy; repita o deploy |
| Gate ou build vermelho: "locked by a running process" | Um deploy falhado deixou `aspire` e `Simulab.AppHost` vivos | Seção "Processos presos"; encerre e rode de novo |
| `InvalidTemplate ... parameters were supplied ... 'location'` | O `aspire deploy` envia `location` a todo template | Já corrigido no `email.bicep` (commit `49e6fca`); se voltar, é outro template |
| `AKSCapacityHeavyUsage` em `provision-cae` | Sem capacidade para criar o ambiente dos containers na região (visto em `centralus`) | Troque de região (`centralus` funcionou); apague o grupo e refaça |
| `MaxNumberOfRegionalEnvironmentsInSubExceeded` | A assinatura só admite 1 ambiente de Container Apps por região; um em `ScheduledForDelete` ainda conta | Espere a exclusão terminar (`az containerapp env list`) |
| `Forbidden` ao usar o vault | O papel do passo 1.6 ainda não propagou | Espere 1 a 2 minutos e repita |
| `unrecognized arguments: -n ...` ou `the following arguments are required: --server-name/-s` em `firewall-rule` | Neste subcomando o servidor é `--server-name` e `--name`/`-n` é o nome da regra (confirmado em 08/10/2026) | `firewall-rule create -g $rg --server-name $server --name meu-ip --start-ip-address ... --end-ip-address ...` |
| Workflow `deploy`: `AADSTS700213: No matching federated identity record found for presented assertion subject` | O subject da credencial federada não é o que o GitHub apresenta (nesta conta ele traz ids numéricos) | Parte 5.4: copie o subject da mensagem para a credencial |
| Rótulo do `Read-Host` com a senha escrita | A senha ficou no histórico do terminal | O texto entre aspas é só o rótulo; digite a senha no campo oculto |

### O app no ar não funciona (login "Algo deu errado", páginas com erro)
Primeiro veja se o `api` está de pé e leia o log logo depois de tentar entrar:
```powershell
az containerapp replica list -n api -g rg-simulab-staging -o table
az containerapp logs show -n api -g rg-simulab-staging --type console --tail 300 | Select-String "fail|Exception|ID2083|pg_hba|28P01|forwarded|Unhandled|error"
az containerapp logs show -n web -g rg-simulab-staging --type console --tail 300 | Select-String "fail|Exception|invalid_client|401|forwarded|Unhandled"
```
O que cada palavra indica:
| Aparece no log | Significa | O que fazer |
|---|---|---|
| `no pg_hba.conf entry ... no encryption` | O servidor exige SSL e a conexão veio sem criptografia | Corrigido no código (commit `830b595`: `SSL Mode=Require` na nuvem); repita o deploy |
| `28P01 password authentication failed for user "X"` | O login ou a senha que o Api usa (vault) não é a que o servidor tem. Confirmado: o `--clear-cache` gerava login novo a cada deploy | Seção "PostgreSQL desalinhado" abaixo |
| `ID2083` | O Api não acredita no ingress (endereço fora de `KnownProxies`) | Ver "Endereço do ingress" abaixo |
| `invalid_client` | O segredo do OpenIddict enviado difere do hash que o Api gravou no banco | Use o mesmo valor do primeiro deploy; ver "Segredo" abaixo |
| `Unhandled exception` sem as anteriores | Outra causa | Cole as linhas anteriores ao erro |

Linhas do log que parecem erro, mas são normais:
- `401 Unauthorized` em `keyvault-....vault.azure.net/keys/dataprotection`: é o handshake do SDK; logo depois vem o pedido de token (`localhost:12356/msi/token`) e a repetição que dá certo.
- `BlobNotFound` ao ler `simulab-web.xml`: é o primeiro início (ainda não existe o anel de chaves), seguido de um `PUT`.
- `ConditionNotMet` (304) ao reler o blob: leitura condicional, normal.
- `Failed to determine the https port for redirect`: aviso do Web atrás do ingress; não impediu nada até aqui (não verificado se tem relação com o login).

### Endereço do ingress
O aviso `A forwarded header arrived from ... and is ignored` só aparece quando NENHUM proxy está listado. Com `KnownProxies` preenchido em `appsettings.Staging.json` (hoje `100.100.0.17`, medido no ambiente antigo), ele não aparece: não achar a linha é o esperado. Se o login falhar com `ID2083` ou sem outra causa, o endereço do ingress neste ambiente pode ser outro; peça para esvaziar a lista (ou acrescentar um aviso para endereço não listado) e refaça o deploy para medir.

### PostgreSQL desalinhado (`28P01`): alinhar o servidor e o vault
O login do servidor não muda nunca: o nome em `password authentication failed for user "X"` é o que o Api usa agora; compare com o do servidor. A senha o servidor aceita trocar. O paliativo abaixo gera uma senha nova, aplica no servidor e grava tudo no vault, para os dois ficarem iguais (usado em 08/10/2026). Terminal novo, em duas etapas.

Etapa A (variáveis, senha nova e troca no servidor):
```powershell
$rg = "rg-simulab-staging"
$server = (az postgres flexible-server list -g $rg --query "[0].name" -o tsv)
$vault = (az keyvault list -g $rg --query "[0].name" -o tsv)
$login = (az postgres flexible-server show -g $rg --name $server --query administratorLogin -o tsv)
$fqdn = "$server.postgres.database.azure.com"
$chars = [char[]]([char]'a'..[char]'z' + [char]'A'..[char]'Z' + [char]'0'..[char]'9')
$pw = -join (1..40 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
az postgres flexible-server update -g $rg --name $server --admin-password $pw --query "{estado:state, login:administratorLogin}" -o table
```
O `update` tem que mostrar `Ready` e nenhum `ERROR` antes da etapa B. (Em `flexible-server update` o `--name` funcionou; o `-n` não foi testado.)

Etapa B (vault, reinício do Api e log), no mesmo terminal:
```powershell
az keyvault secret set --vault-name $vault --name deploy--PostgresAdminUser --value $login --query name -o tsv
az keyvault secret set --vault-name $vault --name deploy--PostgresAdminPassword --value $pw --query name -o tsv
az keyvault secret set --vault-name $vault --name connectionstrings--postgres --value "Host=$fqdn;Username=$login;Password=$pw" --query name -o tsv
az keyvault secret set --vault-name $vault --name connectionstrings--simulab --value "Host=$fqdn;Database=simulab;Username=$login;Password=$pw" --query name -o tsv
$rev = az containerapp revision list -n api -g $rg --query "[?properties.active].name" -o tsv
az containerapp revision restart -n api -g $rg --revision $rev
Start-Sleep 60
az containerapp logs show -n api -g $rg --type console --tail 40 | Select-String "28P01|password authentication|Unhandled|Now listening"
```
Esperado: `Now listening` e nenhum `28P01`. O Api lê o vault só na partida: sem o reinício ele fica com a senha antiga. Se o log vier vazio, espere 30 segundos e rode só o último comando.

### Redis com `NOAUTH Authentication required` (Api e Web cheios de `AuthenticationFailure`)
Causa confirmada em 08/10/2026: o `--clear-cache` gerou uma senha nova de Redis; a plataforma atualizou o segredo, mas o Redis em execução ficou com a antiga (o log dele mostrava a partida horas antes). Reiniciar o Redis resolve; depois do D21 (senha fixa em `deploy--RedisPassword`) não deve voltar.
```powershell
$rev = az containerapp revision list -n redis -g rg-simulab-staging --query "[?properties.active].name" -o tsv
az containerapp revision restart -n redis -g rg-simulab-staging --revision $rev
```
Na primeira republicação depois do D21, grave a senha no vault ANTES do deploy (o ambiente atual já tem Redis; defina uma senha, grave-a e use-a no deploy):
```powershell
$chars = [char[]]([char]'a'..[char]'z' + [char]'A'..[char]'Z' + [char]'0'..[char]'9')
$redisPassword = -join (1..32 | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
az keyvault secret set --vault-name $vault -n deploy--RedisPassword --value $redisPassword -o none
${env:Parameters__redis-password} = $redisPassword
```

### Login falha com "Algo deu errado" e o Web recebe 401 em `/connect/token`
Visto em 08/10/2026, depois de Api, Redis e e-mail já funcionarem (cadastro e confirmação de e-mail passam; só o login falha).

**Primeiro leia o motivo no log do Web** (`az containerapp logs show -n web -g rg-simulab-staging --type console --tail 100 | Select-String "client credentials"`). Em 08/10/2026 ele dizia `invalid_client, The mandatory 'client_id' parameter is missing`: o Web não recebia o `client_id`. Corrigido no `AzureDeployment.cs` (Parte 4); a hipótese do segredo velho no banco NÃO era a causa. Só use o conserto abaixo se o motivo for outro (por exemplo, cliente ou segredo inválido).

Diagnóstico (não imprime valores; `True` e `True` indicam que o deploy entrega o mesmo valor, e o hash do banco é que está velho):
```powershell
$sa = az containerapp secret show -n api -g rg-simulab-staging --secret-name authentication--openiddict--clientsecret --query value -o tsv
$vs = az keyvault secret show --vault-name $vault -n deploy--OpenIddictClientSecret --query value -o tsv
"Api e vault iguais: " + ($sa -ceq $vs)
az containerapp secret list -n web -g rg-simulab-staging --query "[].name" -o tsv
```
Conserto (só com a causa confirmada; apaga as sessões de login e o registro do cliente do staging, não os usuários). Rode o SQL no DataGrip, NUNCA no PowerShell (o terminal leria `select` como `Select-Object`). Antes, libere seu IP no firewall (seção "Banco de dados").
```sql
delete from identity.openiddict_tokens
 where application_id in (select id from identity.openiddict_applications where client_id = 'simulab-web');
delete from identity.openiddict_authorizations
 where application_id in (select id from identity.openiddict_applications where client_id = 'simulab-web');
delete from identity.openiddict_applications where client_id = 'simulab-web';
```
Depois, no PowerShell: reinicie o Api (ele recria o cliente com o segredo atual) e remova a regra de firewall.
```powershell
$rev = az containerapp revision list -n api -g rg-simulab-staging --query "[?properties.active].name" -o tsv
az containerapp revision restart -n api -g rg-simulab-staging --revision $rev
az postgres flexible-server firewall-rule delete -g rg-simulab-staging --server-name $server --name meu-ip --yes
```

## Casos de uso (receitas prontas, cada uma com o seu quando usar)

### Caso de uso 1. Refazer o cliente de login do Web (OpenIddict)
**Quando usar:** o login falha com "Algo deu errado" e o Web recebe `401` em `/connect/token`, com o segredo igual no Api, no Web e no vault (diagnóstico acima). O cliente `simulab-web` foi criado por um deploy com outro segredo e o Api nunca o atualiza.

**O que faz:** apaga o registro do cliente e as sessões de login ligadas a ele. O Api recria o cliente na próxima partida, com o segredo atual. **Não apaga usuários**: contas, senhas e confirmações de e-mail continuam como estão.

**Passo 1: liberar seu IP e conectar no DataGrip** (seção "Banco de dados" e "Conectar pelo DataGrip").

**Passo 2: no DataGrip (nunca no PowerShell), nesta ordem:**
```sql
delete from identity.openiddict_tokens
 where application_id in (select id from identity.openiddict_applications where client_id = 'simulab-web');
delete from identity.openiddict_authorizations
 where application_id in (select id from identity.openiddict_applications where client_id = 'simulab-web');
delete from identity.openiddict_applications where client_id = 'simulab-web';
```
Dê **Commit** se o DataGrip estiver em modo manual. Confira: a consulta abaixo deve voltar zero linhas.
```sql
select client_id from identity.openiddict_applications;
```

**Passo 3: no PowerShell, reiniciar o Api e fechar o firewall:**
```powershell
$rg = "rg-simulab-staging"
$server = (az postgres flexible-server list -g $rg --query "[0].name" -o tsv)
$rev = az containerapp revision list -n api -g $rg --query "[?properties.active].name" -o tsv
az containerapp revision restart -n api -g $rg --revision $rev
az postgres flexible-server firewall-rule delete -g $rg --server-name $server --name meu-ip --yes
```

**Passo 4: conferir que o cliente voltou (DataGrip):**
```sql
select id, client_id, client_type from identity.openiddict_applications;
```
Espere uns 30 segundos pelo reinício e tente entrar. Se continuar falhando, leia o log do Web e do Api logo após a tentativa:
```powershell
az containerapp logs show -n web -g $rg --type console --tail 80 | Select-String "connect/token|401|400|invalid|Unhandled|Exception"
az containerapp logs show -n api -g $rg --type console --tail 400 | Select-String "connect/token|OpenIddict|ID[0-9]{4}|invalid_|Unauthorized|Request finished|forwarded header|NOAUTH"
```

#### Se o 401 continuar depois do Caso 1: ligar o log detalhado do OpenIddict no Api (temporário)
O Api não loga nada de `/connect/token` no nível padrão (o filtro de `Microsoft.AspNetCore` e do OpenIddict esconde as linhas). Estas variáveis criam uma revisão nova do Api e mostram o motivo da recusa (`invalid_client`, `ID...`). O próximo `aspire deploy` volta ao normal.
```powershell
az containerapp update -n api -g $rg --set-env-vars "Logging__LogLevel__OpenIddict=Debug" "Logging__LogLevel__Microsoft.AspNetCore.Hosting.Diagnostics=Information"
Start-Sleep 60
```
Tente entrar uma vez e leia (sem filtro de SQL):
```powershell
az containerapp logs show -n api -g $rg --type console --tail 200 | Select-String "OpenIddict|connect/token|ID[0-9]{4}|invalid_|Request starting|Request finished"
```
Não deixe o nível `Debug` ligado: ele registra mais do que o necessário. Para tirar sem esperar o próximo deploy:
```powershell
az containerapp update -n api -g $rg --remove-env-vars "Logging__LogLevel__OpenIddict" "Logging__LogLevel__Microsoft.AspNetCore.Hosting.Diagnostics"
```

#### Depois do commit "log the OAuth error" (próximo deploy): onde ler o motivo do 401
O Web passa a registrar um aviso quando `/connect/token` responde 401, com o código de erro do OAuth (nunca o segredo):
```powershell
az containerapp logs show -n web -g $rg --type console --tail 100 | Select-String "refused this host's own client credentials"
```
Antes desse deploy, o log do Api no nível detalhado não mostrou explicação (as linhas do OpenIddict não aparecem); o log do Web sozinho mostra só o status 401.

### Caso de uso 2. Recriar o admin@simulab.local (não verificado)
**Quando usar:** a senha do admin no vault (`Identity--SeedAdmin--Password`) não é a que o banco guarda. O seed só cria o admin se ele não existir; um admin existente mantém a senha dele.

**Como seria:** apagar a conta `admin@simulab.local` da tabela `identity.users` e reiniciar o Api (o seed a recria com a senha do vault, marcada para trocar no primeiro login). Não escrevi o SQL porque não verifiquei as chaves estrangeiras das tabelas ligadas (`user_roles`, `user_claims`, `user_logins`, `user_tokens`, `account_events`); apagar a linha sem elas pode falhar ou deixar sobras. Peça-me o SQL antes de usar.

Primeiro confirme se a senha digitada é a do vault (só imprime `True` ou `False`; digite no campo oculto):
```powershell
$vault = (az keyvault list -g rg-simulab-staging --query "[0].name" -o tsv)
$try = Read-Host -AsSecureString "Senha que voce digita no login"
(ConvertFrom-SecureString $try -AsPlainText) -ceq (az keyvault secret show --vault-name $vault -n Identity--SeedAdmin--Password --query value -o tsv)
```

### Segredo do OpenIddict
O Api cria o cliente do Web na primeira partida e nunca atualiza o segredo dele. Trocar o valor depois do primeiro deploy quebra o login (`invalid_client`). Se o valor se perdeu e o `api` ainda está no ar, leia do container app (imprime o segredo):
```powershell
az containerapp secret show -n api -g rg-simulab-staging --secret-name authentication--openiddict--clientsecret --query value -o tsv
```
Nunca use o texto de um exemplo como segredo (já aconteceu: o primeiro deploy usou `SEU_VALOR_GERADO`).

---

## Outros comandos

### Login do usuário admin
- Login: `admin@simulab.local`. Senha: a da etapa 1.10. No primeiro login o app pede uma senha nova.
- Reler a senha gravada (imprime o valor na tela; rode só no seu terminal):
```powershell
az keyvault secret show --vault-name $vault -n Identity--SeedAdmin--Password --query value -o tsv
```

### Onde cada comando roda
- `az ...`, `aspire ...`, `$variaveis`: PowerShell. 
- SQL (`select`, `delete`, `update`): somente no DataGrip. Colado no PowerShell dá `Select-Object: A positional parameter cannot be found...`, e nada é executado.
- Terminal novo não tem `$rg`, `$vault`, `$server`; defina-os de novo (erro típico: `--resource-group/-g: expected one argument`):
```powershell
$rg = "rg-simulab-staging"
$vault = (az keyvault list -g $rg --query "[0].name" -o tsv)
$server = (az postgres flexible-server list -g $rg --query "[0].name" -o tsv)
```

### Banco de dados (PostgreSQL)
Conexão pronta (`Host`, `Username`, `Password`, `Database=simulab`); imprime o valor, rode só no seu terminal:
```powershell
az keyvault secret show --vault-name $vault -n connectionstrings--simulab --query value -o tsv
```
Use porta 5432, SSL ligado (`require`), database `simulab`. Nome do servidor:
```powershell
$server = (az postgres flexible-server list -g rg-simulab-staging --query "[0].name" -o tsv)
$server
```
O firewall só tem `AllowAllAzureIps`; para entrar do seu computador, libere seu IP durante o uso:
```powershell
$meuIp = (Invoke-RestMethod https://api.ipify.org)
az postgres flexible-server firewall-rule create -g rg-simulab-staging --server-name $server --name meu-ip --start-ip-address $meuIp --end-ip-address $meuIp
```
Neste subcomando o servidor é `--server-name` e `--name` é o nome da regra. Remova a regra ao terminar:
```powershell
az postgres flexible-server firewall-rule delete -g rg-simulab-staging --server-name $server --name meu-ip --yes
```
#### Conectar pelo DataGrip
Depois de liberar seu IP (acima), pegue os dados da conexão. A senha vai para a área de transferência e não aparece na tela:
```powershell
$rg = "rg-simulab-staging"
$vault = (az keyvault list -g $rg --query "[0].name" -o tsv)
$cs = az keyvault secret show --vault-name $vault -n connectionstrings--simulab --query value -o tsv
$campos = @{}; $cs -split ';' | ForEach-Object { $p = $_ -split '=', 2; if ($p.Count -eq 2) { $campos[$p[0]] = $p[1] } }
"Host:     " + $campos['Host']
"Usuario:  " + $campos['Username']
"Database: " + $campos['Database']
Set-Clipboard $campos['Password']
"Senha copiada para a area de transferencia"
```
No DataGrip: `+` → Data Source → PostgreSQL; Host (porta `5432`), User, Password (Ctrl+V), Database `simulab`; aba SSH/SSL: **Use SSL** com modo `require`; **Test Connection**; em Schemas marque `identity`. Ao terminar, copie qualquer outro texto para limpar a área de transferência e remova a regra de firewall.

Consulta só de leitura (DataGrip), para ver se o cliente do Web existe (a tabela não tem coluna de data de criação; colunas reais: `id`, `client_id`, `client_type`, `client_secret` (hash), `permissions`, etc.):
```sql
select id, client_id, client_type from identity.openiddict_applications;
```

Estado do servidor e SSL obrigatório (retorna `on`):
```powershell
az postgres flexible-server list -g rg-simulab-staging --query "[].{name:name,state:state,publicAccess:network.publicNetworkAccess}" -o table
az postgres flexible-server parameter show -g rg-simulab-staging --server-name $server -n require_secure_transport --query value -o tsv
```

### Diagnóstico
Falha de login ("Algo deu errado"): filtre o log do `api`.
```powershell
az containerapp logs show -n api -g rg-simulab-staging --type console --tail 300 | Select-String "forwarded header|ID2083|Exception|fail"
```
Assinatura ativa:
```powershell
az account show --query "{name:name, id:id}" -o table
```

### Ambientes de Container Apps e a cota (1 por região nesta assinatura)
A assinatura só pode ter 1 ambiente de Container Apps por região (`MaxNumberOfRegionalEnvironmentsInSubExceeded`). Veja os que existem em todos os grupos; um ambiente em `ScheduledForDelete` ainda conta até a exclusão terminar.
```powershell
az containerapp env list --query "[].{name:name, group:resourceGroup, location:location, state:properties.provisioningState}" -o table
```

### Sonda de capacidade em outra região (ex.: testar `centralus` mais tarde)
O Azure só diz se há capacidade na hora de criar, e não há comando que consulte isso antes. A sonda cria um ambiente de teste num grupo à parte e o apaga. Só vale se NÃO houver outro ambiente seu na mesma região (veja a seção acima: o `rg-simulab-staging` dessa região tem que ter sumido de vez). Se a sonda criar, há capacidade; se falhar com `AKSCapacityHeavyUsage`, não há. O custo exato não verifiquei (um ambiente sem containers, mais um workspace de logs).
```powershell
az group create -n simulab-probe -l centralus
az containerapp env create -n probe-env -g simulab-probe -l centralus
```
Limpar a sonda, tendo dado certo ou errado:
```powershell
az group delete -n simulab-probe --yes --no-wait
az group exists -n simulab-probe
```
A saída em `ScheduledForDelete` ou `false` indica que a limpeza está em curso ou terminou. Depois de a sonda ter funcionado, o deploy em `centralus` é a Parte 1 trocando `centralus` por `centralus` nos passos 1.1 e 1.3 (e em 2.1 na republicação).

### Versão do `az` e das extensões (conferido em 07/10/2026: `az` 2.91.0 e `azure-devops` 1.0.8, ambos já na última versão)
```powershell
az version --query "{cli:\"azure-cli\"}" -o json
az extension list --query "[].{name:name, version:version}" -o table
az extension list-available --query "[?name=='azure-devops'].{name:name, latest:version}" -o table
```
O deploy não usa a extensão `azure-devops`.

### Processos presos (um deploy que falhou deixa o `aspire` vivo e trava o build)
```powershell
Get-Process aspire, Simulab.AppHost -ErrorAction SilentlyContinue
Stop-Process -Id ID_DO_ASPIRE, ID_DO_APPHOST
```
Troque `ID_DO_ASPIRE` e `ID_DO_APPHOST` pelos números que o primeiro comando mostrar.

### Parar e iniciar o staging (declarado no `docs/infra.md`; ainda não rodamos)
Pare fora das janelas de teste; o banco guarda os dados. Confira o `docs/infra.md`, seção "Staging start and stop", antes de usar. Precisa de `$server` (acima).
```powershell
az containerapp update -n api -g rg-simulab-staging --min-replicas 0
az containerapp update -n web -g rg-simulab-staging --min-replicas 0
az containerapp update -n redis -g rg-simulab-staging --min-replicas 0
az postgres flexible-server stop -g rg-simulab-staging -n $server
```
Iniciar:
```powershell
az postgres flexible-server start -g rg-simulab-staging -n $server
az containerapp update -n redis -g rg-simulab-staging --min-replicas 1
az containerapp update -n api -g rg-simulab-staging --min-replicas 1
az containerapp update -n web -g rg-simulab-staging --min-replicas 0 --max-replicas 1
```
