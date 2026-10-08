---
page: sign-in-and-sign-out
locale: pt-PT
features: [F-5, B-3, F-7, F-11, F-53]
updated: 2026-10-06
---
# Iniciar e terminar sessão

Inicie sessão com o e-mail e a palavra-passe que criou, e termine sessão a partir do menu da conta na barra do aplicativo.

## Quem pode utilizar
Qualquer pessoa com uma conta no Simulab cujo e-mail esteja confirmado.

## Como fazer
### Iniciar sessão
1. Abra **Iniciar sessão**.
2. Indique o seu e-mail e a sua palavra-passe, depois selecione **Iniciar sessão**.
3. É encaminhado para a página inicial, e o ícone de conta no canto superior direito passa a mostrar a sua conta.

### Terminar sessão
1. Selecione o ícone de conta no canto superior direito.
2. Escolha **Terminar sessão**.
3. Volta para a página inicial, com a sessão terminada.

### Se se esquecer que já tem sessão iniciada
Abrir **Iniciar sessão** enquanto já tem sessão iniciada leva-o diretamente à página inicial; não volta a ver o formulário.

## Campos
| Campo | O que é | Regras |
|---|---|---|
| E-mail | O endereço com o qual se registou | Obrigatório |
| Palavra-passe | A palavra-passe da sua conta | Obrigatória |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| E-mail ou palavra-passe incorretos. | O e-mail ou a palavra-passe não correspondem a uma conta ativa | Verifique ambos e tente novamente; esta mensagem nunca indica qual dos dois está errado |
| Confirme o seu e-mail antes de iniciar sessão. | A sua conta ainda está pendente, desde o registo | Selecione **Reenviar o e-mail de verificação** na mesma página e depois verifique a sua caixa de correio |
| Demasiadas tentativas. Tente novamente dentro de {0}. | Cinco palavras-passe erradas seguidas bloquearam a conta durante 15 minutos | Aguarde o tempo indicado antes de tentar novamente |
| Demasiadas tentativas a partir desta rede. Tente novamente dentro de {0}. | 30 nomes de conta diferentes falharam ao entrar a partir da mesma rede em 15 minutos; ninguém nessa rede consegue entrar até o tempo indicado terminar | Aguarde o tempo indicado antes de tentar novamente, ou tente a partir de outra rede |
| A sua sessão terminou. Inicie sessão novamente. | A sua sessão foi terminada noutro sítio — por exemplo, terminou a sessão noutro separador — e esta página voltou ao início de sessão | Inicie sessão novamente |

Se iniciar sessão num dispositivo enquanto já tem sessão iniciada noutro, ambas as sessões continuam a funcionar: não há limite de quantos dispositivos podem ter sessão iniciada ao mesmo tempo.

## Durante quanto tempo a sessão se mantém
A sessão mantém-se durante até 30 dias em cada dispositivo, mesmo que deixe uma página aberta durante horas ou que a aplicação seja reiniciada; não precisa de iniciar sessão novamente nesse período. Quando a sua sessão é terminada noutro sítio, uma página aberta dá por isso no espaço de um minuto e leva-o para **Iniciar sessão** com a mensagem *A sua sessão terminou. Inicie sessão novamente.* Uma função ou permissão nova aparece no seu menu da próxima vez que abrir uma página, sem iniciar sessão novamente.

## Com a verificação em dois passos
Se ativou a verificação em dois passos, depois da palavra-passe a página pede o código de seis dígitos da sua aplicação de autenticação, ou um dos seus códigos de recuperação. Consulte [Verificação em dois passos](two-factor.md).

## Quando tem de escolher uma nova palavra-passe
A conta de administrador inicial de uma instalação nova é criada com uma palavra-passe que quem montou a instalação conhece. No primeiro início de sessão com ela, a página mostra **Escolha uma nova palavra-passe** em vez de o levar à aplicação (depois do código de dois passos, quando está ativo).
1. Introduza uma nova palavra-passe e repita-a para confirmar. Tem de ter pelo menos 12 caracteres, com letra maiúscula, número e símbolo, e ser diferente da atual.
2. Selecione **Guardar e iniciar sessão**. Inicia sessão com a nova palavra-passe, e qualquer outra sessão da conta é terminada.

Se a página disser *O início de sessão demorou demasiado. Introduza novamente a palavra-passe.*, passaram mais de cinco minutos: selecione **Cancelar** ou inicie sessão de novo desde o princípio. Selecione **Cancelar** em qualquer momento para voltar ao passo da palavra-passe; nada muda até guardar.

## Páginas relacionadas
- [Criar uma conta](create-account.md)
- [Como navegar](getting-around.md)
- [Palavra-passe](password.md): esqueci-me, redefinir e alterar
- [Verificação em dois passos](two-factor.md)
