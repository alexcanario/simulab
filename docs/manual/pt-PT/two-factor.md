---
page: two-factor
locale: pt-PT
features: [F-11]
updated: 2026-09-21
---
# Verificação em dois passos

A verificação em dois passos pede, depois da palavra-passe, um código de seis dígitos de uma aplicação de autenticação no seu telemóvel. Quem descobrir a sua palavra-passe continua a não conseguir entrar sem o seu telemóvel.

## Quem pode usar
Qualquer pessoa com sessão iniciada, na própria conta. É opcional. Se **A minha conta** não tiver a ligação **Segurança**, a verificação em dois passos ainda não está disponível.

## Como fazer
### Ativar
1. Instale uma aplicação de autenticação no telemóvel, como o Google Authenticator, o Microsoft Authenticator ou o 2FAS.
2. Abra **A minha conta** e selecione **Segurança**.
3. Selecione **Ativar a verificação em dois passos**.
4. Na aplicação, adicione uma conta e leia o código QR. Se não o conseguir ler, introduza na aplicação a chave que aparece por baixo do código.
5. Introduza o código de seis dígitos que a aplicação mostra e selecione **Confirmar e ativar**.
6. Aparecem dez códigos de recuperação. Selecione **Transferir** ou **Copiar** e guarde-os num local seguro, longe do telemóvel. Não os voltará a ver.
7. Assinale **Guardei os meus códigos de recuperação** e selecione **Concluir**.

### Iniciar sessão com a verificação ativa
1. Abra **Iniciar sessão** e introduza o e-mail e a palavra-passe, como sempre.
2. A página pede o código de seis dígitos. Introduza o código que a aplicação mostra nesse momento e selecione **Iniciar sessão**.

### Iniciar sessão sem o telemóvel
1. Depois da palavra-passe, selecione **Usar um código de recuperação**.
2. Introduza um dos seus códigos de recuperação e selecione **Iniciar sessão**. Cada código só funciona uma vez; **Segurança** mostra quantos restam.

### Gerar novos códigos de recuperação
1. Em **Segurança**, selecione **Gerar novos códigos de recuperação**.
2. Introduza um código da aplicação (ou um código de recuperação) e selecione **Gerar novos códigos**.
3. Guarde os dez códigos novos. Os antigos deixam de funcionar de imediato.

### Desativar
1. Em **Segurança**, selecione **Desativar a verificação em dois passos** na secção vermelha.
2. Introduza a palavra-passe atual e um código da aplicação (ou um código de recuperação).
3. Selecione **Desativar a verificação em dois passos**. Volta a bastar a palavra-passe para iniciar sessão; a chave da aplicação e os códigos de recuperação deixam de funcionar.

Ativar ou desativar a verificação não termina nenhuma sessão: aplica-se a partir do próximo início de sessão.

## Campos
| Campo | O que é | Regras |
|---|---|---|
| Código de seis dígitos | O código que a aplicação de autenticação mostra | Obrigatório; muda a cada 30 segundos e cada código funciona uma vez |
| Código de recuperação | Um dos dez códigos que guardou, como ABCDE-FGHJK | Obrigatório quando usado; maiúsculas ou minúsculas, com ou sem o hífen; funciona uma vez |
| Palavra-passe atual | A palavra-passe com que inicia sessão | Obrigatória para desativar a verificação |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| O código não está correto ou já foi usado. | O código está errado, expirou ou já foi aceite | Aguarde o próximo código na aplicação e tente novamente |
| O código não foi aceite. Introduza novamente a palavra-passe para tentar outra vez. | No início de sessão, um código errado termina a tentativa | Introduza a palavra-passe outra vez e depois um código novo |
| O início de sessão demorou demasiado. Introduza novamente a palavra-passe. | Passaram mais de 5 minutos entre a palavra-passe e o código | Recomece pela palavra-passe |
| Demasiadas tentativas. Tente novamente dentro de {0}. | Cinco códigos ou palavras-passe errados seguidos bloquearam a conta durante 15 minutos | Aguarde o tempo indicado |
| Demasiadas tentativas a partir desta rede. Tente novamente dentro de {0}. | 30 nomes de conta diferentes falharam ao entrar a partir da mesma rede em 15 minutos (um código errado também conta); volta ao passo da palavra-passe | Aguarde o tempo indicado, ou tente a partir de outra rede |
| A palavra-passe atual não está correta. | A palavra-passe introduzida para desativar a verificação está errada | Introduza-a novamente |

## Páginas relacionadas
- [Iniciar e terminar sessão](sign-in-and-sign-out.md)
- [A minha conta](my-account.md)
- [Palavra-passe](password.md)
