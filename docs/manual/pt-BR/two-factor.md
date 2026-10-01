---
page: two-factor
locale: pt-BR
features: [F-11]
updated: 2026-09-21
---
# Verificação em duas etapas

A verificação em duas etapas pede, depois da senha, um código de seis dígitos de um aplicativo autenticador no seu celular. Quem descobrir sua senha continua sem conseguir entrar sem o seu celular.

## Quem pode usar
Qualquer pessoa conectada, na própria conta. É opcional. Se **Minha conta** não tiver o link **Segurança**, a verificação em duas etapas ainda não está disponível.

## Como fazer
### Ativar
1. Instale um aplicativo autenticador no celular, como Google Authenticator, Microsoft Authenticator ou 2FAS.
2. Abra **Minha conta** e selecione **Segurança**.
3. Selecione **Ativar a verificação em duas etapas**.
4. No aplicativo, adicione uma conta e escaneie o QR code. Se não conseguir escanear, digite no aplicativo a chave que aparece abaixo do código.
5. Digite o código de seis dígitos que o aplicativo mostra e selecione **Confirmar e ativar**.
6. Aparecem dez códigos de recuperação. Selecione **Baixar** ou **Copiar** e guarde-os num lugar seguro, longe do celular. Você não vai vê-los de novo.
7. Marque **Guardei meus códigos de recuperação** e selecione **Concluir**.

### Entrar com a verificação ativa
1. Abra **Entrar** e digite seu e-mail e sua senha, como sempre.
2. A página pede o código de seis dígitos. Digite o código que o aplicativo mostra agora e selecione **Entrar**.

### Entrar sem o celular
1. Depois da senha, selecione **Usar um código de recuperação**.
2. Digite um dos seus códigos de recuperação e selecione **Entrar**. Cada código funciona uma única vez; **Segurança** mostra quantos restam.

### Gerar novos códigos de recuperação
1. Em **Segurança**, selecione **Gerar novos códigos de recuperação**.
2. Digite um código do aplicativo (ou um código de recuperação) e selecione **Gerar novos códigos**.
3. Guarde os dez códigos novos. Os antigos param de funcionar na hora.

### Desativar
1. Em **Segurança**, selecione **Desativar a verificação em duas etapas** na seção vermelha.
2. Digite sua senha atual e um código do aplicativo (ou um código de recuperação).
3. Selecione **Desativar a verificação em duas etapas**. Só a senha volta a bastar para entrar; a chave do aplicativo e os códigos de recuperação deixam de funcionar.

Ativar ou desativar a verificação não desconecta você de nenhum lugar: vale a partir do próximo login.

## Campos
| Campo | O que é | Regras |
|---|---|---|
| Código de seis dígitos | O código que o aplicativo autenticador mostra | Obrigatório; muda a cada 30 segundos e cada código funciona uma vez |
| Código de recuperação | Um dos dez códigos que você guardou, como ABCDE-FGHJK | Obrigatório quando usado; maiúsculas ou minúsculas, com ou sem o traço; funciona uma vez |
| Senha atual | A senha com que você entra | Obrigatória para desativar a verificação |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| O código não está correto ou já foi usado. | O código está errado, expirou ou já foi aceito | Espere o próximo código no aplicativo e tente de novo |
| O código não foi aceito. Digite sua senha de novo para tentar outra vez. | No login, um código errado encerra a tentativa | Digite a senha de novo e depois um código novo |
| O login demorou demais. Digite sua senha de novo. | Passaram mais de 5 minutos entre a senha e o código | Comece de novo pela senha |
| Muitas tentativas. Tente de novo em {0}. | Cinco códigos ou senhas errados seguidos bloquearam a conta por 15 minutos | Espere o tempo indicado |
| Muitas tentativas a partir desta rede. Tente de novo em {0}. | 30 nomes de conta diferentes falharam ao entrar a partir da mesma rede em 15 minutos (um código errado também conta); você volta ao passo da senha | Espere o tempo indicado, ou tente de outra rede |
| A senha atual não está correta. | A senha digitada para desativar a verificação está errada | Digite de novo |

## Páginas relacionadas
- [Entrar e sair](sign-in-and-sign-out.md)
- [Minha conta](my-account.md)
- [Senha](password.md)
