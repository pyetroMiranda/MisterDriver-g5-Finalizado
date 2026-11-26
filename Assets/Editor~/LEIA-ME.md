# Ferramentas do menu "Mr Driver" (desativadas)

Esta pasta guarda todas as ferramentas do menu **Mr Driver** do Unity
(História, Sons, Menu, Níveis, Loja, HUD, Minimapa, Trânsito, Pedestres,
Poderes, Chegada, Tutorial).

Como o nome da pasta termina em `~`, o Unity a ignora: nada aqui é
compilado e o menu Mr Driver some da barra. O jogo continua funcionando
normalmente, porque nenhum script do jogo depende destas ferramentas.

## Como reativar

1. Feche o Unity (ou deixe aberto, ele recarrega sozinho).
2. Renomeie a pasta `Assets/Editor~` para `Assets/Editor`.
3. Mova o arquivo `Editor.meta` de dentro dela para `Assets/Editor.meta`
   (ao lado da pasta).
4. Volte ao Unity e espere compilar. O menu Mr Driver aparece de novo.

Pelo Git Bash, na pasta do projeto:

```bash
git mv "Assets/Editor~" Assets/Editor && git mv Assets/Editor/Editor.meta Assets/Editor.meta
```

Este arquivo (`LEIA-ME.md`) pode ficar dentro da pasta, o Unity só vai
criar um `.meta` para ele.
