using System.Collections.Generic;

// Todas as falas da história num lugar só. A ferramenta (Mr Driver > História) copia estes textos
// para os componentes da cena; depois disso dá para editar direto no Inspector.
//
// Personagens:
//   JASON     - 18 anos, dorminhoco e otimista. Joga simulador de corrida e jura que é o "Mr. Driver".
//   BETÃO   - o pai. Taxista aposentado, piadista, apaixonado pelo seu NISSAN KICKS novinho (e por todas as tecnologias dele).
//   CIDA    - a mãe. Enfermeira, superprotetora, fala rápido e grita "CINTO DE SEGURANÇA!".
//   GERVÁSIO - o instrutor. Nervoso, dramático, monocelha, prancheta e "AI, MEU CORAÇÃO!".
//   JUSSARA - a atendente do mercado. Masca chiclete, sabe de todas as fofocas e ama carros.
//   TURBO   - chefe da Gangue do Escapamento: rebeldes que dirigem sem respeitar nenhuma lei de trânsito.
public static class HistoriaTextos
{
    public static PersonagemHistoria Jason, Pai, Mae, Instrutor, Atendente, Rebelde, Rebelde2, Rebelde3;

    private static Fala F(PersonagemHistoria quem, string texto, Emote emote = Emote.Nenhum, bool tremer = false) =>
        new Fala(quem, texto, emote, tremer);

    private static Fala Narrador(string texto) => new Fala(null, texto);

    private static List<Fala> L(params Fala[] falas) => new List<Fala>(falas);

    // ======================================================================
    // Casa
    // ======================================================================

    // O jogo começa no TURBO RACER 3000: quando o jogador perde, a câmera sai da TV e o Jason reclama.
    // {pontos} vira os pontos da partida que o jogador acabou de fazer.
    public static List<Fala> Acordar() => L(
        F(Jason, "NÃÃÃO!! De novo esse caminhão!", Emote.Exclamacao, true),
        F(Jason, "Esse jogo é impossível! Quem faz um carro mudar de faixa sem dar seta?!", Emote.Gota),
        F(Jason, "{pontos} pontos... E o tal do TONHÃO lá no primeiro lugar com 99999. COMO, gente?!", Emote.Interrogacao),
        Narrador("Sábado, 7 da manhã. Na casa da família Driver..."),
        F(Mae, "JAAAASON! Desliga esse videogame e vem tomar café! O seu pai quer falar com você!", Emote.Exclamacao, true),
        F(Jason, "Só mais uma partidinha, mãe! Eu tava quase passando o caminhão!", Emote.Gota),
        F(Mae, "AGORA, JASON!", Emote.Exclamacao, true),
        F(Jason, "Tá bom, tá bom! ...O pai quer falar comigo? Será que ele descobriu o arranhão na bicicleta dele?", Emote.Gota),
        Narrador("Use as SETAS ou W A S D para andar, SHIFT para correr e ENTER, ESPAÇO ou E para conversar e examinar as coisas."));

    public static List<Fala> ConversaPai() => L(
        F(Pai, "Ôôô, a bela adormecida acordou! Achei que ia ter que te acordar com a buzina do Kicks!"),
        F(Jason, "Bom dia, pai. A mãe disse que você queria falar comigo...?"),
        F(Pai, "Queria sim. Senta aí. ...Não, não senta, fica em pé mesmo que é rapidinho."),
        F(Pai, "Filho, você fez 18 anos semana passada. E sabe o que isso significa?"),
        F(Jason, "Que eu posso comprar bolo sem pedir a sua autorização?", Emote.Interrogacao),
        F(Pai, "Hahaha! Não! Significa que tá na hora de você TIRAR A CARTEIRA DE MOTORISTA!", Emote.Exclamacao, true),
        F(Jason, "SÉRIO?! Pai, eu treino no videogame há ANOS! Eu nasci pra isso! Eu sou praticamente o MR. DRIVER!", Emote.Coracao, true),
        F(Pai, "Mr. Driver, é? Hehe. Trinta anos de táxi e eu nunca levei uma multa."),
        F(Pai, "...Bom, só aquela vez. E aquela outra. Mas aquelas não contam."),
        F(Pai, "Mas escuta: dirigir de verdade não é igual ao seu videogame, filho. Na rua não tem \"continue\"."),
        F(Pai, "Eu já te matriculei na Autoescola Freio de Mão. O instrutor é o Gervásio, meu amigo de infância."),
        F(Pai, "Primeiro, aulas no pátio da autoescola. Depois, o EXAME FINAL pelas ruas da cidade!"),
        F(Pai, "E você vai fazer tudo isso... com o meu NISSAN KICKS NOVINHO!"),
        F(Jason, "O SEU KICKS?! Você não deixa nem eu encostar nele! Você dá bom dia pra ele!", Emote.Exclamacao, true),
        F(Pai, "Claro que dou! Ele tem câmera 360, alerta de ponto cego, sensor de estacionamento, frenagem de emergência..."),
        F(Pai, "Ele é praticamente mais inteligente que eu. Então trata ele com carinho, viu?"),
        F(Pai, "O Gervásio já tá te esperando na autoescola. Vai lá, campeão! E escuta TUDO o que ele falar!"));

    public static List<Fala> PaiDepois() => L(
        F(Pai, "Vai lá, campeão! O Gervásio não gosta de esperar. Ele fica nervoso."),
        F(Pai, "...Bom, ele fica nervoso com tudo. Mas com espera ele fica MAIS."));

    public static List<Fala> MaeAntes() => L(
        F(Mae, "Bom dia, meu amor! Vai falar com o seu pai, ele tá todo misterioso desde ontem!"),
        F(Mae, "E come alguma coisa antes! Saco vazio não para em pé!"));

    public static List<Fala> ConversaMae() => L(
        F(Mae, "Seu pai me contou tudo! Meu filho vai DIRIGIR?!", Emote.Exclamacao, true),
        F(Mae, "Ai, meu Deus. Tá. Tá tudo bem. Eu tô calma. EU TÔ CALMA!", Emote.Gota, true),
        F(Mae, "Regra número um: CINTO DE SEGURANÇA! Regra número dois: CINTO DE SEGURANÇA! Regra número três..."),
        F(Jason, "Deixa eu adivinhar... cinto de segurança?"),
        F(Mae, "Não! Regra número três é: me liga quando chegar. Mas usa o cinto também!"),
        F(Mae, "Toma, fiz uma marmita pra você. Prova de direção dá fome.", Emote.Coracao),
        Narrador("Jason guardou a marmita da mãe na mochila. Tem cheiro de lasanha."));

    public static List<Fala> MaeDepois() => L(
        F(Mae, "Cinto! Retrovisor! Seta! E não fala com estranhos!"),
        F(Mae, "...A não ser com o instrutor. Com o instrutor pode falar."));

    public static List<Fala> PortaFechada() => L(
        F(Jason, "Melhor falar com o pai antes de sair. Senão ele me liga dezessete vezes.", Emote.Gota));

    public static List<Fala> Saida() => L(
        F(Mae, "JASON! ESPERA!", Emote.Exclamacao, true),
        F(Jason, "Que foi, mãe?!", Emote.Interrogacao),
        F(Mae, "...Boa sorte, filho. Eu te amo.", Emote.Coracao),
        F(Mae, "E USA O CINTO!", Emote.Exclamacao, true),
        F(Jason, "Também te amo, mãe! Fui!"));

    // Objetos da casa (o que o Jason pensa quando examina)
    public static List<Fala> Cama() => L(F(Jason, "Minha cama. Ainda tá quentinha... Não, Jason! Foco!"));
    public static List<Fala> CamaDepois() => L(F(Jason, "Cama, hoje não. Hoje eu vou tirar a minha carteira!"));
    public static List<Fala> GuardaRoupa() => L(F(Jason, "Meu guarda-roupa. Metade é moletom laranja. A outra metade também."));
    public static List<Fala> Computador() => L(
        F(Jason, "Meu computador com o simulador de corrida aberto."),
        F(Jason, "Recorde da pista: 1 minuto e 32 segundos. Na vida real deve ser igualzinho, né?"));
    public static List<Fala> Estante() => L(F(Jason, "Troféu de 3º lugar no kart da escola. ...Tinha três participantes."));
    public static List<Fala> Roupas() => L(F(Jason, "Uma pilha de roupas. Limpa ou suja? Só o nariz sabe dizer."));
    public static List<Fala> Poster() => L(F(Jason, "O pôster do carro dos meus sonhos. Um dia eu chego lá!"));
    public static List<Fala> JanelaQuarto() => L(F(Jason, "Céu azul, sol brilhando... Dia perfeito pra alguma coisa importante."));
    public static List<Fala> Geladeira() => L(
        F(Jason, "A geladeira tá cheia de ímãs de todos os lugares onde o pai levou passageiro de táxi."),
        F(Jason, "Tem um bilhete: \"Jason, NÃO coma o pudim. Ass.: sua mãe\"."));
    public static List<Fala> Fogao() => L(F(Jason, "Cheirinho de café passado na hora... e de lasanha?"));
    public static List<Fala> Pia() => L(F(Jason, "A louça de ontem. Hoje é a vez do... ihhh, é a minha vez."));
    public static List<Fala> Fruteira() => L(F(Jason, "Uma banana e uma maçã. Café da manhã de campeão!"));
    public static List<Fala> Mesa() => L(
        F(Jason, "Pão na chapa, café com leite e um bilhetinho:"),
        Narrador("\"Boa sorte hoje, filho! Você vai arrasar. Beijos, mãe e pai.\""),
        F(Jason, "Boa sorte...? Pra quê?", Emote.Interrogacao));
    public static List<Fala> MesaDepois() => L(F(Jason, "Ah, então ERA por isso o bilhete de boa sorte!", Emote.Exclamacao));
    public static List<Fala> TV() => L(Narrador("No jornal: \"Trânsito tranquilo na cidade hoje. Ótimo dia para tirar a carteira!\""),
                                        F(Jason, "Que coincidência...", Emote.Gota));
    public static List<Fala> Sofa() => L(F(Jason, "O sofá do pai. Tem uma marca perfeita do formato dele no meio."));
    public static List<Fala> Planta() => L(F(Jason, "A samambaia da mãe. O nome dela é Gertrudes. Não pergunte."));
    public static List<Fala> Relogio() => L(F(Jason, "7 horas e 3 minutos. Atrasado, como sempre."));
    public static List<Fala> QuadroTaxi() => L(
        F(Jason, "Uma foto do pai com o táxi antigo dele. Do lado, uma foto MAIOR dele com o Kicks novo. Prioridades."));
    public static List<Fala> Sapateira() => L(F(Jason, "Os chinelos do pai, com \"BETÃO\" escrito de caneta. Ninguém ia roubar, pai."));

    // ======================================================================
    // Exame
    // ======================================================================

    // ======================================================================
    // Autoescola (aulas no pátio)
    // ======================================================================

    public static List<Fala> AberturaAutoescola() => L(
        Narrador("Autoescola Freio de Mão. O pátio de treino mais famoso (e mais amassado) da cidade."),
        F(Jason, "Uau... que gráficos!", Emote.Coracao),
        F(Instrutor, "Gráficos? Que gráficos? É um pátio, menino.", Emote.Interrogacao),
        F(Instrutor, "Bom dia! Eu sou o Gervásio, seu instrutor. Vinte e cinco anos de profissão!"),
        F(Instrutor, "E só três pontes de safena.", Emote.Gota),
        F(Jason, "Prazer, seu Gervásio! Pode ficar tranquilo, eu nasci pra isso!"),
        F(Instrutor, "Todos dizem isso... Todos. Dizem. Isso.", Emote.Gota),
        F(Instrutor, "Antes do exame na cidade, você vai fazer CINCO aulas aqui no pátio. Cada uma está pintada no chão."),
        F(Instrutor, "Derrubou cone, bateu no muro? FALTA. Com 10 faltas a gente começa tudo de novo. E eu tomo meu remédio."),
        F(Instrutor, "AULA 1: a RETA. Segure W ou a SETA PARA CIMA para andar. Siga a seta amarela!"));

    public static string ObjetivoAula1 => "Aula 1: entre na reta e acelere";
    public static List<Fala> Aula1Entrada() => L(
        F(Instrutor, "Isso! Agora lá no fim da reta tem a faixa de PARE."),
        F(Instrutor, "Para frear, segure S ou a SETA PARA BAIXO. Pare o carro em cima do PARE, sem passar da linha!"));

    public static string ObjetivoAula1Pare => "Aula 1: pare o carro na faixa de PARE";
    public static List<Fala> Aula1Pare() => L(
        F(Instrutor, "Parou certinho! Meu coração nem acelerou.", Emote.Coracao),
        F(Instrutor, "AULA 2: CURVAS. Vire com A e D, ou com as setas dos lados."),
        F(Instrutor, "Dê uma volta inteira no circuito em volta da ilha, seguindo as setas do chão. E tira o pé na curva!"));

    public static string ObjetivoAula2 => "Aula 2: siga o circuito pelas setas do chão";
    public static List<Fala> Aula2Meio() => L(F(Instrutor, "Boa curva! Agora a de cima... devagarinho, com amor."));
    public static List<Fala> Aula2Fim() => L(
        F(Jason, "Viu só? Curva é comigo mesmo!"),
        F(Instrutor, "Tá bom, tá bom. AULA 3: SLALOM. Vá até o corredor de cones ali na direita."),
        F(Instrutor, "Costure entre os cones do meio sem derrubar NENHUM. Eles têm família também."));

    public static string ObjetivoAula3 => "Aula 3: entre no corredor de cones";
    public static string ObjetivoAula3Fim => "Aula 3: costure entre os cones até a chegada";
    public static List<Fala> Aula3() => L(
        F(Instrutor, "Que elegância! Parecia um balé!", Emote.Coracao),
        F(Instrutor, "AULA 4: GARAGEM. Essa é de RÉ. Você vai entrar DE COSTAS na garagem de cones lá em cima."),
        F(Instrutor, "Pare na frente dela com o carro virado para baixo e segure S: com o carro parado, ele anda de ré."),
        F(Instrutor, "A área fica AMARELA se você entrar de frente. Tem que ficar VERDE!"));

    public static string ObjetivoAula4 => "Aula 4: estacione DE RÉ dentro da garagem";
    public static List<Fala> Aula4() => L(
        F(Instrutor, "DE RÉ! Que orgulho. Vou contar para o seu pai.", Emote.Coracao),
        F(Jason, "Ele vai dizer que no tempo dele era mais difícil."),
        F(Instrutor, "E era mesmo. AULA 5: a temida BALIZA. Estacione na vaga amarela, paralelo à calçada."),
        F(Instrutor, "Passe da vaga, pare, e entre de ré virando o volante. Calma que dá!"));

    public static string ObjetivoAula5 => "Aula 5: faça a baliza na vaga amarela";
    public static List<Fala> Aula5() => L(
        F(Instrutor, "BALIZA PERFEITA! ...Bom, quase. Mas tá dentro!", Emote.Exclamacao, true),
        F(Instrutor, "Última tarefa: estacione na VAGA em frente à autoescola. Tem um cafezinho me esperando lá."));

    public static string ObjetivoVagaAutoescola => "Estacione na VAGA em frente à autoescola";

    public static List<Fala> ReacoesCone() => L(
        F(Instrutor, "O CONE! Coitado do cone!", Emote.Exclamacao),
        F(Instrutor, "Cone é pra DESVIAR, não pra abraçar!", Emote.Exclamacao),
        F(Instrutor, "Mais um cone pra minha coleção de traumas...", Emote.Gota),
        F(Instrutor, "Esse cone tinha nome, Jason! Era o Jorginho!", Emote.Gota));

    public static List<Fala> ReacoesMuro() => L(
        F(Instrutor, "AI, MEU CORAÇÃO! Isso era um MURO!", Emote.Exclamacao),
        F(Instrutor, "O KICKS NOVO DO SEU PAI! Ele vai me matar!", Emote.Exclamacao),
        F(Instrutor, "Respira, Gervásio... respira... Um, dois, três...", Emote.Gota));

    public static List<Fala> ReprovadoAutoescola() => L(
        F(Instrutor, "Chega, chega, CHEGA! Dez faltas!", Emote.Exclamacao, true),
        F(Instrutor, "Respira fundo, Jason. Eu também respiro. Vamos começar as aulas de novo.", Emote.Gota));

    public static List<Fala> AulasPerfeitas() => L(
        F(Instrutor, "Todas as aulas com ZERO faltas! Em 25 anos eu nunca vi isso!", Emote.Exclamacao, true),
        F(Jason, "Eu falei que nasci pra isso!", Emote.Coracao));

    public static List<Fala> AulasConcluidas() => L(
        F(Instrutor, "Aulas concluídas! Com {faltas} falta(s), mas concluídas."),
        F(Jason, "Bora! Tô pronto!"));

    public static List<Fala> FimAutoescola() => L(
        F(Instrutor, "Agora vem o que importa: o EXAME FINAL. Lá fora, na cidade."),
        F(Instrutor, "Com trânsito de verdade, pedestres de verdade e buracos de verdade."),
        F(Jason, "Com... trânsito de verdade?", Emote.Gota),
        F(Instrutor, "Bem-vindo à vida real, Mr. Driver. Vamos!"),
        Narrador("Gervásio respirou fundo três vezes. E lá foram eles, para as ruas da cidade..."));

    // ======================================================================
    // Exame final (cidade)
    // ======================================================================

    public static List<Fala> AberturaExame() => L(
        Narrador("EXAME FINAL. As ruas da cidade, com trânsito de verdade."),
        F(Instrutor, "Aqui fora é diferente, Jason. Carros, pedestres, buracos... e nenhum cone pra te proteger."),
        F(Instrutor, "Cada batida tira pontos da carteira e vira falta. Bateu 5 vezes: REPROVADO. E eu tenho um treco."),
        F(Jason, "Pode deixar. Eu treinei com o melhor instrutor da cidade!"),
        F(Instrutor, "...Isso foi um elogio? Ai, meu coração.", Emote.Coracao),
        F(Instrutor, "Siga a seta amarela até o primeiro ponto. Devagarinho. Com amor."));

    public static string ObjetivoPonto1 => "Siga a seta amarela até o ponto marcado";
    public static List<Fala> Ponto1() => L(
        F(Instrutor, "Muito bem! Viu? Nem doeu. Pelo menos em mim.", Emote.Coracao),
        F(Instrutor, "Cuidado com os outros carros: eles param nos cruzamentos e para os pedestres. Você também!"));

    public static string ObjetivoPonto2 => "Continue seguindo a seta";
    public static List<Fala> Ponto2() => L(
        F(Jason, "Tô pegando o jeito, seu Gervásio!"),
        F(Instrutor, "Tá sim. Eu tô até respirando normal. Olha só."),
        F(Instrutor, "Agora presta atenção: no próximo ponto tem uma placa de PARE."),
        F(Instrutor, "PARAR é PARAR, hein? Carro parado de verdade. Nada de \"quase parar\"!", Emote.Exclamacao, true));

    public static string ObjetivoPare => "Pare o carro totalmente na placa de PARE";
    public static List<Fala> Pare() => L(
        F(Instrutor, "Isso! Parou bonitinho! Meu cardiologista vai ficar orgulhoso.", Emote.Coracao),
        F(Instrutor, "Ah, e o freio de mão é o ESPAÇO. Mas NADA de ficar derrapando no meu exame, ouviu?!"),
        F(Instrutor, "Igualzinho na autoescola: última tarefa, estacionar na vaga. Com calma."));

    public static string ObjetivoVaga => "Estacione dentro da vaga e fique parado";


    // A TV do quarto com o TURBO RACER 3000 ({recorde} e {tonhao} viram os números na hora)
    public static List<Fala> TVQuarto() => L(
        F(Jason, "O meu videogame com o TURBO RACER 3000. Um clássico de 1987!"),
        F(Jason, "Meu recorde: {recorde} pontos. Em primeiro lugar: TONHÃO, com {tonhao}.", Emote.Interrogacao),
        F(Jason, "Ninguém nunca viu esse Tonhão. O nome dele já veio gravado no cartucho!", Emote.Gota),
        F(Jason, "...Depois eu jogo mais. Agora não dá pra deixar o pai esperando."));

    public static List<Fala> TVQuarto2() => L(
        F(Jason, "TURBO RACER 3000. Meu recorde: {recorde} pontos."),
        F(Jason, "Depois de dirigir de verdade, esse jogo até parece fácil..."),
        F(Jason, "...Mentira. Continua impossível. E o Tonhão continua lá em cima com {tonhao}.", Emote.Gota));

    public static List<Fala> ReacoesBatida() => L(
        F(Instrutor, "Aqui não tem ficha extra, hein! Isso não é videogame!", Emote.Exclamacao),
        F(Instrutor, "AI, MEU CORAÇÃO!", Emote.Exclamacao),
        F(Instrutor, "CADÊ A SETA?! ...Quer dizer, CADÊ O FREIO?!", Emote.Exclamacao),
        F(Instrutor, "Isso vai para a minha prancheta... em vermelho.", Emote.Gota),
        F(Instrutor, "Eu tenho FAMÍLIA, Jason!", Emote.Gota),
        F(Instrutor, "O KICKS NOVO DO SEU PAI! Ele vai me matar!", Emote.Exclamacao),
        F(Instrutor, "Respira, Gervásio... respira... Um, dois, três...", Emote.Gota));

    public static List<Fala> Reprovado() => L(
        F(Instrutor, "PARA! PARA TUDO! Chega, chega, chega!", Emote.Exclamacao, true),
        F(Instrutor, "Jason, você está... REPROVADO.", Emote.Gota),
        F(Jason, "Ah, não...", Emote.Gota),
        F(Instrutor, "Calma, calma. Todo mundo reprova uma vez. Eu reprovei sete.", Emote.Coracao),
        F(Instrutor, "Respira fundo e vamos tentar de novo do começo."));

    public static List<Fala> AprovadoPerfeito() => L(
        F(Instrutor, "Estacionou! E com ZERO faltas!", Emote.Exclamacao, true),
        F(Instrutor, "Em 25 anos eu nunca vi isso. Vou até contar pra minha cardiologista!", Emote.Coracao),
        F(Instrutor, "Jason Driver... você está APROVADO!", Emote.Coracao, true),
        F(Jason, "EU SABIA! EU SOU O MR. DRIVER!", Emote.Coracao, true));

    public static List<Fala> Aprovado() => L(
        F(Instrutor, "Estacionou! Com {faltas} falta(s), mas estacionou."),
        F(Instrutor, "E o meu coração continua batendo, então... você está APROVADO!", Emote.Coracao, true),
        F(Jason, "AEEEE! EU SOU O MR. DRIVER!", Emote.Coracao, true));

    public static List<Fala> Despedida() => L(
        F(Instrutor, "Agora vai lá, Mr. Driver. As ruas são suas."),
        F(Instrutor, "Respeite os outros carros, os pedestres e os buracos. E cuide dessa carteira: são 20 pontos!"),
        F(Instrutor, "Se bater muito, ela é suspensa. E aí você volta pra MIM.", Emote.Gota),
        F(Jason, "Pode deixar, seu Gervásio! Obrigado por tudo!"),
        F(Instrutor, "De nada. Agora, se me der licença... eu vou deitar um pouquinho."),
        Narrador("Com a carteira no bolso, Jason voltou correndo para casa para contar a novidade..."));
    // ======================================================================
    // Capítulo 2: de volta para casa (Historia_Garagem)
    // ======================================================================

    public static List<Fala> ChegadaCasa() => L(
        Narrador("De volta para casa. Com a carteira de motorista no bolso."),
        F(Mae, "ELE CHEGOU! BETÃO, ELE CHEGOOOU!", Emote.Exclamacao, true),
        F(Pai, "E aí, campeão? Como foi? Pode falar. Eu aguento.", Emote.Gota),
        F(Jason, "Pai... mãe..."),
        F(Jason, "EU PASSEI! TÁ AQUI A MINHA CARTEIRA!", Emote.Coracao, true),
        F(Mae, "AAAAAH! MEU BEBÊ É MOTORISTA!", Emote.Coracao, true),
        F(Mae, "Deixa eu ver a foto! ...Ai, que lindo! Saiu de olho fechado, mas que lindo!"),
        F(Pai, "EU SABIA! Puxou o pai! Trinta anos de táxi correm nessas veias!", Emote.Coracao),
        F(Pai, "E o Gervásio? Sobreviveu?"),
        F(Jason, "Ele disse que ia deitar um pouquinho."),
        F(Pai, "Hehe, normal. Quando eu tirei a carteira com ele, ele deitou uma semana."),
        F(Pai, "Bom... agora que você é motorista de verdade, eu tenho uma SURPRESA."),
        F(Pai, "Me encontra na porta da GARAGEM, ali no canto da sala. Sem pressa: dá uma volta, come alguma coisa..."),
        F(Mae, "Isso! Eu fiz PUDIM pra comemorar! Tá na mesa da cozinha. Só UM pedaço!", Emote.Coracao),
        Narrador("Ande pela casa à vontade. Quando estiver pronto, fale com o pai na porta da garagem."));

    public static List<Fala> PaiPortaGaragem() => L(
        F(Pai, "Chegou! Pronto pra surpresa?"),
        F(Pai, "Essa porta aqui, filho... é a GARAGEM. A partir de hoje, você também tem a chave."),
        Narrador("O pai tirou do bolso um chaveiro com um taxímetro de miniatura. CLIC!"),
        F(Pai, "Vem comigo. E não repara a bagunça."));

    public static List<Fala> AulaGaragem() => L(
        F(Pai, "Bem-vindo à GARAGEM DO BETÃO!", Emote.Exclamacao, true),
        F(Pai, "Ferramentas, pneus, tinta... e a minha bicicleta. Que alguém arranhou, aliás."),
        F(Jason, "Nossa, que coisa, né? Quem será?", Emote.Gota),
        F(Pai, "E ali, filho... a estrela da casa. O meu NISSAN KICKS!"),
        F(Pai, "Câmera 360 pra estacionar, alerta de ponto cego, frenagem de emergência, central multimídia... É tecnologia que não acaba mais!"),
        F(Jason, "Pai, você tá chorando?", Emote.Gota),
        F(Pai, "É o cheiro de carro novo. Bate na emoção."),
        F(Pai, "Ele agora é seu também. E não é só ele: os meus amigos do ponto de táxi deixam uns carros guardados aqui."),
        F(Pai, "Pode escolher QUALQUER UM. E aquela prateleira tem tinta de todas as cores: pode pintar do jeito que quiser!"),
        F(Jason, "QUALQUER UM?! Pai, você é o melhor pai do mundo!", Emote.Coracao, true),
        F(Pai, "Eu sei. Agora presta atenção, que é igual autoescola:"),
        F(Pai, "Chegue perto do carro e aperte ENTER para abrir a garagem."),
        F(Pai, "SETAS para os lados trocam o carro. SETAS para cima e para baixo trocam a cor. ENTER confirma e ESC volta."));

    public static List<Fala> PaiEscolher() => L(
        F(Pai, "Chegue perto do carro e aperte ENTER. Escolha com o coração!"),
        F(Pai, "...E com o bolso também. Mas hoje é por minha conta."));

    public static List<Fala> DesistiuGaragem() => L(
        F(Pai, "Ficou na dúvida? Normal. Eu fiz três test drives antes de escolher o Kicks."),
        F(Pai, "Quando quiser, é só chegar perto do carro e apertar ENTER."));

    public static List<Fala> EscolheuKicks() => L(
        F(Pai, "O KICKS! Escolha de quem entende de carro!", Emote.Coracao, true),
        F(Pai, "Com o alerta de ponto cego e a frenagem de emergência, eu fico até mais tranquilo. Ele cuida de você."),
        F(Pai, "Mas lembra: tecnologia ajuda, quem dirige com cuidado é VOCÊ."),
        F(Jason, "Prometo, pai!"));

    public static List<Fala> PintouKicks() => L(
        F(Pai, "Você PINTOU o meu Kicks?!", Emote.Exclamacao, true),
        F(Pai, "..."),
        F(Pai, "Sabe que ficou estiloso? Tecnologia de ponta E personalidade. Gostei!", Emote.Coracao));

    public static List<Fala> EscolheuOutro() => L(
        F(Pai, "Trocou o Kicks por ESSE aí?!", Emote.Exclamacao, true),
        F(Pai, "O Kicks tem câmera 360, filho! CÂMERA. TRE-ZEN-TOS E SES-SEN-TA!", Emote.Gota),
        F(Jason, "Pai, é só pra ir no mercado...", Emote.Gota),
        F(Pai, "Tá bom, tá bom. Mas se mudar de ideia, ele tá aqui. Brilhando."));

    public static List<Fala> MissaoMercado() => L(
        F(Mae, "JAAAASON!", Emote.Exclamacao, true),
        F(Jason, "Que foi, mãe?!", Emote.Interrogacao),
        F(Mae, "Já que agora você é MOTORISTA... vai no mercado pra mim? Acabou o leite!"),
        F(Jason, "Claro! Primeira missão oficial do Mr. Driver!", Emote.Coracao),
        F(Mae, "Anota: leite, ovos, pão de forma e café. E NADA de pudim! Ainda tem pudim aqui!"),
        F(Pai, "O Mercado Bom Preço fica lá do outro lado da cidade. Você vai ter que atravessar tudo."),
        F(Pai, "Vai com calma e com SEGURANÇA. Cada batida tira pontos da sua carteira."),
        F(Pai, "Carteira suspensa = carro de volta pra garagem e você de volta pra bicicleta."),
        F(Jason, "A bicicleta arranhada?"),
        F(Pai, "...Essa mesma."),
        F(Pai, "Vou abrir o portão. Quando estiver pronto, é só sair com o carro por ali!"),
        Narrador("Você pode trocar de carro de novo antes de sair. Saia pelo portão da garagem quando quiser."));

    public static List<Fala> TrocouDeNovo() => L(
        F(Pai, "Mudou de novo? Você é igualzinho a sua mãe escolhendo roupa."),
        F(Pai, "...Não conta pra ela que eu disse isso."));

    public static List<Fala> PaiDepoisGaragem() => L(
        F(Pai, "O portão tá aberto, Mr. Driver! Leite, ovos, pão e café."),
        F(Pai, "E traz o troco! O troco é sagrado."));

    public static List<Fala> MaeAntesGaragem() => L(
        F(Mae, "Já liguei pra sua tia, pra sua avó e pro grupo da família. Todo mundo sabe que você passou!", Emote.Coracao),
        F(Mae, "O pudim tá na mesa. Só um pedaço! ...Tá bom, dois. Hoje é dia de festa."));

    public static List<Fala> MaeDepoisGaragem() => L(
        F(Mae, "Leite, ovos, pão de forma e café! Repete comigo!"),
        F(Jason, "Leite, ovos, pão de forma e café..."),
        F(Mae, "E NADA de pudim! E USA O CINTO!", Emote.Exclamacao, true));

    public static List<Fala> SaidaGaragem() => L(
        Narrador("Jason entrou no carro, ajustou o banco, os retrovisores e..."),
        F(Mae, "O CINTOOO!", Emote.Exclamacao, true),
        F(Jason, "TÔ DE CINTO, MÃE!", Emote.Exclamacao, true),
        F(Pai, "Vai com Deus, Mr. Driver! E cuida bem da máquina!"),
        Narrador("E lá foi o Jason, pela primeira vez sozinho, para as ruas da cidade..."));

    public static List<Fala> PortaFrenteGaragem() => L(
        F(Jason, "Pela porta da frente? Nada disso! Agora eu saio é DE CARRO, pela garagem!"));

    public static List<Fala> PortaGaragemTrancadaCap2() => L(
        F(Jason, "Trancada. O pai disse que tinha uma surpresa aqui dentro... Vou falar com ele."));

    public static List<Fala> PortaoFechadoCap2() => L(
        F(Jason, "O portão da garagem tá fechado. Primeiro vou ver o que o pai quer me mostrar."));

    // Capítulo 1: a garagem ainda é proibida
    public static List<Fala> PortaGaragemTrancadaCap1() => L(
        F(Jason, "A garagem do pai. Trancada, como sempre."),
        F(Jason, "Lá dentro tá o Nissan Kicks novo dele. Ele dá boa noite pro carro antes de dormir.", Emote.Gota));

    public static List<Fala> CarroCoberto() => L(
        F(Jason, "O Kicks do pai, debaixo da capa. Ainda tem cheiro de carro novo!"));

    // Objetos da casa no capítulo 2
    public static List<Fala> Cama2() => L(F(Jason, "Minha cama. Hoje eu durmo como MOTORISTA HABILITADO."));
    public static List<Fala> GuardaRoupa2() => L(F(Jason, "Moletom laranja: o uniforme oficial do Mr. Driver."));
    public static List<Fala> Computador2() => L(
        F(Jason, "O simulador de corrida... Tsc. Agora eu jogo é na vida real."),
        F(Jason, "...Mais tarde eu jogo uma partidinha. Só uma."));
    public static List<Fala> Estante2() => L(F(Jason, "Vou abrir espaço aqui pro troféu de melhor motorista da cidade."));
    public static List<Fala> Roupas2() => L(F(Jason, "A pilha de roupas cresceu enquanto eu tava fora. Ela tem vida própria."));
    public static List<Fala> Poster2() => L(F(Jason, "O carro dos meus sonhos. Agora falta só... o dinheiro."));
    public static List<Fala> JanelaQuarto2() => L(
        F(Jason, "Lá fora, a rua sem saída onde eu andava de bicicleta. Agora eu vou sair dela de carro!"));
    public static List<Fala> Geladeira2() => L(F(Jason, "Ímã novo na geladeira: \"PARABÉNS, MOTORISTA!\". A mãe é rápida."));
    public static List<Fala> Fogao2() => L(F(Jason, "O cheirinho de pudim vem daqui."));
    public static List<Fala> Pia2() => L(F(Jason, "A louça continua aqui. Tirar carteira não me livrou dela."));
    public static List<Fala> Fruteira2() => L(F(Jason, "Uma banana. Energia pra dirigir!"));
    public static List<Fala> Mesa2() => L(
        F(Jason, "Um pudim INTEIRO com uma vela em cima. Escrito com calda: \"PARABÉNS, MR. DRIVER\"."),
        F(Jason, "Só um pedacinho... (nhac)", Emote.Coracao),
        F(Jason, "...Tá bom, dois."));
    public static List<Fala> TV2() => L(
        Narrador("No jornal: \"Jovem passa no exame de direção e instrutor é visto deitado no sofá da autoescola.\""),
        F(Jason, "...Pelo menos não falaram o meu nome.", Emote.Gota));
    public static List<Fala> Sofa2() => L(F(Jason, "A marca do pai no sofá continua perfeita. Isso é dedicação."));
    public static List<Fala> Planta2() => L(F(Jason, "A Gertrudes ganhou um lacinho. Até a samambaia tá comemorando."));
    public static List<Fala> Relogio2() => L(F(Jason, "Quase meio-dia. O dia mais longo da minha vida!"));
    public static List<Fala> QuadroTaxi2() => L(F(Jason, "O pai e o Kicks. Um dia vai ter uma foto minha aqui do lado!"));
    public static List<Fala> Sapateira2() => L(F(Jason, "Meu tênis de dirigir. É igual ao de sempre, mas agora é de dirigir."));

    // Garagem
    public static List<Fala> Bancada() => L(
        F(Jason, "A bancada do pai. Cada ferramenta tem o contorno desenhado na parede."),
        F(Jason, "Menos a chave de fenda. O contorno tá vazio desde 2009."));
    public static List<Fala> Tintas() => L(F(Jason, "Latas de tinta de TODAS as cores. Dá pra pintar o carro do jeito que eu quiser!"));
    public static List<Fala> Pneus() => L(F(Jason, "Pneus velhos do táxi antigo do pai. Ele não joga fora: diz que têm valor sentimental."));
    public static List<Fala> Tambor() => L(F(Jason, "Um tambor de óleo. Cheiro de oficina!"));
    public static List<Fala> Bicicleta() => L(
        F(Jason, "A bicicleta do pai. E o arranhão..."),
        F(Jason, "...Melhor não ficar olhando muito pra ele.", Emote.Gota));
    public static List<Fala> CaixaFerramentas() => L(F(Jason, "Uma caixa de ferramentas com um adesivo: \"NÃO MEXA. Ass.: Betão\"."));
    public static List<Fala> CalendarioGaragem() => L(F(Jason, "Um calendário com o dia em que o Kicks chegou marcado com um coração."));
    public static List<Fala> PlacaVagaGaragem() => L(
        F(Jason, "Uma placa: \"VAGA DO KICKS\". Embaixo, escrito a caneta: \"e do Jason\".", Emote.Coracao));
    public static List<Fala> JanelaGaragem() => L(F(Jason, "Dá pra ver a rua sem saída daqui. E o vizinho regando as plantas de pijama."));

    // ======================================================================
    // Nivel1: ir ao mercado
    // ======================================================================

    public static List<Fala> AberturaMercado() => L(
        Narrador("A rua sem saída da família Driver. Primeira missão oficial: o Mercado Bom Preço, do outro lado da cidade."),
        F(Jason, "Tá. Respira. Cinto, retrovisor, seta... Igualzinho o seu Gervásio ensinou."),
        Narrador("TRIM TRIM! O celular tocou no viva-voz."),
        F(Mae, "Jason! Esqueci de pedir: compra fermento também! E vai devagar! E me liga quando chegar!"),
        F(Pai, "E nada de pressa, filho! A cidade não vai fugir. O mercado também não."),
        F(Jason, "Leite, ovos, pão de forma, café e fermento. Pode deixar!"),
        Narrador("Siga a seta amarela e o radar até o mercado. Cada batida tira pontos da carteira: dirija com segurança!"));

    public static List<Fala> ReacoesBatidaMercado() => L(
        F(Jason, "Foi mal! Foi mal!", Emote.Gota),
        F(Jason, "A mãe NÃO pode saber disso...", Emote.Gota),
        F(Jason, "Calma, Jason. Respira. Igual o Gervásio: um, dois, três...", Emote.Gota),
        F(Jason, "Ai, a minha carteira!", Emote.Exclamacao),
        F(Jason, "Desculpa, moço!", Emote.Gota));

    public static List<Fala> ChegadaMercadoPerfeita() => L(
        F(Jason, "Cheguei! Mercado Bom Preço!", Emote.Coracao, true),
        F(Jason, "E sem NENHUM arranhão! O seu Gervásio ia chorar de orgulho.", Emote.Coracao),
        Narrador("Agora é só fazer as compras... e não comprar pudim."));

    public static List<Fala> ChegadaMercado() => L(
        F(Jason, "Cheguei! Mercado Bom Preço!", Emote.Coracao),
        F(Jason, "Com uns arranhõezinhos... mas cheguei! A mãe não precisa saber dos detalhes.", Emote.Gota),
        Narrador("Agora é só fazer as compras... e não comprar pudim."));

    // ---------- Volta para casa, com os rebeldes nas ruas ----------

    public static List<Fala> AberturaVolta() => L(
        Narrador("Do lado de fora do mercado, o ronco de motores barulhentos ecoa pela cidade..."),
        F(Jason, "Os rebeldes da Gangue do Escapamento tão por aí. Carro pichado, moto, contramão..."),
        Narrador("TRIM TRIM!"),
        F(Pai, "Jason! A Jussara do mercado me ligou. Tem uns baderneiros fazendo racha na cidade!"),
        F(Pai, "Eles não respeitam NADA: nem semáforo, nem faixa, nem placa. Fique longe deles e venha direto pra casa!"),
        F(Mae, "E USA O CINTO! E LIGA O PISCA! E NÃO CORRE!", Emote.Exclamacao, true),
        F(Jason, "Pode deixar. Eles fazem tudo errado... e eu vou fazer tudo CERTO."),
        Narrador("Volte para casa, na rua sem saída. Os rebeldes aparecem em vermelho no radar: desvie deles!"));

    public static List<Fala> ReacoesRebelde() => L(
        F(Jason, "Ei! Isso é CONTRAMÃO!", Emote.Exclamacao),
        F(Jason, "Que barbeiragem! Cadê a seta?!", Emote.Exclamacao),
        F(Jason, "Calma, Jason. Não entra na pilha deles. Respira...", Emote.Gota),
        F(Jason, "Quem te deu carteira, hein?!", Emote.Exclamacao),
        F(Jason, "Eles passaram no vermelho! De novo!", Emote.Gota));

    public static List<Fala> VoltouParaCasaPerfeito() => L(
        F(Jason, "Cheguei em casa! E sem NENHUM arranhão, mesmo com aqueles rebeldes na rua!", Emote.Coracao, true),
        F(Pai, "ESSE É O MEU FILHO! Dirigiu certinho no meio da bagunça!", Emote.Coracao, true),
        F(Mae, "Trouxe o leite? Os ovos? O pão? O café? O fermento?"),
        F(Jason, "Tudo! E nenhum pudim.", Emote.Coracao),
        F(Mae, "...Que pena. Eu ia te deixar comer um pedaço.", Emote.Gota),
        Narrador("Mas a Gangue do Escapamento continua solta pela cidade... Essa história ainda não acabou."));

    public static List<Fala> VoltouParaCasa() => L(
        F(Jason, "Ufa... cheguei em casa. Com uns arranhões, mas cheguei!", Emote.Gota),
        F(Pai, "O importante é que você tá bem, filho. Aqueles rebeldes são um perigo."),
        F(Mae, "Trouxe o leite? Os ovos? O pão? O café? O fermento?"),
        F(Jason, "Tudo, mãe! E nenhum pudim."),
        Narrador("Mas a Gangue do Escapamento continua solta pela cidade... Essa história ainda não acabou."));
    // ======================================================================
    // Dentro do mercado (Historia_Mercado)
    // ======================================================================

    public static List<Fala> EntradaMercado() => L(
        Narrador("Mercado Bom Preço. Ar condicionado, musiquinha ambiente e cheiro de pão quentinho."),
        F(Jason, "Bom, a lista: leite, ovos, pão de forma, café e fermento."),
        F(Jason, "Pego tudo, passo no caixa e volto pra casa. Moleza!"),
        Narrador("Ande pelos corredores e examine as prateleiras com ENTER para pegar as compras."));

    public static string ObjetivoCompras => "Pegue as compras ({n} de {total}): {lista}";
    public static string ObjetivoCaixa => "Vá até o caixa e pague as compras";
    public static string ObjetivoSairMercado => "Saia do mercado e volte para o carro";

    public static List<Fala> PegouLeite() => L(F(Jason, "Leite integral. A mãe só toma esse.", Emote.Coracao));
    public static List<Fala> PegouOvos() => L(F(Jason, "Uma dúzia de ovos. Vou carregar com MUITO cuidado. Igual dirigir."));
    public static List<Fala> PegouPao() => L(F(Jason, "Pão de forma! Ainda tá quentinho. Será que eu como um só...? Não. Foco."));
    public static List<Fala> PegouCafe() => L(F(Jason, "Café. Sem ele o pai não funciona de manhã."));
    public static List<Fala> PegouFermento() => L(F(Jason, "Fermento! Quase que eu esqueço. A mãe vai fazer bolo!", Emote.Coracao));
    public static List<Fala> JaPegou() => L(F(Jason, "Esse eu já peguei."));

    public static List<Fala> Pudim() => L(
        F(Jason, "PUDIM EM OFERTA... leve 2, pague 1...", Emote.Coracao),
        F(Jason, "NÃO. A mãe disse NADA de pudim. Seja forte, Jason.", Emote.Gota));

    public static List<Fala> CaixaAntes() => L(
        F(Atendente, "Opa! Ainda tá faltando coisa da lista, né? Eu vejo no seu olhar.", Emote.Interrogacao),
        F(Atendente, "Pode ir, que eu espero. *ploc* (bola de chiclete)"));

    public static List<Fala> Caixa() => L(
        F(Atendente, "Boa tarde! Bem-vindo ao Bom Preço! Eu sou a Jussara. *ploc*"),
        F(Atendente, "Leite, ovos, pão de forma, café, fermento... Hmm, a Dona Cida vai fazer bolo, né?"),
        F(Jason, "Como você sabe que eu sou filho da Dona Cida?!", Emote.Exclamacao),
        F(Atendente, "Querido, eu sei de TUDO nessa cidade. Você é o Jason, que passou no exame hoje. O Gervásio ainda tá deitado."),
        F(Atendente, "E aquele carro lá fora é seu? Que lindo! Eu AMO carro."),
        F(Atendente, "Ó, dica de amiga: meu primo Tonhão vai abrir uma loja de peças ali na avenida. A TURBO PEÇAS!"),
        F(Atendente, "Pneu melhor, freio melhor, suspensão... Tudo pra deixar seu carro mais seguro e mais esperto."),
        F(Atendente, "Ainda tá em reforma, mas logo abre. Guarda suas moedas, hein?"),
        F(Jason, "Turbo Peças... Vou lembrar!", Emote.Coracao),
        F(Atendente, "Deu tudo vinte e três e cinquenta. Quer sacolinha?"));

    public static List<Fala> ChegadaRebeldes() => L(
        Narrador("VRUUUM! VRUM VRUM! CRASH!"),
        F(Atendente, "Ah, não... De novo NÃO!", Emote.Exclamacao, true),
        F(Jason, "Que barulho foi esse?!", Emote.Interrogacao));

    public static List<Fala> Baderna() => L(
        F(Rebelde, "E AÍ, BOM PREÇO! A GANGUE DO ESCAPAMENTO CHEGOU!", Emote.Exclamacao, true),
        F(Atendente, "TURBO! Já falei que é proibido entrar de skate, de patins E DE MOTO no mercado!", Emote.Exclamacao, true),
        F(Rebelde, "Relaxa, Jussara! A gente só veio buscar refrigerante... e derrubar umas latinhas.", Emote.Coracao),
        F(Rebelde, "Opa! Quem é esse aí? Cara de carteira nova!", Emote.Interrogacao),
        F(Jason, "Eu sou o Jason. E vocês deixaram as motos em cima da calçada, na vaga para pessoas com deficiência e na faixa de pedestre!"),
        F(Rebelde, "HAHAHA! Placa? Faixa? Semáforo? A gente não liga pra nada disso!", Emote.Exclamacao),
        F(Jason, "As leis de trânsito existem pra proteger todo mundo. Inclusive vocês.", Emote.Exclamacao),
        F(Rebelde, "Ih, olha o certinho! Quero ver você chegar em casa inteiro, novato. A cidade agora é NOSSA!"),
        F(Rebelde, "GALERA! PRA RUA! VRUUUUM!", Emote.Exclamacao, true));

    public static List<Fala> DepoisBaderna() => L(
        F(Atendente, "Aff... Desculpa a bagunça, Jason. Esses são os rebeldes da Gangue do Escapamento."),
        F(Atendente, "Andam de carro pichado e de moto, sem respeitar NENHUMA lei de trânsito. Racha, contramão, sinal vermelho..."),
        F(Atendente, "Toma cuidado na volta pra casa, viu? Eles vão estar pelas ruas."),
        F(Jason, "Pode deixar, Jussara. Eles fazem tudo errado... e eu vou fazer tudo certo.", Emote.Exclamacao),
        F(Atendente, "Assim que eu gosto! E lembra da Turbo Peças! *ploc*", Emote.Coracao));

    public static List<Fala> AtendenteDepois() => L(
        F(Atendente, "Cuidado com a Gangue do Escapamento lá fora! E volta sempre! *ploc*"));

    public static List<Fala> PortaMercadoAntes() => L(F(Jason, "Ainda não! Primeiro as compras, depois o caixa."));

    public static List<Fala> SaidaMercado() => L(
        F(Jason, "Compras no carro. Agora é voltar pra casa com MUITA atenção."));

    // Prateleiras e coisas do mercado
    public static List<Fala> GeladeiraFrios() => L(F(Jason, "Iogurtes, queijos e requeijão. Tudo gelado e organizado por validade."));
    public static List<Fala> Hortifruti() => L(F(Jason, "Frutas fresquinhas! Tem banana, maçã, laranja... e um abacaxi com cara de bravo."));
    public static List<Fala> Bebidas() => L(F(Jason, "A geladeira de refrigerantes. Ela faz um barulhinho estranho: BRRRMMM."));
    public static List<Fala> Limpeza() => L(F(Jason, "Corredor de limpeza. Cheiro de lavanda que dá pra sentir de longe."));
    public static List<Fala> CaixaFechado() => L(F(Jason, "\"CAIXA FECHADO\". Tem um gato dormindo em cima da esteira."));
    public static List<Fala> Latas() => L(F(Jason, "Uma pirâmide perfeita de latas de milho. Alguém passou HORAS montando isso."));
    public static List<Fala> LatasDerrubadas() => L(F(Jason, "A pirâmide de latas... destruída. Coitado de quem montou."));
    public static List<Fala> Carrinhos() => L(F(Jason, "Carrinhos de compras. Um deles tem a rodinha torta, como sempre."));
    public static List<Fala> AvisoMercado() => L(F(Jason, "Um cartaz: \"PROIBIDO ENTRAR DE MOTO\". Por que alguém precisaria desse aviso?"));
}
