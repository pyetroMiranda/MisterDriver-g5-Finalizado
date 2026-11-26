// Nomes das cenas da história e o progresso dela.
// A história é: Historia_Casa (Jason acorda e fala com os pais) → Historia_Autoescola (aulas no pátio com cones)
// → Historia_Exame (exame final pelas ruas da cidade) → Historia_Garagem (de volta para casa com a carteira:
// o pai mostra a garagem, o Jason escolhe o carro e a mãe pede para ir ao mercado) → Nivel1 (ir ao mercado).
// A cena Tutorial é só o tutorial (botão TUTORIAL do menu).
public static class ProgressoHistoria
{
    public const string CenaMenu = "Menu";
    public const string CenaTutorial = "Tutorial";
    public const string CenaVideogame = "Historia_Videogame";   // o TURBO RACER 3000 que o Jason joga antes de tudo
    public const string CenaCasa = "Historia_Casa";
    public const string CenaAutoescola = "Historia_Autoescola";
    public const string CenaExame = "Historia_Exame";
    public const string CenaGaragem = "Historia_Garagem";
    public const string CenaMercado = "Historia_Mercado";   // dentro do Nivel1: as compras e a Gangue do Escapamento
    public const string PrimeiroNivel = "Nivel1";

    // O jogador já tirou a carteira (terminou o exame)?
    public static bool CarteiraTirada => ProgressoNiveis.Concluido(CenaExame);

    // Terminou a parte da história antes do primeiro nível (voltou para casa e escolheu o carro)?
    public static bool HistoriaConcluida => ProgressoNiveis.Concluido(CenaGaragem);

    public static void ConcluirExame() => ProgressoNiveis.Concluir(CenaExame);

    // Usado pelas ferramentas de teste: marca todos os capítulos como jogados
    public static void ConcluirTudo()
    {
        ProgressoNiveis.Concluir(CenaCasa);
        ProgressoNiveis.Concluir(CenaAutoescola);
        ProgressoNiveis.Concluir(CenaExame);
        ProgressoNiveis.Concluir(CenaGaragem);
    }

    // Capítulo onde o jogador parou (o botão JOGAR continua dali)
    public static string ProximaCena()
    {
        if (HistoriaConcluida) return PrimeiroNivel;
        if (CarteiraTirada) return CenaGaragem;
        if (ProgressoNiveis.Concluido(CenaAutoescola)) return CenaExame;
        if (ProgressoNiveis.Concluido(CenaCasa)) return CenaAutoescola;
        return CenaVideogame;   // a história começa no videogame do Jason (e a casa vem depois)
    }

    public static bool Comecou => ProgressoNiveis.Concluido(CenaCasa);

    // Usado pelas ferramentas de teste (menu Mr Driver > História)
    public static void Apagar()
    {
        ProgressoNiveis.Desfazer(CenaCasa);
        ProgressoNiveis.Desfazer(CenaAutoescola);
        ProgressoNiveis.Desfazer(CenaExame);
        ProgressoNiveis.Desfazer(CenaGaragem);
        ProgressoNiveis.Desfazer(CenaMercado);
        MissaoMercado.VoltandoDoMercado = false;
        GaragemJogador.Apagar();
    }
}
