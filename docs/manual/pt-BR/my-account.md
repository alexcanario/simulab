---
page: my-account
locale: pt-BR
features: [F-8, F-10, F-11, F-16]
updated: 2026-09-22
---
# Minha conta

Veja seu e-mail, mude o nome que aparece no menu da conta e escolha o idioma em que o Simulab abre e escreve seus e-mails.

## Quem pode usar
Qualquer pessoa conectada. Cada um vê e muda só a própria conta.

## Como fazer
### Abrir Minha conta
1. Clique no ícone da conta no canto superior direito e depois em **Minha conta**.

### Mudar o nome de exibição ou o idioma preferido
1. Em **Nome de exibição**, digite o nome que você quer ver no menu da conta, ou deixe em branco para usar só o e-mail.
2. Em **Idioma preferido**, escolha Português (Brasil), Português (Portugal) ou English.
3. Clique em **Salvar**. A página recarrega já no idioma escolhido, com "Seu perfil foi salvo.", e o menu da conta mostra o novo nome.

**Cancelar** desfaz o que você mudou e volta aos dados salvos. Se tentar sair da página com mudanças não salvas, o Simulab pergunta se você quer descartá-las.

### Como o idioma preferido funciona
- Toda vez que você entra, o Simulab abre no seu idioma preferido, em qualquer dispositivo, mesmo que o navegador esteja em outro idioma.
- Trocar o idioma pelo globo da barra superior, conectado, também muda o seu idioma preferido.
- Seus e-mails (redefinição de senha, aviso de senha alterada) são escritos no idioma preferido.
- Um dispositivo que já estava conectado passa a usar o novo idioma no próximo login.

### Alterar a senha
Clique em **Alterar senha**, logo abaixo dos campos. Veja [Senha](password.md).

### Proteger a conta com um código
Selecione **Segurança**, ao lado de **Alterar senha**, para ativar a verificação em duas etapas. O link só aparece onde a verificação em duas etapas está disponível. Veja [Verificação em duas etapas](two-factor.md).

### Baixar seus dados
**Seus dados**, logo acima de **Apagar minha conta**, dá uma cópia de tudo o que o Simulab guarda sobre você, em um arquivo JSON.
1. Clique em **Baixar meus dados**.
2. Digite sua senha atual e clique em **Baixar**. A senha protege o arquivo: ele tem dados pessoais.
3. O navegador salva `simulab-my-data-<data>.json`, e uma mensagem avisa que o download começou.

O arquivo traz sua conta (e-mail, nome de exibição, telefone, idioma preferido, datas), os papéis que você tem, os termos e a política de privacidade que você aceitou com o endereço IP de onde aceitou, as mudanças feitas nos seus papéis e quantos aparelhos estão conectados. Nunca traz sua senha nem qualquer código de segurança. Nada fica guardado no servidor, e você pode baixar quantas vezes quiser.

Cada download envia um e-mail avisando. Se esse e-mail chegar e não foi você, alguém sabe sua senha: troque-a na hora.

### Apagar sua conta
No fim da página, **Apagar minha conta** remove seus dados pessoais para sempre. Veja [Apagar sua conta](erase-account.md).

## Campos
| Campo | Significado | Regras |
|---|---|---|
| E-mail | O endereço com que você entra | Só leitura; não pode ser alterado aqui |
| Nome de exibição | O nome que aparece no menu da conta | Opcional; no máximo 120 caracteres |
| Idioma preferido | O idioma em que o app abre quando você entra e em que seus e-mails são escritos | Obrigatório; um dos três idiomas da lista |

## Mensagens
| Mensagem | O que significa | O que fazer |
|---|---|---|
| Seu perfil foi salvo. | O nome e o idioma foram gravados | Nada |
| Use no máximo 120 caracteres. | O nome de exibição é longo demais; **Salvar** fica desativado | Encurte o nome |
| Escolha um dos idiomas da lista. | O idioma enviado não é um dos três disponíveis | Escolha um idioma da lista e salve de novo |
| Não foi possível carregar seu perfil. | O Simulab não conseguiu ler seus dados agora | Clique em **Tentar novamente** |
| O download começou. Um e-mail de confirmação está a caminho. | O arquivo foi montado e entregue ao navegador | Nada; procure o arquivo nos seus downloads |
| A senha atual não está correta. | A senha digitada no diálogo de download está errada | Digite de novo; tentativas demais bloqueiam a conta por um tempo |

## Páginas relacionadas
- [Senha](password.md)
- [Verificação em duas etapas](two-factor.md)
- [Apagar sua conta](erase-account.md)
- [Como navegar](getting-around.md)
