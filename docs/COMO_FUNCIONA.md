# Como o Mr. Driver funciona

Guia para quem quer entender (e aprender com) o código do jogo. Ele explica cada parte do projeto, como as
cenas da história (cutscenes) são roteirizadas e como as animações foram feitas. Os nomes de arquivos e de
funções citados aqui existem no projeto: vale abrir o arquivo do lado enquanto lê.

- Unity 6000.3.4f1, 2D (URP), C#
- Todo o código, os comentários e os textos estão em português

---

## 1. Visão geral

### O caminho do jogador

```
Menu ─ JOGAR ─► TURBO RACER 3000 (o videogame "de 1987")
                     │ perdeu
                     ▼
               Casa do Jason ─► Autoescola ─► Exame na cidade ─► Garagem
                                                                   │
                                                                   ▼
                     Nivel1: ida ao mercado ─► Mercado (por dentro) ─► Nivel1: volta para casa
```

- **Menu**: blocos estilo "painel de console" (JOGAR, TUTORIAL, LOJA, OPÇÕES, SOBRE, NÍVEIS, SAIR).
- **Cenas de história** (`Historia_*`): visão de cima em grade, como os RPGs antigos, com diálogos.
- **Fases de direção** (`Tutorial`, `Historia_Autoescola`, `Historia_Exame`, `Nivel1`): o carro de verdade,
  com trânsito, pedestres, poderes e a carteira de motorista como "vida".

### As pastas

| Pasta | O que tem |
|---|---|
| `Assets/Scripts/Carro` | o carro do jogador e as peças dele (luzes, nitro, buzina, carteira...) |
| `Assets/Scripts/Camera` | a câmera que segue o carro |
| `Assets/Scripts/Trafego` | os carros da cidade e a Gangue do Escapamento |
| `Assets/Scripts/Pedestres` | as pessoas andando e atravessando |
| `Assets/Scripts/Poderes` | GPS, ABS, câmera lenta, advogado... |
| `Assets/Scripts/HUD` | velocímetro, minimapa, carteira na tela, pausa |
| `Assets/Scripts/Niveis` | chegada da fase, tela de "nível concluído", progresso, buraco |
| `Assets/Scripts/Historia` | tudo das cenas de história (grade, diálogos, roteiros) |
| `Assets/Scripts/Garagem` | escolha e pintura do carro |
| `Assets/Scripts/Loja` | moedas e itens |
| `Assets/Scripts/Menu` | menu principal e as telas (Sobre, Opções...) |
| `Assets/Scripts/Som` | sistema de som e música |
| `Assets/Scripts/Videogame` | o TURBO RACER 3000 |
| `Assets/Scripts/Tutorial` | a pista de treino |
| `Assets/Scripts/Antigos` | scripts que não são mais usados (guardados para consulta) |
| `Assets/Editor/...` | as ferramentas do menu **Mr Driver** (só rodam dentro da Unity) |

---

## 2. A ideia principal: o jogo é "montado" por código

Quase tudo do jogo (as cenas da história, os desenhos em pixel art, os sons, as músicas e as telas do
menu) é **gerado por ferramentas** que ficam no menu **Mr Driver** da Unity. O botão
**Mr Driver > História > Fazer tudo** recria tudo do zero.

Por que fazer assim?

1. **Dá para refazer sempre.** Mudou uma fala? Roda a ferramenta de novo e a cena sai atualizada.
2. **Nada se perde.** Desenhos, falas e sons ficam em código/texto, que o Git guarda bem.
3. **Consistência.** Todas as caixas de diálogo, todos os botões e todos os personagens seguem o mesmo padrão.

Como uma ferramenta é feita (exemplo simplificado de `Editor/Historia/MontarHistoria.cs`):

```csharp
[MenuItem("Mr Driver/História/2. Criar cena da casa")]   // aparece no menu da Unity
private static void MenuCasa()
{
    CriarCasa(1);   // cria a cena, os objetos e liga os componentes
}
```

Para "ligar" um campo `[SerializeField] private` de um componente pelo Editor, as ferramentas usam o
`SerializedObject` (é o mesmo que arrastar algo no Inspector):

```csharp
SerializedObject dados = new SerializedObject(roteiro);
dados.FindProperty("dialogo").objectReferenceValue = caixaDeDialogo;
dados.ApplyModifiedPropertiesWithoutUndo();
```

> Importante: rodar uma ferramenta de novo **sobrescreve** o que foi mexido à mão naquela cena.
> Para mudar algo de vez, mude no código da ferramenta (ou nos textos de `HistoriaTextos.cs`).

---

## 3. Pixel art feita por código

### Como um desenho vira imagem

`Editor/Arte/PixelArt.cs` cria uma imagem pixel por pixel e salva como PNG já configurado como sprite
(filtro **Point**, para os pixels ficarem quadradinhos e nítidos, sem borrar).

A classe `HistoriaArte.Tela` é uma "folha quadriculada" onde se pinta:

```csharp
Tela t = new Tela(16, 26);              // 16 x 26 pixels
t.Ret(2, 4, 13, 14, corDaTV);           // retângulo
t.P(13, 15, vermelho);                  // um pixel (a luzinha da TV)
t.Salvar(PastaCasa, "TVQuarto", Vector2.zero);
```

Os desenhos maiores (casas, mercado, pátio da autoescola, casas vizinhas) são feitos assim também, com
funções pequenas que se repetem: `Telhado`, `Janela`, `Porta`, `Arvore`, `Piscina`, `CarroEstacionado`...
As **casas vizinhas** (`Editor/Arte/CidadeArteCasas.cs`) usam números aleatórios com **semente fixa**
(`System.Random(semente)`): cada terreno sorteia o tipo de telhado, as cores e o quintal, mas sai igual
toda vez que a ferramenta roda.

### Personagens desenhados em arquivos de texto

Os personagens ficam em `Assets/Editor/ArteHistoria/*.txt`. Cada letra é uma cor da paleta:

```
paleta
k 40 22 44        ← contorno
p 255 214 170     ← pele
h 120 70 40       ← cabelo
fim

quadro cabeca_baixo
....kkhjhkkk....
...khhhjhhhhk...
.khihhihhihhihk.
.kippppppppppik.
```

- As cabeças têm 16x13 pixels. Os corpos (`Corpos.txt`) são compartilhados e só trocam as cores.
- O **retrato** da caixa de diálogo (32x32) é desenhado só pela metade esquerda: o código espelha para fazer
  a direita (rostos são simétricos, então dá metade do trabalho).
- Os capangas da gangue são o mesmo desenho com **troca de paleta** (`CriarPersonagem(..., trocas)`).

---

## 4. O carro do jogador

`Scripts/Carro/CarroController.cs` é o "motor" do carro: lê o teclado (WASD/setas, Espaço = freio de mão) e
move o `Rigidbody2D` com física de arcade, incluindo a derrapagem.

Os outros componentes **não mexem no carro**: eles só **leem** o que o controle expõe
(`VelocidadeAtual`, `EstaDerrapando`, `Freando`...) ou usam "ganchos" combinados
(`MultiplicadorVelocidade`, `ForcarAcelerar`...). Isso deixa cada peça independente:

| Componente | O que faz |
|---|---|
| `LuzesFreio`, `LuzesRe`, `Farois` | acendem conforme o estado do carro |
| `RastrosPneu` + `MarcaPneu` | deixam marca no chão ao derrapar |
| `RodasDianteiras` | giram as rodas da frente com o volante |
| `Nitro` | CTRL: multiplica a velocidade por um tempo |
| `Buzina`, `MolaParaChoque` | H buzina (os carros param); M liga o campo de força |
| `CarteiraMotorista` | a "vida": 20 pontos; batidas tiram pontos |
| `PiscarAoDano` | pisca o carro quando perde pontos |
| `VisualCarroJogador` | desenha o carro escolhido na garagem (modelo + cor) |

A câmera (`Scripts/Camera/CameraSegue.cs`) gira junto com o carro, mantém o carro perto do centro, olha um
pouco à frente conforme a velocidade e se afasta (zoom) quando o carro corre.

---

## 5. A cidade viva: trânsito, pedestres e a gangue

### Trânsito

- **`RedeTrafego` / `GrafoTrafego`**: as ruas são um **grafo**. Cada ponto fica no meio da rua e cada ligação é
  uma rua de mão dupla. Os carros andam na faixa da direita (a linha da rua deslocada para o lado).
- **`CarroTrafego`**: planeja um caminho de vários trechos. As curvas são **curvas de Bézier** (uma fórmula que
  liga dois segmentos com uma curva suave). Ele também:
  - escolhe a próxima rua em cada esquina (seguir reto é mais provável);
  - "enxerga" quem está na frente (outro carro, o jogador ou um pedestre atravessando) e freia com
    antecedência, usando a fórmula da distância de frenagem.
- **`Cruzamento`**: decide **a vez** de cada carro na esquina. Dois carros só passam juntos se os caminhos não
  se cruzam (por exemplo, dois carros vindo de frente seguindo reto).
- **`GeradorTrafego`**: espalha os carros no começo da fase e **recicla** os que ficaram longe ou travados,
  colocando-os de novo perto do jogador, sempre **fora da câmera**. Assim a cidade parece cheia sem precisar
  de centenas de carros.

### Pedestres

`Scripts/Pedestres/Pedestre.cs` anda por uma rota e, antes de atravessar, olha se os carros que vêm estão longe
o bastante para frear com calma. Se o jogador bater, a pessoa grita "ai!", dá um pulinho para o lado, fica
piscando sem colisão por um tempo e a carteira perde o dobro de pontos.

### A Gangue do Escapamento

`Scripts/Trafego/CarroRebelde.cs` usa o mesmo grafo, mas **sem respeitar regra nenhuma**: corre, não para na
esquina, ultrapassa pela contramão e as motos fazem zigue-zague. O `GangueRebelde` cria os veículos longe do
jogador quando ele sai do mercado.

---

## 6. As cenas de história

### O "motor" de grade (estilo RPG antigo)

| Script | O que faz |
|---|---|
| `MapaCasa` | a grade: cada célula é livre ou parede; diz quem ocupa cada célula |
| `MovimentoGrade` | anda **uma célula por vez**, deslizando suavemente de uma para a outra |
| `JogadorCasa` | lê o teclado e manda o `MovimentoGrade` andar; ENTER/E interage |
| `Interagivel` | base de tudo que dá para "examinar" (móveis, portas, NPCs, a TV) |
| `NpcHistoria` | um personagem com falas; vira para o jogador ao conversar |
| `PortaCasa` | porta trancada (com falas) ou saída da cena |
| `AnimadorPersonagem` | escolhe o desenho certo (direção + passo) e mostra os balõezinhos |
| `CameraCasa` | segue o Jason com suavidade sem mostrar fora do mapa |

Quem está **mais embaixo** na tela aparece **na frente**. A ordem de desenho é calculada pela linha da grade:
`sortingOrder = 100 + linha * 10`.

### Diálogos

- **`Fala`**: quem fala, o texto, um emote opcional e se a caixa "treme".
- **`HistoriaTextos.cs`**: **todas** as falas do jogo, num lugar só. É ótimo para revisar o roteiro.
- **`CaixaDialogo`**: a caixa estilo Pokémon. Ela tem:
  - o texto aparecendo **letra por letra**, com pausa maior depois de "." e ",";
  - um bipe de "voz" a cada duas letras. O tom muda conforme o personagem (pai grave, mãe aguda...) e o
    timbre também (onda quadrada, mais fina ou triângulo);
  - o retrato e o nome do personagem;
  - uma setinha piscando quando dá para continuar. ENTER completa o texto, ou passa para a próxima fala.

---

## 7. Como as cutscenes funcionam

Todas as cutscenes são feitas com **corrotinas** (`IEnumerator` + `yield return`). Uma corrotina é uma
função que pode **pausar no meio** e continuar depois, sem travar o jogo. Por isso uma cena inteira pode ser
escrita "em ordem", como um roteiro de teatro:

```csharp
private IEnumerator Abertura()                 // RoteiroCasa.cs (simplificado)
{
    jogador.Travado = true;                    // 1. o jogador não anda durante a cena

    yield return SairDaTV();                   // 2. espera a animação da TV terminar
    jason.MostrarEmote(Emote.Exclamacao);      // 3. balãozinho "!"

    yield return dialogo.Conversar(falas);     // 4. espera o jogador ler todas as falas

    objetivo.Mostrar("Fale com o seu pai");    // 5. mostra o objetivo
    jogador.Travado = false;                   // 6. devolve o controle
}
```

Cada `yield return` significa **"espere isto acabar"**:

| Escrito no código | Espera... |
|---|---|
| `yield return null;` | o próximo quadro (frame) |
| `yield return new WaitForSeconds(0.5f);` | meio segundo |
| `yield return dialogo.Conversar(falas);` | o jogador passar todas as falas |
| `yield return npc.Movimento.Andar(Direcao.Cima, 3);` | o personagem andar 3 quadradinhos |
| `yield return TransicaoTela.Escurecer();` | a tela escurecer |

### As peças de toda cutscene

1. **Travar o jogador**: `jogador.Travado = true`.
2. **Mover personagens**: `Andar(direção, passos)` em sequência (veja a gangue entrando no mercado em
   `RoteiroMercado.Pagar`).
3. **Falas**: `dialogo.Conversar(lista)`. Para comentários rápidos sem parar o jogo: `dialogo.Comentar(fala)`.
4. **Emotes**: `MostrarEmote(Emote.Coracao)`.
5. **Trocar de cena com fade**: `TransicaoTela.Carregar("Nome")` escurece, carrega e clareia.
6. **Objetivo na tela**: `ObjetivoHUD.Mostrar("...")` (faz um "plim").

### Os roteiros de cada cena

Cada cena de história tem um "roteiro", uma classe que herda de `RoteiroBase`:

| Roteiro | Cena |
|---|---|
| `RoteiroCasa` | casa no capítulo 1: sair da TV, falar com o pai, ir para a autoescola |
| `MissaoExame` | aulas da autoescola e o exame (pontos no chão, faltas e o instrutor reagindo) |
| `RoteiroGaragem` | capítulo 2: os pais comemoram, o pai ensina a garagem, a mãe pede o mercado |
| `RoteiroMercado` | compras, caixa, a gangue chegando e saindo |
| `MissaoMercado` | o Nivel1: a ida, a entrada no mercado e a volta com a gangue nas ruas |

Os objetos da cena (NPCs, portas, prateleiras) **avisam o roteiro** quando o jogador interage:
`RoteiroBase.Atual.ConversarCom(npc)`, `InteragirCom(objeto)`... Assim a mesma porta pode ter
comportamentos diferentes em cada capítulo.

### Exemplo: a gangue invadindo o mercado (`RoteiroMercado.Pagar`)

1. Toca a caixa registradora e a atendente conversa.
2. Os rebeldes, que estavam **desativados** na cena, são ativados na porta e andam para dentro, um por vez.
3. Quando o primeiro chega, as latas trocam de desenho ("derrubadas") e toca o som de latas.
4. A música troca para a de perseguição, todos falam e os rebeldes saem na ordem inversa.
5. A música volta ao normal e o jogador recupera o controle.

### Exemplo: o fim de fase "interrompido" (`ChegadaNivel` + `IFinalNivel`)

Quando o carro estaciona, a `ChegadaNivel` procura na cena alguém que implementa `IFinalNivel` e roda o
`AntesDaTela` antes da tela de "nível concluído". Na ida ao mercado, a `MissaoMercado` mostra as falas e
**interrompe** o fim da fase para carregar a cena de dentro do mercado. Na volta, deixa a fase terminar
normalmente.

### Exemplo: a surpresa do videogame (a TV encolhendo)

Esta é a cutscene mais "truque de cinema" do jogo:

1. No **TURBO RACER 3000**, quando a contagem da ficha termina, o código **tira uma foto da tela**:
   ```csharp
   yield return new WaitForEndOfFrame();                       // espera a tela terminar de desenhar
   Videogame.GuardarTela(ScreenCapture.CaptureScreenshotAsTexture());
   SceneManager.LoadScene("Historia_Casa");
   ```
2. A foto fica numa variável `static` (que sobrevive à troca de cena).
3. Na casa, `RoteiroCasa.SairDaTV` coloca essa foto **cobrindo a tela inteira** (um `RawImage` numa interface
   por cima de tudo). O jogador nem percebe que a cena trocou.
4. A foto **encolhe** até o tamanho e a posição da telinha da TV do quarto. A posição vem de
   `Camera.WorldToScreenPoint` (converte a posição do mundo para pixels da tela).
5. O tamanho diminui **multiplicando** (interpolação em logaritmo), não somando. Isso imita a câmera se
   afastando de verdade:
   ```csharp
   float largura = Mathf.Exp(Mathf.Lerp(Mathf.Log(cheia.width), Mathf.Log(alvo.width), k));
   ```
6. A foto some e fica o desenho da TV, mostrando um mini "GAME OVER". O Jason reclama.

---

## 8. Como as animações foram feitas

O jogo quase não usa o Animator da Unity. As animações são **código** que muda posição, tamanho, cor ou desenho
a cada quadro. Três ferramentas aparecem o tempo todo:

- `Mathf.Lerp(a, b, k)`: o valor entre `a` e `b` (k de 0 a 1).
- `Mathf.SmoothStep(0, 1, k)`: igual, mas começa e termina devagar (movimento "macio").
- `Mathf.Sin(tempo * velocidade)`: vai e volta sozinho (balançar, pulsar, piscar).

| Animação | Onde | Como funciona |
|---|---|---|
| Caminhada dos personagens | `AnimadorPersonagem` | 3 desenhos por direção (parado, pé esquerdo, pé direito). Na 1ª metade de cada passo mostra um pé, depois volta a "parado"; o pé alterna a cada passo. A esquerda é a direita espelhada (`flipX`). |
| Deslizar de célula em célula | `MovimentoGrade.Passo` | `Vector3.Lerp(inicio, fim, t)` com `t` indo de 0 a 1 |
| Balãozinho (!, ?, coração) | `AnimadorPersonagem.RotinaEmote` | escala de 0,3 a 1 com um "pulinho" de seno no meio |
| Caixa de diálogo abrindo | `CaixaDialogo.Animar` | `CanvasGroup.alpha` e posição subindo juntos |
| Caixa tremendo (susto) | `CaixaDialogo.Tremer` | posição com deslocamento aleatório que diminui |
| Portão da garagem | `PortaCasa.Enrolar` | a escala vertical encolhe (o desenho tem o apoio em cima) com uma tremidinha |
| Brilho nas prateleiras | `ObjetoRoteiro` | posição sobe e desce com seno |
| Seta do objetivo | `SetaObjetivo` | gira em volta do carro apontando com `Atan2` |
| Blocos do menu | `BlocoMenu` | ao selecionar: cresce, a borda pulsa (seno) e o ícone flutua |
| Telas do menu | `TelaMenu` | entram deslizando 40 px e aparecendo (alpha) em 0,3 s |
| TV encolhendo | `RoteiroCasa.SairDaTV` | foto da tela interpolada até a TV (seção 7) |
| Explosão no videogame | `TurboRacer.Batida` | troca entre 3 desenhos a cada 0,08 s |
| Textos piscando | `TurboRacer` | `Mathf.Repeat(Time.time, 1f) < 0.6f` liga/desliga |
| Estrada andando | `TurboRacer.Rolar` | as faixas e a grama descem de 0 a 12 px e voltam (padrão que se repete) |
| Câmera seguindo o carro | `CameraSegue` | gira com `LerpAngle` e ajusta o zoom pela velocidade |
| Carro piscando ao bater | `PiscarAoDano` | liga e desliga o desenho rapidamente |

**Dica:** quando o jogo está **pausado** (`Time.timeScale = 0`), use `Time.unscaledDeltaTime` nas animações
de interface. Senão elas param junto com o jogo. A caixa de diálogo e os menus fazem isso.

---

## 9. O TURBO RACER 3000 (o videogame falso)

`Scripts/Videogame/TurboRacer.cs` é uma **máquina de estados**:

```
Titulo ─ENTER─► Carregando (a barra trava no meio) ─► Jogando ─bateu─► Batida (explosão)
                                                          ▲                │
                                                          │                ▼
                                            (minijogo) ENTER ◄── FimDeJogo: "INSIRA UMA FICHA"
                                                                           │ (história) acabou o tempo
                                                                           ▼
                                                                   foto da tela → casa do Jason
```

- **Tudo é interface (UI)**: a "tela" tem 160x120 pixels e é ampliada 8,4 vezes. As posições são arredondadas
  para inteiros, para o movimento ficar "duro" como num console velho.
- **Dificuldade**: a velocidade e a frequência de carros aumentam com o tempo.
  - aos 8 s chegam carros que mudam de faixa **sem dar seta**;
  - aos 17 s, caminhões que ocupam duas faixas;
  - aos 31 s, ondas na contramão.
  - A regra `FaixasOcupadas` garante que **as três faixas nunca fecham juntas**: é difícil, mas sempre dá
    para passar.
- **Efeito de TV de tubo**: linhas horizontais (imagem repetida), cantos escuros arredondados, um brilho no
  vidro e o **reflexo do rosto do Jason**, que aparece mais quanto melhor você joga (é a pista da surpresa!).
- **Letras pixeladas**: `TextoPixel` monta cada palavra com uma imagem por letra (`FontePixel`).
- **Minijogo**: depois de terminar o Nivel1, o bloco "TURBO RACER 3000" aparece na tela de Níveis. O recorde
  fica salvo em `PlayerPrefs`.

---

## 10. Sons e músicas feitos por código

### Efeitos (`Editor/Sons/SintetizadorSons.cs`)

Um som digital é uma lista de números entre -1 e 1 (22.050 números por segundo). O sintetizador cria cada
som somando "ingredientes":

| Ingrediente | Para que serve | Exemplo no jogo |
|---|---|---|
| Onda quadrada | som de videogame | bipes, moeda, falas |
| Triângulo | som mais suave | voz do narrador, baixo das músicas |
| Serra (dente de serra) | som "áspero" | motor, moto |
| Seno | som puro | sininhos, bumbo |
| Ruído (números aleatórios) | chiado | batidas, pneu, vento |
| Filtro passa-baixa / passa-faixa | deixa o ruído mais grave ou mais "assobiado" | derrapagem |
| Envelope (ataque e queda) | como o som começa e termina | toda nota |

Exemplo, a moeda clássica (duas notas rápidas):

```csharp
Tom(b, 0.00, 0.07, t => 988,  Onda.Quadrada, 0.35);            // si
Tom(b, 0.07, 0.26, t => 1319, Onda.Quadrada, 0.35, decai: 9);  // mi, mais longa
```

Os sons em **loop** (motor, derrapagem, moto) são feitos um pouquinho mais longos e o fim é misturado com o
começo (`FecharLoop`), para não dar "clique" na emenda.

### Músicas (`Editor/Sons/CompositorMusicas.cs`)

Cada música é escrita como texto. A melodia usa **graus da escala** (1 = dó numa música em dó maior), com a
duração em semicolcheias (16 = um compasso):

```
"3:4 2:2 1:2 5,:4 1:4"     →  mi (longa), ré, dó, sol de baixo, dó
```

O compositor toca junto, para cada compasso, o **acorde**, o **baixo**, o **arpejo** e a **bateria**
(bumbo = seno descendo rápido; caixa e chimbal = ruído curto). No fim, a "cauda" (ecos e notas ainda soando)
é jogada para o começo, para a música repetir sem emenda.

### Tocando no jogo (`Scripts/Som/`)

- **`Sons`**: liga **sozinho** em qualquer cena (`RuntimeInitializeOnLoadMethod`). Ele:
  - toca a música de cada cena;
  - coloca som no carro e em todos os botões;
  - oferece `Sons.Tocar("Moeda")` para o resto do código.
- **`BancoSons`** (`Assets/Resources/BancoSons.asset`): a lista "nome → som", com volume. Dá para trocar sons
  pelo Inspector.
- **`SomCarro`**: o motor muda de tom com a velocidade (simulando marchas), a derrapagem aumenta de volume
  conforme o carro escorrega, e há bipe de ré, freada, buraco, batidas e o som de cada poder.

---

## 11. Menu, progresso e salvamento

- **`MenuController`**: troca os painéis (menu, opções, sobre, níveis, loja).
- **`MenuBlocos` / `BlocoMenu`**: os blocos do menu, com navegação pelo teclado e descrição na barra de baixo.
- **`TelaMenu`**: o comportamento comum das telas Sobre, Opções, Níveis e Loja (ESC volta).
- **`OpcoesMenu`**: volume com porcentagem, tela cheia e "recomeçar história" (com confirmação).
- **Progresso**: `ProgressoNiveis` guarda em `PlayerPrefs` quais cenas foram concluídas.
  `ProgressoHistoria.ProximaCena()` usa isso para o JOGAR continuar de onde o jogador parou.

---

## 12. Mãos na massa: como mudar as coisas

| Quero mudar... | Onde |
|---|---|
| uma fala | `Assets/Editor/Historia/HistoriaTextos.cs` e depois **Mr Driver > História > Fazer tudo** |
| o desenho de um personagem | `Assets/Editor/ArteHistoria/<Nome>.txt` e depois **1. Desenhar personagens** |
| a dificuldade do TURBO RACER | componente **TurboRacer** na cena `Historia_Videogame` |
| o volume de um som | `Assets/Resources/BancoSons.asset` |
| um som ou uma música | `SintetizadorSons.cs` / `CompositorMusicas.cs` e depois **Mr Driver > Sons > Gerar sons e músicas** |
| quantos carros na cidade | componente **GeradorTrafego** no objeto `RedeTrafego` |
| a câmera | componente **CameraSegue** no prefab `Main Camera` |

### Sugestões para estudar o código

1. Comece por `RoteiroCasa.cs`: é curto e mostra o padrão de cutscene com corrotinas.
2. Depois `MovimentoGrade.cs` e `AnimadorPersonagem.cs`, para ver o movimento e a animação de caminhada.
3. Para física e matemática de jogo: `CarroController.cs` e `CarroTrafego.cs` (curvas de Bézier e frenagem).
4. Para algo divertido e completo num arquivo só: `TurboRacer.cs`.
5. Para entender como um som é feito "na mão": `SintetizadorSons.cs`, função `Tom`.
