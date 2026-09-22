---
page: my-account
locale: pt-PT
features: [F-8, F-10, F-11, F-16]
updated: 2026-09-22
---
# A minha conta

Veja o seu e-mail, altere o nome que aparece no menu da conta e escolha o idioma em que o Simulab abre e escreve os seus e-mails.

## Quem pode utilizar
Qualquer pessoa com sessão iniciada. Cada um vê e altera apenas a sua própria conta.

## Como fazer
### Abrir A minha conta
1. Selecione o ícone da conta no canto superior direito e depois **A minha conta**.

### Alterar o nome de apresentação ou o idioma preferido
1. Em **Nome de apresentação**, escreva o nome que pretende ver no menu da conta, ou deixe em branco para usar apenas o e-mail.
2. Em **Idioma preferido**, escolha Português (Brasil), Português (Portugal) ou English.
3. Selecione **Guardar**. A página é recarregada no idioma escolhido, com "O seu perfil foi guardado.", e o menu da conta mostra o novo nome.

**Cancelar** desfaz o que alterou e repõe os dados guardados. Se tentar sair da página com alterações por guardar, o Simulab pergunta se as pretende descartar.

### Como funciona o idioma preferido
- Sempre que inicia sessão, o Simulab abre no seu idioma preferido, em qualquer dispositivo, mesmo que o navegador esteja noutro idioma.
- Mudar o idioma pelo globo da barra superior, com sessão iniciada, também muda o seu idioma preferido.
- Os seus e-mails (redefinição da palavra-passe, aviso de palavra-passe alterada) são escritos no idioma preferido.
- Um dispositivo que já tinha sessão iniciada passa a usar o novo idioma no próximo início de sessão.

### Alterar a palavra-passe
Selecione **Alterar palavra-passe**, logo abaixo dos campos. Consulte [Palavra-passe](password.md).

### Proteger a conta com um código
Selecione **Segurança**, ao lado de **Alterar palavra-passe**, para ativar a verificação em dois passos. A ligação só aparece onde a verificação em dois passos está disponível. Consulte [Verificação em dois passos](two-factor.md).

### Transferir os seus dados
**Os seus dados**, mesmo acima de **Eliminar a minha conta**, dá-lhe uma cópia de tudo o que o Simulab guarda sobre si, num ficheiro JSON.
1. Selecione **Transferir os meus dados**.
2. Escreva a sua palavra-passe atual e selecione **Transferir**. A palavra-passe protege o ficheiro: contém dados pessoais.
3. O navegador guarda `simulab-my-data-<data>.json`, e uma mensagem avisa que a transferência começou.

O ficheiro traz a sua conta (endereço de correio eletrónico, nome de apresentação, telefone, idioma preferido, datas), os perfis que tem, os termos e a política de privacidade que aceitou com o endereço IP a partir do qual aceitou, as alterações feitas aos seus perfis e quantos dispositivos têm sessão iniciada. Nunca traz a sua palavra-passe nem qualquer código de segurança. Nada fica guardado no servidor, e pode transferir as vezes que quiser.

Cada transferência envia-lhe um e-mail a avisar. Se esse e-mail chegar e não tiver sido o próprio, alguém conhece a sua palavra-passe: altere-a de imediato.

### Eliminar a sua conta
No fim da página, **Eliminar a minha conta** remove os seus dados pessoais definitivamente. Consulte [Eliminar a sua conta](erase-account.md).

## Campos
| Campo | Significado | Regras |
|---|---|---|
| E-mail | O endereço com que inicia sessão | Só de leitura; não pode ser alterado aqui |
| Nome de apresentação | O nome que aparece no menu da conta | Opcional; no máximo 120 caracteres |
| Idioma preferido | O idioma em que a aplicação abre quando inicia sessão e em que os seus e-mails são escritos | Obrigatório; um dos três idiomas da lista |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| O seu perfil foi guardado. | O nome e o idioma foram gravados | Nada |
| Utilize no máximo 120 caracteres. | O nome de apresentação é demasiado longo; **Guardar** fica desativado | Encurte o nome |
| Escolha um dos idiomas da lista. | O idioma enviado não é um dos três disponíveis | Escolha um idioma da lista e guarde novamente |
| Não foi possível carregar o seu perfil. | O Simulab não conseguiu ler os seus dados neste momento | Selecione **Tentar novamente** |
| A transferência começou. Está a caminho um e-mail de confirmação. | O ficheiro foi montado e entregue ao navegador | Nada; procure o ficheiro nas suas transferências |
| A palavra-passe atual não está correta. | A palavra-passe escrita na janela de transferência está errada | Escreva novamente; demasiadas tentativas bloqueiam a conta durante algum tempo |

## Páginas relacionadas
- [Palavra-passe](password.md)
- [Verificação em dois passos](two-factor.md)
- [Eliminar a sua conta](erase-account.md)
- [Como navegar](getting-around.md)
