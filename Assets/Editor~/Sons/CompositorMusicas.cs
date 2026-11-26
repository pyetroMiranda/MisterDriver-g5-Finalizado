using System;
using System.Collections.Generic;
using static SintetizadorSons;

// Músicas do jogo em estilo chiptune, compostas e "tocadas" por código e salvas como .wav.
// Cada música tem: tom e escala, andamento, um acorde por compasso, a melodia (graus da escala),
// e estilos de baixo, arpejo, pad e bateria. As de nível são loops sem emenda; as vinhetas tocam uma vez.
//
// Melodia: "grau:duração" separados por espaço. Duração em semicolcheias (16 = um compasso).
//   grau 1..7 da escala; ' sobe uma oitava, , desce uma oitava; # e b alteram meio tom; . = pausa.
// Acorde: grau da escala (tríade da própria escala); "5M" = acorde maior (dominante na escala menor); "-" = sem acorde.
public static class CompositorMusicas
{
    public class Musica
    {
        public string id;
        public int bpm = 120;
        public int raiz = 60;        // MIDI do grau 1 da melodia
        public bool menor;
        public string[] acordes;
        public string melodia;
        public int baixo = 1, bateria = 1;
        public int arpejo;           // 0 = sem, 2 = colcheias, 1 = semicolcheias
        public bool pad;
        public Onda timbre = Onda.Quadrada;
        public double duty = 0.5, volumeMelodia = 0.22, vibrato = 0.012, eco = 0.25;
        public bool loop = true;
        public float volume = 0.6f;  // volume sugerido no banco
        public double chiado;        // ruído de fundo (TV velha)
        public int niveis;           // 0 = normal; > 0 = poucos níveis de volume (videogame de 4 bits)
    }

    public static List<Musica> Todas()
    {
        return new List<Musica>
        {
            // Casa da família Driver (e a garagem): calma e aconchegante
            new Musica
            {
                id = "MusicaCasa", bpm = 92, raiz = 65, baixo = 1, bateria = 1, arpejo = 2, pad = true,
                duty = 0.5, volumeMelodia = 0.2, volume = 0.5f,
                acordes = new[] { "1", "6", "4", "5", "1", "6", "2", "5", "4", "5", "3", "6", "2", "5", "1", "1" },
                melodia =
                    "3:4 2:2 1:2 5,:4 1:4 | 6,:4 1:2 3:2 2:8 | 4:4 3:2 2:2 1:4 2:4 | 3:6 2:2 5,:8 | " +
                    "3:4 2:2 1:2 5,:4 1:4 | 6,:4 1:2 3:2 5:8 | 4:4 3:2 4:2 5:4 6:4 | 5:12 .:4 | " +
                    "6:4 5:2 4:2 4:4 2:4 | 5:4 4:2 3:2 3:4 1:4 | 3:4 2:2 3:2 5:8 | 6:6 5:2 4:4 3:4 | " +
                    "4:4 2:2 4:2 6:4 5:4 | 5:4 2:2 3:2 4:4 7,:4 | 1:8 3:4 5:4 | 1':8 .:8",
            },
            // Autoescola: animada, de "primeira aula"
            new Musica
            {
                id = "MusicaAutoescola", bpm = 120, raiz = 67, baixo = 2, bateria = 2, arpejo = 2,
                duty = 0.25, volumeMelodia = 0.2, volume = 0.5f,
                acordes = new[] { "1", "4", "5", "1", "6", "4", "5", "5", "1", "4", "5", "6", "4", "5", "1", "1" },
                melodia =
                    "1:2 3:2 5:2 3:2 1':4 5:4 | 4:2 6:2 1':2 6:2 4:4 .:4 | 5:2 4:2 3:2 2:2 7,:4 5,:4 | 1:6 .:2 5,:2 6,:2 7,:4 | " +
                    "3:2 5:2 7:2 5:2 3:4 2:2 1:2 | 6,:2 1:2 4:2 6:2 5:4 4:4 | 3:4 2:2 3:2 5:4 4:2 3:2 | 2:8 .:4 5,:4 | " +
                    "1:2 3:2 5:2 3:2 1':4 5:4 | 6:2 5:2 4:2 6:2 1':4 6:4 | 5:2 6:2 5:2 4:2 3:4 2:4 | 3:4 5:4 7:4 5:4 | " +
                    "6:4 1':4 6:2 5:2 4:4 | 5:4 4:2 3:2 2:4 7,:4 | 1:4 3:4 5:4 1':4 | 1':6 .:10",
            },
            // Exame: concentrada e um pouco tensa (relógio tiquetaqueando)
            new Musica
            {
                id = "MusicaExame", bpm = 112, raiz = 62, menor = true, baixo = 4, bateria = 4, arpejo = 1, pad = true,
                duty = 0.25, volumeMelodia = 0.18, volume = 0.5f,
                acordes = new[] { "1", "1", "6", "7", "1", "1", "4", "5M", "6", "7", "1", "1", "4", "5M", "1", "5M" },
                melodia =
                    "1:3 1:1 5:4 4:2 3:2 2:4 | 1:3 1:1 5:4 6:4 5:4 | 6:6 5:2 4:4 3:4 | 7,:6 1:2 2:4 3:4 | " +
                    "1:3 1:1 5:4 4:2 3:2 2:4 | 1:3 1:1 5:4 1':4 7:4 | 6:4 4:4 1':4 6:4 | 5:4 #7:4 2':4 #7:4 | " +
                    "6:4 5:2 6:2 1':4 6:4 | 7:4 6:2 7:2 2':4 7:4 | 1':6 7:2 6:4 5:4 | 4:4 3:4 2:4 1:4 | " +
                    "4:3 4:1 6:4 1':4 6:4 | 5:3 5:1 #7:4 2':4 #7:4 | 1':8 5:8 | 5:4 #7:4 2':4 #7:4",
            },
            // Nivel1, ida ao mercado: pop animado de dirigir pela cidade
            new Musica
            {
                id = "MusicaCidade", bpm = 128, raiz = 72, baixo = 2, bateria = 2, arpejo = 1,
                duty = 0.5, volumeMelodia = 0.18, volume = 0.5f,
                acordes = new[] { "1", "5", "6", "4", "1", "5", "6", "4", "4", "5", "3", "6", "2", "4", "5", "5" },
                melodia =
                    "5:2 5:2 6:2 5:2 3:4 1:4 | 2:2 2:2 3:2 2:2 7,:4 5,:4 | 1:2 1:2 2:2 3:2 6:4 5:4 | 4:2 3:2 4:2 6:2 5:8 | " +
                    "5:2 5:2 6:2 5:2 1':4 7:2 6:2 | 5:2 4:2 3:2 2:2 5:4 2:4 | 3:2 2:2 1:2 6,:2 1:4 3:4 | 2:6 1:2 1:8 | " +
                    "6:2 6:2 1':2 6:2 5:4 4:4 | 5:2 5:2 7:2 5:2 4:4 2:4 | 3:2 3:2 5:2 3:2 7:4 5:4 | 6:2 5:2 3:2 1:2 3:8 | " +
                    "4:2 4:2 6:2 4:2 2:4 4:4 | 6:2 6:2 1':2 6:2 4:4 6:4 | 5:4 7:4 2':4 7:4 | 5:8 .:8",
            },
            // Volta para casa com a Gangue do Escapamento nas ruas (e a baderna no mercado): perseguição
            new Musica
            {
                id = "MusicaPerseguicao", bpm = 150, raiz = 64, menor = true, baixo = 3, bateria = 3, arpejo = 1,
                timbre = Onda.Serra, volumeMelodia = 0.16, eco = 0.15, volume = 0.5f,
                acordes = new[] { "1", "6", "3", "7", "1", "6", "3", "7", "4", "1", "7", "1", "6", "7", "5M", "5M" },
                melodia =
                    "1:2 1:2 5:2 1:2 6:2 1:2 5:2 3:2 | 1:2 1:2 6,:2 1:2 3:4 2:2 1:2 | 3:2 3:2 5:2 3:2 1':4 7:2 5:2 | 7:2 6:2 7:2 6:2 4:4 2:4 | " +
                    "1:2 1:2 5:2 1:2 6:2 1:2 5:2 3:2 | 1:2 1:2 6,:2 1:2 3:4 5:2 6:2 | 7:2 1':2 7:2 5:2 3:4 5:4 | 2:4 4:4 6:4 4:4 | " +
                    "4:2 4:2 6:2 4:2 1':4 6:4 | 5:2 5:2 3:2 5:2 1':4 5:4 | 7,:2 2:2 4:2 7,:2 2:4 4:4 | 1:4 3:4 5:4 1':4 | " +
                    "6:2 5:2 6:2 1':2 3':4 1':4 | 7:2 6:2 7:2 2':2 4':4 2':4 | 5:2 5:2 #7:2 5:2 2':4 #7:4 | 5:4 #7:4 2':4 #7:4",
            },
            // Mercado Bom Preço: música de mercado, gingada (bossa)
            new Musica
            {
                id = "MusicaMercado", bpm = 100, raiz = 69, baixo = 5, bateria = 5, arpejo = 0, pad = true,
                timbre = Onda.Triangulo, volumeMelodia = 0.3, vibrato = 0.02, volume = 0.5f,
                acordes = new[] { "1", "6", "2", "5", "1", "6", "2", "5", "4", "3", "6", "2", "2", "5", "1", "1" },
                melodia =
                    "3:3 2:1 3:4 5:6 .:2 | 6:3 5:1 3:4 1:6 .:2 | 2:3 1:1 2:4 4:6 .:2 | 5:3 4:1 3:4 2:8 | " +
                    "3:3 2:1 3:4 5:4 1':4 | 1':3 7:1 6:4 5:4 3:4 | 4:3 3:1 2:4 6,:4 1:4 | 2:12 .:4 | " +
                    "4:3 5:1 6:4 1':4 6:4 | 5:3 4:1 3:4 5:8 | 6:3 5:1 6:4 1':4 3':4 | 2':6 1':2 6:8 | " +
                    "4:3 3:1 4:4 6:4 4:4 | 5:3 #4:1 5:4 7:4 2':4 | 1':6 5:2 3:8 | 1:8 .:8",
            },
            // TURBO RACER 3000: um canal só, sem baixo nem bateria, com chiado de TV velha
            new Musica
            {
                id = "MusicaTurboRacer", bpm = 150, raiz = 72, baixo = 0, bateria = 0, arpejo = 0, pad = false,
                duty = 0.5, volumeMelodia = 0.3, vibrato = 0, eco = 0, chiado = 0.025, niveis = 6, volume = 0.22f,
                acordes = new[] { "1", "6", "4", "5", "1", "6", "4", "5" },
                melodia =
                    "1:1 3:1 5:1 1':1 5:1 3:1 1:1 3:1 5:1 1':1 3':1 1':1 5:2 3:2 | " +
                    "6,:1 1:1 3:1 6:1 3:1 1:1 6,:1 1:1 3:1 6:1 1':1 6:1 3:2 1:2 | " +
                    "4,:1 6,:1 1:1 4:1 1:1 6,:1 4,:1 6,:1 1:1 4:1 6:1 4:1 1:2 6,:2 | " +
                    "5,:1 7,:1 2:1 5:1 2:1 7,:1 5,:1 7,:1 2:1 5:1 7:1 5:1 2:2 7,:2 | " +
                    "5:2 5:2 6:2 5:2 1':4 7:4 | 6:2 6:2 5:2 3:2 1:4 3:4 | 4:2 4:2 6:2 1':2 7:4 6:4 | 5:2 4:2 3:2 2:2 7,:8",
            },
            // Vinhetas (tocam uma vez)
            new Musica
            {
                id = "VinhetaVitoria", bpm = 140, raiz = 72, baixo = 6, bateria = 6, arpejo = 1, loop = false,
                duty = 0.5, volumeMelodia = 0.22, eco = 0.2, volume = 0.65f,
                acordes = new[] { "1", "1" },
                melodia = "1:2 3:2 5:2 1':4 3':2 5':4 | 4':2 3':2 2':2 3':10",
            },
            new Musica
            {
                id = "VinhetaCarteira", bpm = 120, raiz = 67, baixo = 6, bateria = 6, arpejo = 1, pad = true, loop = false,
                duty = 0.5, volumeMelodia = 0.22, eco = 0.25, volume = 0.65f,
                acordes = new[] { "1", "4", "5", "1" },
                melodia = "5,:2 1:2 3:2 5:2 3:2 5:2 1':4 | 6:4 5:2 4:2 6:4 1':4 | 2':4 7:2 5:2 7:4 2':4 | 1':12 .:4",
            },
            new Musica
            {
                id = "VinhetaDerrota", bpm = 80, raiz = 64, menor = true, baixo = 0, bateria = 0, arpejo = 0, loop = false,
                timbre = Onda.Serra, volumeMelodia = 0.3, vibrato = 0.03, eco = 0.1, volume = 0.6f,
                acordes = new[] { "-", "-" },
                melodia = "5:4 b5:4 4:4 b4:4 | b4:12 .:4",
            },
        };
    }

    // ======================================================================
    // Tocar
    // ======================================================================

    private static readonly int[] Maior = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] Menor = { 0, 2, 3, 5, 7, 8, 10 };

    public static float[] Renderizar(Musica m)
    {
        double passo = 60.0 / m.bpm / 4.0;   // uma semicolcheia
        int compassos = m.acordes.Length;
        double total = compassos * 16 * passo;
        double cauda = m.loop ? 2.0 : 2.5;
        float[] b = Novo(total + cauda);

        float[] melodia = Novo(total + cauda);
        TocarMelodia(m, melodia, passo);
        if (m.eco > 0) Eco(melodia, passo * 3, 0.3, m.eco);
        Somar(b, melodia, 1);

        for (int c = 0; c < compassos; c++)
        {
            double t0 = c * 16 * passo;
            int[] acorde = Acorde(m, m.acordes[c]);
            if (acorde == null) continue;
            Baixo(m, b, t0, passo, acorde);
            if (m.arpejo > 0) Arpejo(m, b, t0, passo, acorde);
            if (m.pad) Pad(b, t0, passo, acorde);
        }
        for (int c = 0; c < compassos; c++) Bateria(m, b, c, c * 16 * passo, passo, compassos);

        if (m.chiado > 0) Ruido(b, 0, total + cauda, m.chiado, 0, 0, 0, null, 2000, 999);

        // Loop sem emenda: a cauda (ecos, notas soando) volta para o começo
        int n = (int)(total * Taxa);
        if (m.loop)
        {
            for (int i = n; i < b.Length; i++) b[i - n] += b[i];
            Array.Resize(ref b, n);
        }

        Saturar(b, 1.2);
        if (m.niveis > 0) Quantizar(b, m.niveis);
        return Fim(b, 0.85);
    }

    // Soma das durações da melodia (para conferir se bate com os compassos)
    public static int DuracaoMelodia(Musica m)
    {
        int s = 0;
        foreach (string tk in Tokens(m.melodia)) s += int.Parse(tk.Substring(tk.IndexOf(':') + 1));
        return s;
    }

    private static IEnumerable<string> Tokens(string melodia)
    {
        foreach (string tk in melodia.Replace("|", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) yield return tk;
    }

    private static void TocarMelodia(Musica m, float[] b, double passo)
    {
        int pos = 0;
        foreach (string tk in Tokens(m.melodia))
        {
            int dp = tk.IndexOf(':');
            string nota = tk.Substring(0, dp);
            int dur = int.Parse(tk.Substring(dp + 1));
            if (nota != ".")
            {
                double f = Freq(Midi(m, nota, m.raiz));
                double d = dur * passo;
                Tom(b, pos * passo, d * 0.95, t => f, m.timbre, m.volumeMelodia, ataque: 0.008, soltura: Math.Min(0.06, d * 0.3),
                    decai: m.timbre == Onda.Triangulo ? 1.2 : 0.8, duty: m.duty, vib: d > 0.25 ? m.vibrato : 0, vibHz: 5.5, vibAtraso: 0.12);
            }
            pos += dur;
        }
    }

    private static int Midi(Musica m, string nota, int raiz)
    {
        int alter = 0, oitava = 0, grau = 1;
        foreach (char c in nota)
        {
            if (c == '#') alter++;
            else if (c == 'b') alter--;
            else if (c == '\'') oitava++;
            else if (c == ',') oitava--;
            else if (char.IsDigit(c)) grau = c - '0';
        }
        int[] escala = m.menor ? Menor : Maior;
        return raiz + escala[(grau - 1) % 7] + 12 * oitava + alter;
    }

    private static double Freq(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

    // Tríade do grau (em semitons acima da raiz da música); "5M" força a terça maior
    private static int[] Acorde(Musica m, string s)
    {
        if (s == "-") return null;
        bool maior = s.EndsWith("M");
        int grau = int.Parse(s.TrimEnd('M'));
        int[] escala = m.menor ? Menor : Maior;
        int[] r = new int[3];
        for (int k = 0; k < 3; k++)
        {
            int idx = grau - 1 + k * 2;
            r[k] = escala[idx % 7] + 12 * (idx / 7);
        }
        if (maior) r[1] = r[0] + 4;
        return r;
    }

    private static void Baixo(Musica m, float[] b, double t0, double passo, int[] a)
    {
        int raiz = m.raiz - 24;
        void N(int passoIni, int dur, int semitom, double vol = 0.26)
        {
            double f = Freq(raiz + semitom);
            Tom(b, t0 + passoIni * passo, dur * passo * 0.92, t => f, Onda.Triangulo, vol, ataque: 0.005, soltura: 0.03);
        }
        int r = a[0], q = a[2];
        switch (m.baixo)
        {
            case 1: N(0, 8, r); N(8, 8, q - 12 < r ? q : q - 12); break;                         // calmo
            case 2: for (int i = 0; i < 8; i++) N(i * 2, 2, i % 2 == 0 ? r : r + 12, 0.22); break; // pop (oitavas)
            case 3: for (int i = 0; i < 8; i++) N(i * 2, 2, i == 6 ? q : r, 0.24); break;          // perseguição
            case 4: N(0, 4, r); N(6, 2, r); N(8, 4, q); N(12, 4, r); break;                         // tenso
            case 5: N(0, 6, r); N(6, 2, q); N(8, 6, q); N(14, 2, r); break;                         // bossa
            case 6: N(0, 16, r, 0.28); break;                                                       // vinheta
        }
    }

    private static void Arpejo(Musica m, float[] b, double t0, double passo, int[] a)
    {
        int raiz = m.raiz - 12;
        int cada = m.arpejo == 1 ? 1 : 2;
        int[] ordem = { 0, 1, 2, 1 };
        for (int s = 0, k = 0; s < 16; s += cada, k++)
        {
            double f = Freq(raiz + a[ordem[k % 4]] + (k % 8 >= 4 ? 12 : 0));
            Tom(b, t0 + s * passo, cada * passo * 0.8, t => f, Onda.Quadrada, 0.06, ataque: 0.002, soltura: 0.02, decai: 6, duty: 0.125);
        }
    }

    private static void Pad(float[] b, double t0, double passo, int[] a)
    {
        foreach (int n in a)
        {
            double f = Freq(48 + 12 + n);
            Tom(b, t0, 16 * passo, t => f, Onda.Quadrada, 0.035, ataque: 0.25, soltura: 0.25, duty: 0.5, vib: 0.004, vibHz: 4);
        }
    }

    // ---------- Bateria ----------

    private static void Bateria(Musica m, float[] b, int compasso, double t0, double passo, int compassos)
    {
        void Em(int s, Action<double> tocar) => tocar(t0 + s * passo);
        int semente = compasso * 97;
        switch (m.bateria)
        {
            case 1:   // calma
                Em(0, t => Bumbo(b, t, 0.35)); Em(8, t => Bumbo(b, t, 0.3));
                Em(4, t => Aro(b, t, 0.18, semente)); Em(12, t => Aro(b, t, 0.18, semente + 1));
                for (int s = 0; s < 16; s += 2) Em(s, t => Chimbal(b, t, 0.04, false, semente + s));
                break;
            case 2:   // pop
                Em(0, t => Bumbo(b, t, 0.5)); Em(8, t => Bumbo(b, t, 0.5)); Em(10, t => Bumbo(b, t, 0.35));
                Em(4, t => Caixa(b, t, 0.3, semente)); Em(12, t => Caixa(b, t, 0.3, semente + 1));
                for (int s = 0; s < 16; s += 2) Em(s, t => Chimbal(b, t, 0.06, s == 14, semente + s));
                if (compasso % 4 == 3) { Em(13, t => Caixa(b, t, 0.18, semente + 5)); Em(15, t => Caixa(b, t, 0.22, semente + 6)); }
                break;
            case 3:   // perseguição
                for (int s = 0; s < 16; s += 4) Em(s, t => Bumbo(b, t, 0.55));
                Em(4, t => Caixa(b, t, 0.35, semente)); Em(12, t => Caixa(b, t, 0.35, semente + 1));
                for (int s = 0; s < 16; s++) Em(s, t => Chimbal(b, t, s % 2 == 0 ? 0.05 : 0.03, s % 4 == 2, semente + s));
                if (compasso % 4 == 0) Em(0, t => Prato(b, t, 0.12, semente + 9));
                if (compasso % 4 == 3) for (int s = 12; s < 16; s++) Em(s, t => Caixa(b, t, 0.15 + 0.04 * (s - 12), semente + s));
                break;
            case 4:   // tensa: relógio
                Em(0, t => Bumbo(b, t, 0.45)); Em(10, t => Bumbo(b, t, 0.3));
                Em(12, t => Caixa(b, t, 0.22, semente));
                for (int s = 0; s < 16; s += 2) Em(s, t => Aro(b, t, s % 4 == 0 ? 0.08 : 0.05, semente + s));
                if (compasso % 4 == 3) { Em(12, t => Surdo(b, t, 0.3, 140)); Em(14, t => Surdo(b, t, 0.3, 105)); }
                break;
            case 5:   // bossa: clave 3-2 + chocalho
                Em(0, t => Bumbo(b, t, 0.3)); Em(6, t => Bumbo(b, t, 0.2)); Em(8, t => Bumbo(b, t, 0.3)); Em(14, t => Bumbo(b, t, 0.2));
                foreach (int s in new[] { 0, 3, 6, 10, 12 }) Em(s, t => Aro(b, t, 0.13, semente + s));
                for (int s = 0; s < 16; s += 2) Em(s, t => Chocalho(b, t, s % 4 == 0 ? 0.05 : 0.035, semente + s));
                break;
            case 6:   // vinheta: rufar no começo e prato no fim
                if (compasso == 0) for (int s = 0; s < 4; s++) Em(s, t => Caixa(b, t, 0.12 + 0.05 * s, semente + s));
                if (compasso == compassos - 1) { Em(0, t => Prato(b, t, 0.2, semente + 7)); Em(0, t => Bumbo(b, t, 0.6)); }
                else { Em(4, t => Bumbo(b, t, 0.4)); Em(8, t => Caixa(b, t, 0.25, semente + 8)); }
                break;
        }
    }

    private static void Bumbo(float[] b, double t, double vol)
    {
        Tom(b, t, 0.18, x => 150 * Math.Exp(-25 * x) + 45, Onda.Seno, vol, ataque: 0.001, decai: 18);
        Tom(b, t, 0.01, x => 1200, Onda.Triangulo, vol * 0.2, ataque: 0.0005, decai: 200);
    }

    private static void Caixa(float[] b, double t, double vol, int semente)
    {
        Ruido(b, t, 0.16, vol, 0.001, 0.04, 14, null, 1000, semente);
        Tom(b, t, 0.08, x => 190, Onda.Triangulo, vol * 0.5, ataque: 0.001, decai: 25);
    }

    private static void Aro(float[] b, double t, double vol, int semente)
    {
        Tom(b, t, 0.03, x => 1700, Onda.Triangulo, vol, ataque: 0.0005, decai: 60);
        Ruido(b, t, 0.02, vol * 0.4, 0.0005, 0.005, 60, null, 3000, semente);
    }

    private static void Chimbal(float[] b, double t, double vol, bool aberto, int semente)
    {
        Ruido(b, t, aberto ? 0.18 : 0.05, vol, 0.0005, 0.01, aberto ? 12 : 60, null, 6000, semente);
    }

    private static void Chocalho(float[] b, double t, double vol, int semente)
    {
        Ruido(b, t, 0.06, vol, 0.01, 0.02, 40, x => 9000, 4000, semente);
    }

    private static void Surdo(float[] b, double t, double vol, double f0)
    {
        Tom(b, t, 0.3, x => f0 * Math.Exp(-3 * x) + f0 * 0.6, Onda.Seno, vol, ataque: 0.001, decai: 10);
    }

    private static void Prato(float[] b, double t, double vol, int semente)
    {
        Ruido(b, t, 1.3, vol, 0.001, 0.3, 2.5, null, 3000, semente);
    }

    // ---------- Mistura ----------

    private static void Somar(float[] destino, float[] origem, double ganho)
    {
        for (int i = 0; i < destino.Length && i < origem.Length; i++) destino[i] += (float)(origem[i] * ganho);
    }

    private static void Eco(float[] b, double atraso, double feedback, double mistura)
    {
        int d = (int)(atraso * Taxa);
        float[] eco = new float[b.Length];
        for (int i = d; i < b.Length; i++) eco[i] = (float)((b[i - d] + eco[i - d]) * feedback);
        for (int i = 0; i < b.Length; i++) b[i] += (float)(eco[i] * mistura / feedback);
    }
}
