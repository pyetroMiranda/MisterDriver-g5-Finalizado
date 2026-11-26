using System;
using System.Collections.Generic;
using System.IO;

// Sintetizador dos efeitos sonoros do jogo (estilo videogame 8/16-bit, combinando com a pixel art).
// Tudo é gerado por código (ondas quadrada/triângulo/serra/seno + ruído + filtros) e salvo como .wav.
// Não usa nada da Unity (só System), para poder ser testado fora do Editor.
// Os nomes (ids) são os mesmos que o jogo usa em Sons.Tocar("...").
public static class SintetizadorSons
{
    public const int Taxa = 22050;

    public enum Onda { Quadrada, Triangulo, Serra, Seno }

    public struct Efeito
    {
        public string id;
        public bool loop;
        public float volume;   // volume sugerido no banco
        public Func<float[]> gerar;
    }

    // ======================================================================
    // Lista de efeitos
    // ======================================================================

    public static List<Efeito> Todos()
    {
        List<Efeito> l = new List<Efeito>();
        void A(string id, float vol, Func<float[]> g, bool loop = false) => l.Add(new Efeito { id = id, volume = vol, gerar = g, loop = loop });

        // ---------- Interface ----------
        A("Clique", 0.7f, Clique);
        A("Navegar", 0.45f, Navegar);
        A("Confirmar", 0.7f, Confirmar);
        A("Voltar", 0.7f, VoltarUI);
        A("Abrir", 0.6f, Abrir);
        A("Comprar", 0.8f, Comprar);
        A("SemMoedas", 0.7f, SemMoedas);
        A("Pausa", 0.7f, Pausa);
        A("Despausa", 0.7f, Despausa);

        // ---------- Carro do jogador ----------
        A("MotorLoop", 0.5f, MotorLoop, true);
        A("DerrapagemLoop", 0.5f, DerrapagemLoop, true);
        A("Freio", 0.6f, Freio);
        A("FreioDeMao", 0.7f, FreioDeMao);
        A("Re", 0.45f, Re);
        A("Buraco", 0.9f, Buraco);
        A("Batida", 0.9f, Batida);
        A("BatidaLeve", 0.75f, BatidaLeve);
        A("BatidaParede", 0.85f, BatidaParede);
        A("Cone", 0.8f, Cone);
        A("PerdeuPontos", 0.7f, PerdeuPontos);
        A("Moeda", 0.65f, Moeda);
        A("Estacionou", 0.7f, Estacionou);

        // ---------- Poderes ----------
        A("Buzina", 0.75f, Buzina);
        A("NitroInicio", 0.85f, NitroInicio);
        A("NitroLoop", 0.5f, NitroLoop, true);
        A("MolaAtivar", 0.75f, MolaAtivar);
        A("MolaEmpurrao", 0.8f, MolaEmpurrao);
        A("GPSLiga", 0.6f, GPSLiga);
        A("GPSDesliga", 0.6f, GPSDesliga);
        A("CameraLentaInicio", 0.8f, CameraLentaInicio);
        A("CameraLentaFim", 0.7f, CameraLentaFim);
        A("ABS", 0.7f, ABS);
        A("Suspensao", 0.7f, Suspensao);
        A("Advogado", 0.8f, Advogado);
        A("PoderPronto", 0.45f, PoderPronto);

        // ---------- Trânsito, rebeldes e pessoas ----------
        A("BuzinaTrafego", 0.6f, BuzinaTrafego);
        A("BuzinaTrafego2", 0.6f, BuzinaTrafego2);
        A("BuzinaRebelde", 0.7f, BuzinaRebelde);
        A("MotoLoop", 0.45f, MotoLoop, true);
        A("CarroRebeldeLoop", 0.5f, CarroRebeldeLoop, true);
        A("Ai", 0.75f, Ai);
        A("Pulo", 0.5f, Pulo);

        // ---------- História ----------
        A("Passo", 0.35f, () => Passo(0));
        A("Passo2", 0.35f, () => Passo(1));
        A("Esbarrao", 0.55f, Esbarrao);
        A("Porta", 0.7f, Porta);
        A("PortaoGaragem", 0.75f, PortaoGaragem);
        A("Interagir", 0.45f, Interagir);
        A("PegouItem", 0.7f, PegouItem);
        A("Latas", 0.9f, Latas);
        A("CaixaRegistradora", 0.8f, Comprar);
        A("Emote_Exclamacao", 0.6f, EmoteExclamacao);
        A("Emote_Interrogacao", 0.6f, EmoteInterrogacao);
        A("Emote_Coracao", 0.55f, EmoteCoracao);
        A("Emote_Sono", 0.5f, EmoteSono);
        A("Emote_Gota", 0.55f, EmoteGota);
        A("Objetivo", 0.6f, Objetivo);
        A("Transicao", 0.5f, Transicao);
        A("DialogoAbrir", 0.5f, DialogoAbrir);
        A("DialogoAvancar", 0.45f, DialogoAvancar);
        A("VozGrave", 0.5f, () => Voz(Onda.Quadrada, 0.5));
        A("VozMedia", 0.5f, () => Voz(Onda.Quadrada, 0.25));
        A("VozFina", 0.5f, () => Voz(Onda.Quadrada, 0.125));
        A("VozSuave", 0.55f, () => Voz(Onda.Triangulo, 0.5));
        A("VozNarrador", 0.5f, () => Voz(Onda.Triangulo, 0.5, 0.06));
        A("Ponto", 0.7f, Ponto);
        A("Falta", 0.7f, Falta);

        // ---------- TURBO RACER 3000 (o videogame "de 1987" do Jason): tudo bem tosco, de 4 bits ----------
        A("RetroStart", 0.6f, RetroStart);
        A("RetroCarregando", 0.45f, RetroCarregando);
        A("RetroMotorLoop", 0.22f, RetroMotorLoop, true);
        A("RetroBatida", 0.8f, RetroBatida);
        A("RetroPonto", 0.4f, RetroPonto);
        A("RetroGameOver", 0.45f, RetroGameOver);
        A("RetroTique", 0.45f, RetroTique);
        A("RetroFicha", 0.45f, RetroFicha);
        return l;
    }

    // ======================================================================
    // Interface
    // ======================================================================

    private static float[] Clique()
    {
        float[] b = Novo(0.07);
        Tom(b, 0, 0.05, t => 1100 - 3000 * t, Onda.Quadrada, 0.5, decai: 30, duty: 0.25);
        Tom(b, 0, 0.02, t => 2200, Onda.Triangulo, 0.25, decai: 60);
        return Fim(b, 0.75);
    }

    private static float[] Navegar()
    {
        float[] b = Novo(0.04);
        Tom(b, 0, 0.03, t => 1250, Onda.Quadrada, 0.4, decai: 50, duty: 0.5);
        return Fim(b, 0.6);
    }

    private static float[] Confirmar()
    {
        float[] b = Novo(0.22);
        Tom(b, 0, 0.07, t => 660, Onda.Quadrada, 0.45, duty: 0.25);
        Tom(b, 0.06, 0.14, t => 990, Onda.Quadrada, 0.45, decai: 15, duty: 0.25);
        return Fim(b, 0.75);
    }

    private static float[] VoltarUI()
    {
        float[] b = Novo(0.22);
        Tom(b, 0, 0.07, t => 880, Onda.Quadrada, 0.45, duty: 0.25);
        Tom(b, 0.06, 0.14, t => 587, Onda.Quadrada, 0.45, decai: 15, duty: 0.25);
        return Fim(b, 0.75);
    }

    private static float[] Abrir()
    {
        float[] b = Novo(0.3);
        Ruido(b, 0, 0.26, 0.5, 0.1, 0.12, 0, t => 500 + 6000 * t / 0.26, 300, 11);
        Tom(b, 0, 0.26, t => 300 + 2300 * t, Onda.Triangulo, 0.12, ataque: 0.08, soltura: 0.12);
        return Fim(b, 0.6);
    }

    // "Ka-ching!" (gaveta + sininho), também usado pela caixa registradora do mercado
    private static float[] Comprar()
    {
        float[] b = Novo(0.7);
        Tom(b, 0, 0.05, t => 180, Onda.Quadrada, 0.3, decai: 40);
        Ruido(b, 0, 0.06, 0.35, 0.001, 0.02, 40, t => 3000, 800, 3);
        Tom(b, 0.07, 0.08, t => 1318, Onda.Quadrada, 0.25, duty: 0.5);
        Tom(b, 0.08, 0.6, t => 2637, Onda.Seno, 0.35, decai: 7);
        Tom(b, 0.08, 0.5, t => 3951, Onda.Seno, 0.18, decai: 9);
        Tom(b, 0.08, 0.4, t => 5274, Onda.Seno, 0.08, decai: 12);
        return Fim(b, 0.8);
    }

    private static float[] SemMoedas()
    {
        float[] b = Novo(0.4);
        Tom(b, 0, 0.12, t => 140, Onda.Quadrada, 0.4);
        Tom(b, 0.15, 0.2, t => 110, Onda.Quadrada, 0.4, decai: 4);
        return Fim(b, 0.7);
    }

    private static float[] Pausa()
    {
        float[] b = Novo(0.2);
        Tom(b, 0, 0.08, t => 660, Onda.Triangulo, 0.4);
        Tom(b, 0.07, 0.11, t => 440, Onda.Triangulo, 0.4, decai: 12);
        return Fim(b, 0.7);
    }

    private static float[] Despausa()
    {
        float[] b = Novo(0.2);
        Tom(b, 0, 0.08, t => 440, Onda.Triangulo, 0.4);
        Tom(b, 0.07, 0.11, t => 660, Onda.Triangulo, 0.4, decai: 12);
        return Fim(b, 0.7);
    }

    // ======================================================================
    // Carro
    // ======================================================================

    // Motor em loop (1 s): o jogo muda o tom conforme a velocidade
    private static float[] MotorLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        Tom(b, 0, d, t => 55, Onda.Serra, 0.35, ataque: 0, soltura: 0);
        Tom(b, 0, d, t => 110, Onda.Quadrada, 0.12, ataque: 0, soltura: 0, duty: 0.3);
        Tom(b, 0, d, t => 165, Onda.Triangulo, 0.08, ataque: 0, soltura: 0);
        Modular(b, t => 0.72 + 0.28 * Math.Abs(Math.Sin(2 * Math.PI * 27.5 * t)));   // "puf-puf" dos pistões
        Ruido(b, 0, d, 0.12, 0, 0, 0, t => 500, 40, 21);
        PassaBaixa(b, 1400);
        FecharLoop(ref b, 0.05);
        return Fim(b, 0.7);
    }

    private static float[] DerrapagemLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        FaixaRuido(b, 0, d, 0.6, 2600, 6, 0, 0, 31);
        Tom(b, 0, d, t => 1750, Onda.Seno, 0.15, ataque: 0, soltura: 0, vib: 0.02, vibHz: 7);
        Modular(b, t => 0.8 + 0.2 * Math.Sin(2 * Math.PI * 9 * t));
        FecharLoop(ref b, 0.06);
        return Fim(b, 0.6);
    }

    private static float[] Freio()
    {
        float[] b = Novo(0.6);
        FaixaRuido(b, 0, 0.55, 0.5, 3000, 8, 0.02, 0.3, 41);
        Tom(b, 0, 0.55, t => 2100 - 700 * t, Onda.Seno, 0.22, decai: 3, vib: 0.03, vibHz: 9);
        return Fim(b, 0.6);
    }

    // Puxar o freio de mão: catraca + chiado curto
    private static float[] FreioDeMao()
    {
        float[] b = Novo(0.6);
        for (int i = 0; i < 6; i++)
        {
            Tom(b, i * 0.03, 0.02, t => 1800, Onda.Quadrada, 0.3, decai: 80, duty: 0.3);
            Ruido(b, i * 0.03, 0.015, 0.3, 0.001, 0.005, 0, t => 6000, 2000, 50 + i);
        }
        FaixaRuido(b, 0.18, 0.35, 0.35, 2500, 5, 0.03, 0.2, 52);
        return Fim(b, 0.7);
    }

    // Bipe de ré (o sensor de ré do Kicks)
    private static float[] Re()
    {
        float[] b = Novo(0.16);
        Tom(b, 0, 0.14, t => 1046, Onda.Quadrada, 0.35, ataque: 0.005, soltura: 0.02);
        return Fim(b, 0.55);
    }

    // Buraco: pancada grave + suspensão balançando + um "clanc" de metal
    private static float[] Buraco()
    {
        float[] b = Novo(0.45);
        Tom(b, 0, 0.25, t => 90 * Math.Exp(-6 * t) + 35, Onda.Seno, 0.9, decai: 10);
        Ruido(b, 0, 0.12, 0.5, 0.001, 0.05, 25, t => 900, 30, 61);
        Tom(b, 0.05, 0.3, t => 220 + 40 * Math.Sin(2 * Math.PI * 18 * t), Onda.Triangulo, 0.2, decai: 10);
        Tom(b, 0.02, 0.15, t => 1350, Onda.Seno, 0.15, decai: 25);
        Tom(b, 0.02, 0.15, t => 2210, Onda.Seno, 0.1, decai: 30);
        return Fim(b, 0.9);
    }

    private static float[] Batida() => BatidaCom(0.65, 1.0, 71);
    private static float[] BatidaLeve() => BatidaCom(0.3, 0.55, 72);

    private static float[] BatidaCom(double dur, double forca, int semente)
    {
        float[] b = Novo(dur + 0.05);
        Ruido(b, 0, dur, 0.9 * forca, 0.001, 0.1, 6, t => 4000 * Math.Exp(-4 * t) + 300, 30, semente);
        Tom(b, 0, 0.3, t => 120 * Math.Exp(-5 * t) + 40, Onda.Seno, 0.8 * forca, decai: 9);
        double[] metal = { 930, 1510, 2270, 3170 };
        foreach (double f in metal) Tom(b, 0.01, dur * 0.8, t => f, Onda.Seno, 0.12 * forca, decai: 8);
        Random r = new Random(semente);
        for (int i = 0; i < (int)(6 * forca); i++)   // caquinhos de vidro
        {
            double f = 3000 + r.NextDouble() * 2000, ini = 0.06 + r.NextDouble() * dur * 0.6;
            Tom(b, ini, 0.08, t => f, Onda.Seno, 0.07, decai: 40);
        }
        Saturar(b, 1.6);
        return Fim(b, 0.9);
    }

    private static float[] BatidaParede()
    {
        float[] b = Novo(0.5);
        Tom(b, 0, 0.3, t => 80 * Math.Exp(-4 * t) + 40, Onda.Seno, 0.9, decai: 12);
        Ruido(b, 0, 0.3, 0.6, 0.001, 0.08, 12, t => 1200, 40, 81);
        Random r = new Random(82);
        for (int i = 0; i < 5; i++)
            Ruido(b, 0.08 + r.NextDouble() * 0.25, 0.03, 0.15, 0.001, 0.01, 60, t => 5000, 1500, 83 + i);
        Saturar(b, 1.4);
        return Fim(b, 0.9);
    }

    private static float[] Cone()
    {
        float[] b = Novo(0.25);
        Tom(b, 0, 0.12, t => 420 * Math.Exp(-3 * t) + 200, Onda.Triangulo, 0.6, decai: 25);
        Ruido(b, 0, 0.05, 0.3, 0.001, 0.02, 50, t => 3000, 500, 91);
        Tom(b, 0.11, 0.08, t => 300, Onda.Triangulo, 0.3, decai: 30);
        return Fim(b, 0.8);
    }

    private static float[] PerdeuPontos()
    {
        float[] b = Novo(0.3);
        Tom(b, 0, 0.1, t => 880, Onda.Quadrada, 0.35);
        Tom(b, 0.11, 0.16, t => 622, Onda.Quadrada, 0.35, decai: 6);
        return Fim(b, 0.65);
    }

    private static float[] Moeda()
    {
        float[] b = Novo(0.34);
        Tom(b, 0, 0.07, t => 988, Onda.Quadrada, 0.35);
        Tom(b, 0.07, 0.26, t => 1319, Onda.Quadrada, 0.35, decai: 9);
        return Fim(b, 0.6);
    }

    private static float[] Estacionou()
    {
        float[] b = Novo(0.5);
        Tom(b, 0, 0.1, t => 784, Onda.Seno, 0.4);
        Tom(b, 0.09, 0.1, t => 1046, Onda.Seno, 0.4);
        Tom(b, 0.18, 0.3, t => 1318, Onda.Seno, 0.4, decai: 6);
        return Fim(b, 0.7);
    }

    // ======================================================================
    // Poderes
    // ======================================================================

    private static float[] Buzina()
    {
        float[] b = Novo(0.45);
        Tom(b, 0, 0.4, t => 415, Onda.Quadrada, 0.35, ataque: 0.01, soltura: 0.05);
        Tom(b, 0, 0.4, t => 523, Onda.Quadrada, 0.3, ataque: 0.01, soltura: 0.05);
        PassaBaixa(b, 2500);
        return Fim(b, 0.75);
    }

    private static float[] NitroInicio()
    {
        float[] b = Novo(0.85);
        Ruido(b, 0, 0.8, 0.8, 0.01, 0.2, 2.5, t => 300 + 5000 * Math.Min(1, t / 0.25), 100, 101);
        Tom(b, 0, 0.6, t => 80 + 200 * t, Onda.Serra, 0.3, decai: 3);
        Tom(b, 0, 0.05, t => 200, Onda.Quadrada, 0.5, decai: 40);
        return Fim(b, 0.85);
    }

    private static float[] NitroLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        Ruido(b, 0, d, 0.5, 0, 0, 0, t => 1800, 200, 111);
        Tom(b, 0, d, t => 70, Onda.Serra, 0.2, ataque: 0, soltura: 0);
        Modular(b, t => 0.7 + 0.3 * Math.Sin(2 * Math.PI * 16 * t));
        FecharLoop(ref b, 0.06);
        return Fim(b, 0.6);
    }

    // Campo de força ligando: subida com brilho
    private static float[] MolaAtivar()
    {
        float[] b = Novo(0.6);
        Tom(b, 0, 0.45, t => 300 + 1500 * t, Onda.Quadrada, 0.35, decai: 2, duty: 0.25);
        Tom(b, 0, 0.45, t => 2000 + 400 * t, Onda.Triangulo, 0.1, vib: 0.05, vibHz: 20);
        Tom(b, 0.4, 0.18, t => 1800, Onda.Seno, 0.25, decai: 10);
        return Fim(b, 0.75);
    }

    private static float[] MolaEmpurrao()
    {
        float[] b = Novo(0.4);
        Tom(b, 0, 0.36, t => 180 + 120 * Math.Sin(2 * Math.PI * 14 * t) * Math.Exp(-6 * t), Onda.Triangulo, 0.6, decai: 6);
        return Fim(b, 0.8);
    }

    private static float[] GPSLiga()
    {
        float[] b = Novo(0.25);
        Tom(b, 0, 0.08, t => 1318, Onda.Seno, 0.4);
        Tom(b, 0.1, 0.13, t => 1760, Onda.Seno, 0.4, decai: 10);
        return Fim(b, 0.6);
    }

    private static float[] GPSDesliga()
    {
        float[] b = Novo(0.25);
        Tom(b, 0, 0.08, t => 1760, Onda.Seno, 0.4);
        Tom(b, 0.1, 0.13, t => 1318, Onda.Seno, 0.4, decai: 10);
        return Fim(b, 0.6);
    }

    // Câmera lenta: "vuuum" descendo
    private static float[] CameraLentaInicio()
    {
        float[] b = Novo(0.95);
        Tom(b, 0, 0.9, t => 600 * Math.Exp(-2.5 * t) + 60, Onda.Serra, 0.3, decai: 1.5);
        Ruido(b, 0, 0.9, 0.3, 0.02, 0.3, 2, t => 2000 * Math.Exp(-3 * t) + 200, 60, 121);
        PassaBaixa(b, 3000);
        return Fim(b, 0.8);
    }

    private static float[] CameraLentaFim()
    {
        float[] b = Novo(0.55);
        Tom(b, 0, 0.5, t => 80 + 700 * t * t * 4, Onda.Serra, 0.25, ataque: 0.05);
        Ruido(b, 0, 0.5, 0.25, 0.1, 0.1, 0, t => 300 + 3000 * t, 60, 131);
        PassaBaixa(b, 3500);
        return Fim(b, 0.7);
    }

    // Freio ABS: o pedal "pulsando"
    private static float[] ABS()
    {
        float[] b = Novo(0.45);
        for (int i = 0; i < 8; i++)
        {
            FaixaRuido(b, i * 0.05, 0.03, 0.4, 2800, 5, 0.003, 0.01, 140 + i);
            Tom(b, i * 0.05, 0.03, t => 1900, Onda.Seno, 0.15, decai: 30);
        }
        return Fim(b, 0.7);
    }

    // Suspensão reforçada: bomba hidráulica + "psst"
    private static float[] Suspensao()
    {
        float[] b = Novo(0.6);
        Tom(b, 0, 0.4, t => 90 + 60 * t, Onda.Quadrada, 0.25, decai: 3, duty: 0.2);
        Ruido(b, 0.3, 0.25, 0.4, 0.01, 0.1, 8, t => 5000, 2000, 151);
        return Fim(b, 0.7);
    }

    // Advogado: duas marteladas e um "objeção!" em bipes
    private static float[] Advogado()
    {
        float[] b = Novo(0.75);
        for (int i = 0; i < 2; i++)
        {
            Tom(b, i * 0.18, 0.1, t => 300 * Math.Exp(-10 * t) + 120, Onda.Triangulo, 0.7, decai: 30);
            Ruido(b, i * 0.18, 0.05, 0.3, 0.001, 0.02, 40, t => 2500, 200, 161 + i);
        }
        Tom(b, 0.38, 0.12, t => 660, Onda.Quadrada, 0.3, duty: 0.25);
        Tom(b, 0.5, 0.2, t => 880, Onda.Quadrada, 0.3, decai: 6, duty: 0.25);
        return Fim(b, 0.8);
    }

    private static float[] PoderPronto()
    {
        float[] b = Novo(0.32);
        Tom(b, 0, 0.1, t => 1568, Onda.Seno, 0.3, decai: 15);
        Tom(b, 0.08, 0.22, t => 2093, Onda.Seno, 0.3, decai: 12);
        return Fim(b, 0.5);
    }

    // ======================================================================
    // Trânsito, rebeldes e pessoas
    // ======================================================================

    private static float[] BuzinaTrafego()
    {
        float[] b = Novo(0.35);
        Tom(b, 0, 0.3, t => 349, Onda.Quadrada, 0.35, ataque: 0.01, soltura: 0.04);
        Tom(b, 0, 0.3, t => 440, Onda.Quadrada, 0.3, ataque: 0.01, soltura: 0.04);
        PassaBaixa(b, 2200);
        return Fim(b, 0.7);
    }

    // "Bi-biii!" (duas buzinadas)
    private static float[] BuzinaTrafego2()
    {
        float[] b = Novo(0.55);
        for (int i = 0; i < 2; i++)
        {
            double ini = i * 0.2, dur = i == 0 ? 0.12 : 0.3;
            Tom(b, ini, dur, t => 330, Onda.Quadrada, 0.35, ataque: 0.01, soltura: 0.03);
            Tom(b, ini, dur, t => 392, Onda.Quadrada, 0.3, ataque: 0.01, soltura: 0.03);
        }
        PassaBaixa(b, 2200);
        return Fim(b, 0.7);
    }

    // Buzina musical da gangue (aquela clássica de cinco notas)
    private static float[] BuzinaRebelde()
    {
        float[] b = Novo(1.1);
        double[] notas = { 523, 523, 523, 698, 880, 523, 523, 523, 698, 880 };
        double[] dur = { 0.08, 0.08, 0.08, 0.14, 0.2, 0.08, 0.08, 0.08, 0.14, 0.2 };
        double ini = 0;
        for (int i = 0; i < 5; i++)
        {
            double f = notas[i];
            Tom(b, ini, dur[i], t => f, Onda.Serra, 0.3, ataque: 0.005, soltura: 0.02);
            Tom(b, ini, dur[i], t => f * 1.26, Onda.Quadrada, 0.15, ataque: 0.005, soltura: 0.02);
            ini += dur[i] + 0.03;
        }
        PassaBaixa(b, 3000);
        return Fim(b, 0.75);
    }

    // Moto de dois tempos ("ring-ding-ding")
    private static float[] MotoLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        Tom(b, 0, d, t => 95, Onda.Serra, 0.35, ataque: 0, soltura: 0);
        Tom(b, 0, d, t => 190, Onda.Quadrada, 0.15, ataque: 0, soltura: 0, duty: 0.15);
        Modular(b, t => 0.6 + 0.4 * Math.Abs(Math.Sin(2 * Math.PI * 31 * t)));
        Ruido(b, 0, d, 0.08, 0, 0, 0, t => 6000, 1500, 171);
        PassaBaixa(b, 3000);
        FecharLoop(ref b, 0.05);
        return Fim(b, 0.65);
    }

    // Carro rebaixado com escapamento furado
    private static float[] CarroRebeldeLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        Tom(b, 0, d, t => 45, Onda.Serra, 0.4, ataque: 0, soltura: 0);
        Tom(b, 0, d, t => 67, Onda.Quadrada, 0.2, ataque: 0, soltura: 0, duty: 0.3);
        Modular(b, t => 0.7 + 0.15 * Math.Sin(2 * Math.PI * 11 * t) + 0.15 * Math.Sin(2 * Math.PI * 17 * t));
        Ruido(b, 0, d, 0.15, 0, 0, 0, t => 300, 30, 181);
        PassaBaixa(b, 900);
        Saturar(b, 2);
        FecharLoop(ref b, 0.05);
        return Fim(b, 0.7);
    }

    // "Ai!" (voz feita com formantes: de "a" para "i")
    private static float[] Ai()
    {
        float[] b = Novo(0.38);
        Voz(b, 0, 0.32, t => 290 - 90 * t, t => 800 - 1600 * t, t => 1200 + 3300 * t, 0.7);
        return Fim(b, 0.75);
    }

    private static float[] Pulo()
    {
        float[] b = Novo(0.2);
        Tom(b, 0, 0.18, t => 300 + 900 * t, Onda.Quadrada, 0.35, decai: 6, duty: 0.25);
        return Fim(b, 0.6);
    }

    // ======================================================================
    // História (casa, mercado, falas, exame)
    // ======================================================================

    private static float[] Passo(int variacao)
    {
        float[] b = Novo(0.08);
        Ruido(b, 0, 0.06, 0.6, 0.001, 0.02, 55, t => variacao == 0 ? 900 : 1200, 80, 191 + variacao);
        Tom(b, 0, 0.04, t => variacao == 0 ? 110 : 130, Onda.Seno, 0.4, decai: 60);
        return Fim(b, 0.6);
    }

    private static float[] Esbarrao()
    {
        float[] b = Novo(0.16);
        Tom(b, 0, 0.12, t => 120 * Math.Exp(-8 * t) + 50, Onda.Seno, 0.8, decai: 20);
        Ruido(b, 0, 0.08, 0.3, 0.001, 0.03, 30, t => 600, 30, 201);
        return Fim(b, 0.7);
    }

    // Porta: rangido + trinco
    private static float[] Porta()
    {
        float[] b = Novo(0.5);
        Tom(b, 0, 0.35, t => 380 + 60 * Math.Sin(2 * Math.PI * 23 * t) + 150 * t, Onda.Serra, 0.12, ataque: 0.04, decai: 2);
        PassaBaixa(b, 2500);
        Tom(b, 0.33, 0.05, t => 1500, Onda.Quadrada, 0.3, decai: 60, duty: 0.3);
        Tom(b, 0.36, 0.1, t => 90, Onda.Seno, 0.4, decai: 30);
        return Fim(b, 0.7);
    }

    // Portão de enrolar da garagem subindo
    private static float[] PortaoGaragem()
    {
        float[] b = Novo(1.4);
        Tom(b, 0, 1.3, t => 100, Onda.Quadrada, 0.12, ataque: 0.1, soltura: 0.2, duty: 0.3);
        Random r = new Random(211);
        for (double t0 = 0.05; t0 < 1.15; t0 += 0.045)
            Ruido(b, t0, 0.02, 0.15 + r.NextDouble() * 0.15, 0.001, 0.01, 0, t => 6000, 2000, 212 + (int)(t0 * 100));
        Tom(b, 1.2, 0.15, t => 90, Onda.Seno, 0.5, decai: 15);
        Ruido(b, 1.2, 0.08, 0.3, 0.001, 0.03, 30, t => 1500, 60, 299);
        return Fim(b, 0.75);
    }

    private static float[] Interagir()
    {
        float[] b = Novo(0.06);
        Tom(b, 0, 0.05, t => 880, Onda.Quadrada, 0.3, decai: 20, duty: 0.25);
        return Fim(b, 0.5);
    }

    private static float[] PegouItem()
    {
        float[] b = Novo(0.45);
        Tom(b, 0, 0.08, t => 784, Onda.Quadrada, 0.3);
        Tom(b, 0.08, 0.08, t => 988, Onda.Quadrada, 0.3);
        Tom(b, 0.16, 0.26, t => 1319, Onda.Quadrada, 0.3, decai: 6);
        Tom(b, 0.16, 0.26, t => 2637, Onda.Seno, 0.1, decai: 8);
        return Fim(b, 0.7);
    }

    // A pirâmide de latas desmoronando
    private static float[] Latas()
    {
        float[] b = Novo(1.3);
        Random r = new Random(221);
        for (int i = 0; i < 20; i++)
        {
            double ini = Math.Pow(r.NextDouble(), 1.6) * 0.9;
            double f = 1800 + r.NextDouble() * 2400;
            double dec = 25 + r.NextDouble() * 15;
            double vol = 0.12 + r.NextDouble() * 0.12;
            Tom(b, ini, 0.25, t => f, Onda.Seno, vol, decai: dec);
            Tom(b, ini, 0.2, t => f * 2.76, Onda.Seno, vol * 0.5, decai: dec * 1.3);
            Ruido(b, ini, 0.02, vol, 0.001, 0.01, 0, t => 8000, 3000, 222 + i);
        }
        Tom(b, 0.05, 0.2, t => 150, Onda.Seno, 0.25, decai: 15);
        return Fim(b, 0.85);
    }

    private static float[] EmoteExclamacao()
    {
        float[] b = Novo(0.22);
        Tom(b, 0, 0.06, t => 880, Onda.Quadrada, 0.35);
        Tom(b, 0.06, 0.13, t => 1760, Onda.Quadrada, 0.35, decai: 8);
        return Fim(b, 0.65);
    }

    private static float[] EmoteInterrogacao()
    {
        float[] b = Novo(0.3);
        Tom(b, 0, 0.26, t => 500 + 8000 * t * t, Onda.Triangulo, 0.4, decai: 2);
        return Fim(b, 0.65);
    }

    private static float[] EmoteCoracao()
    {
        float[] b = Novo(0.5);
        double[] f = { 1568, 1976, 2349, 3136 };
        for (int i = 0; i < f.Length; i++)
        {
            double fi = f[i];
            Tom(b, i * 0.06, 0.3, t => fi, Onda.Seno, 0.25, decai: 10, vib: 0.01, vibHz: 12);
        }
        return Fim(b, 0.6);
    }

    private static float[] EmoteSono()
    {
        float[] b = Novo(0.7);
        Tom(b, 0, 0.6, t => 220 - 60 * t, Onda.Triangulo, 0.35, ataque: 0.1, soltura: 0.2, vib: 0.04, vibHz: 4);
        return Fim(b, 0.55);
    }

    private static float[] EmoteGota()
    {
        float[] b = Novo(0.18);
        Tom(b, 0, 0.15, t => 1400 * Math.Exp(-10 * t) + 300, Onda.Seno, 0.5, decai: 12);
        return Fim(b, 0.6);
    }

    private static float[] Objetivo()
    {
        float[] b = Novo(0.6);
        Tom(b, 0, 0.5, t => 1046, Onda.Seno, 0.3, decai: 6);
        Tom(b, 0.08, 0.5, t => 1568, Onda.Seno, 0.3, decai: 6);
        Tom(b, 0.08, 0.5, t => 1572, Onda.Triangulo, 0.08, decai: 6);
        return Fim(b, 0.6);
    }

    private static float[] Transicao()
    {
        float[] b = Novo(0.65);
        Ruido(b, 0, 0.6, 0.3, 0.25, 0.3, 0, t => 300 + 2500 * Math.Sin(Math.PI * t / 0.6), 80, 231);
        return Fim(b, 0.45);
    }

    private static float[] DialogoAbrir()
    {
        float[] b = Novo(0.08);
        Tom(b, 0, 0.06, t => 600 + 3000 * t, Onda.Quadrada, 0.3, decai: 20, duty: 0.25);
        return Fim(b, 0.5);
    }

    private static float[] DialogoAvancar()
    {
        float[] b = Novo(0.05);
        Tom(b, 0, 0.035, t => 1500, Onda.Quadrada, 0.25, decai: 50, duty: 0.25);
        return Fim(b, 0.45);
    }

    // Bipe de "voz" das falas (o jogo muda o tom para cada personagem). Base em 520 Hz, como o bipe antigo.
    private static float[] Voz(Onda forma, double duty, double vib = 0)
    {
        float[] b = Novo(0.05);
        Tom(b, 0, 0.045, t => 520, forma, 0.5, ataque: 0.002, soltura: 0.015, duty: duty, vib: vib, vibHz: 30);
        return Fim(b, 0.55);
    }

    private static float[] Ponto()
    {
        float[] b = Novo(0.45);
        Tom(b, 0, 0.1, t => 1046, Onda.Seno, 0.35);
        Tom(b, 0.09, 0.32, t => 1568, Onda.Seno, 0.35, decai: 6);
        Tom(b, 0.09, 0.25, t => 3136, Onda.Seno, 0.08, decai: 10);
        return Fim(b, 0.7);
    }

    // Campainha de falta do exame
    private static float[] Falta()
    {
        float[] b = Novo(0.4);
        Tom(b, 0, 0.35, t => 120, Onda.Quadrada, 0.35);
        Tom(b, 0, 0.35, t => 127, Onda.Serra, 0.2);
        return Fim(b, 0.7);
    }

    // ======================================================================
    // TURBO RACER 3000
    // ======================================================================

    private static float[] RetroStart()
    {
        float[] b = Novo(0.3);
        double[] n = { 523, 659, 784, 1046 };
        for (int i = 0; i < n.Length; i++) { double f = n[i]; Tom(b, i * 0.055, 0.06, t => f, Onda.Quadrada, 0.4, ataque: 0, soltura: 0); }
        Quantizar(b, 8);
        return Fim(b, 0.6);
    }

    // Barulhinho de "dados" carregando da fita
    private static float[] RetroCarregando()
    {
        float[] b = Novo(0.7);
        Random r = new Random(301);
        for (double t0 = 0; t0 < 0.65; t0 += 0.035)
        {
            double f = 300 + r.Next(0, 6) * 220;
            Tom(b, t0, 0.03, t => f, Onda.Quadrada, 0.25, ataque: 0, soltura: 0);
        }
        Quantizar(b, 6);
        return Fim(b, 0.45);
    }

    private static float[] RetroMotorLoop()
    {
        const double d = 1.05;
        float[] b = Novo(d);
        Tom(b, 0, d, t => 60, Onda.Quadrada, 0.4, ataque: 0, soltura: 0, duty: 0.5);
        Tom(b, 0, d, t => 90, Onda.Quadrada, 0.15, ataque: 0, soltura: 0, duty: 0.25);
        Quantizar(b, 4);
        FecharLoop(ref b, 0.02);
        return Fim(b, 0.5);
    }

    // Batida: ruído de 4 bits e um "vuuum" descendo
    private static float[] RetroBatida()
    {
        float[] b = Novo(0.9);
        Ruido(b, 0, 0.85, 0.9, 0.001, 0.3, 4, t => 3000, 0, 311);
        Tom(b, 0, 0.6, t => 220 * Math.Exp(-4 * t) + 40, Onda.Quadrada, 0.4);
        Quantizar(b, 5);
        return Fim(b, 0.8);
    }

    private static float[] RetroPonto()
    {
        float[] b = Novo(0.2);
        Tom(b, 0, 0.07, t => 1046, Onda.Quadrada, 0.4, ataque: 0, soltura: 0);
        Tom(b, 0.08, 0.1, t => 1568, Onda.Quadrada, 0.4, ataque: 0, soltura: 0.02);
        Quantizar(b, 8);
        return Fim(b, 0.55);
    }

    // Musiquinha de GAME OVER descendo
    private static float[] RetroGameOver()
    {
        float[] b = Novo(1.3);
        double[] n = { 523, 440, 349, 262 };
        double[] dur = { 0.18, 0.18, 0.18, 0.6 };
        double ini = 0;
        for (int i = 0; i < n.Length; i++)
        {
            double f = n[i];
            Tom(b, ini, dur[i], t => f, Onda.Quadrada, 0.4, ataque: 0, soltura: 0.03, vib: i == 3 ? 0.03 : 0, vibHz: 7);
            ini += dur[i] + 0.02;
        }
        Quantizar(b, 8);
        return Fim(b, 0.65);
    }

    private static float[] RetroTique()
    {
        float[] b = Novo(0.05);
        Tom(b, 0, 0.035, t => 880, Onda.Quadrada, 0.4, ataque: 0, soltura: 0);
        return Fim(b, 0.45);
    }

    // "SEM FICHAS": buzina grave e feia
    private static float[] RetroFicha()
    {
        float[] b = Novo(0.5);
        Tom(b, 0, 0.18, t => 98, Onda.Quadrada, 0.45, ataque: 0, soltura: 0);
        Tom(b, 0.22, 0.25, t => 92, Onda.Quadrada, 0.45, ataque: 0, soltura: 0.03);
        Quantizar(b, 4);
        return Fim(b, 0.6);
    }

    // Poucos níveis de volume (som de videogame bem velho)
    public static void Quantizar(float[] b, int niveis)
    {
        double max = 0;
        foreach (float v in b) max = Math.Max(max, Math.Abs(v));
        if (max < 1e-6) return;
        for (int i = 0; i < b.Length; i++) b[i] = (float)(Math.Round(b[i] / max * niveis) / niveis * max);
    }

    // ======================================================================
    // Ferramentas de som
    // ======================================================================

    public static float[] Novo(double segundos) => new float[Math.Max(1, (int)(segundos * Taxa))];

    public static double Forma(Onda o, double fase, double duty)
    {
        switch (o)
        {
            case Onda.Quadrada: return fase < duty ? 1 : -1;
            case Onda.Triangulo: return 4 * Math.Abs(fase - 0.5) - 1;
            case Onda.Serra: return 2 * fase - 1;
            default: return Math.Sin(2 * Math.PI * fase);
        }
    }

    // Uma nota: frequência pode variar com o tempo; envelope = ataque linear, queda exponencial e soltura no fim
    public static void Tom(float[] b, double ini, double dur, Func<double, double> freq, Onda forma, double vol,
                           double ataque = 0.004, double soltura = 0.03, double decai = 0, double duty = 0.5,
                           double vib = 0, double vibHz = 6, double vibAtraso = 0)
    {
        int i0 = (int)(ini * Taxa), n = (int)(dur * Taxa);
        double fase = 0;
        for (int k = 0; k < n; k++)
        {
            int i = i0 + k;
            if (i < 0) continue;
            if (i >= b.Length) break;
            double t = k / (double)Taxa;
            double v = vib > 0 && t > vibAtraso ? 1 + vib * Math.Sin(2 * Math.PI * vibHz * (t - vibAtraso)) : 1;
            fase += freq(t) * v / Taxa;
            fase -= Math.Floor(fase);
            b[i] += (float)(vol * Envelope(t, dur, ataque, soltura, decai) * Forma(forma, fase, duty));
        }
    }

    public static double Envelope(double t, double dur, double ataque, double soltura, double decai)
    {
        double a = ataque > 0 && t < ataque ? t / ataque : 1;
        if (decai > 0) a *= Math.Exp(-decai * t);
        if (soltura > 0 && t > dur - soltura) a *= Math.Max(0, (dur - t) / soltura);
        return a;
    }

    // Ruído branco com passa-baixa (corte pode variar) e passa-alta fixo
    public static void Ruido(float[] b, double ini, double dur, double vol, double ataque, double soltura, double decai,
                             Func<double, double> passaBaixa, double passaAlta, int semente)
    {
        Random r = new Random(semente);
        int i0 = (int)(ini * Taxa), n = (int)(dur * Taxa);
        double lp = 0, hpY = 0, hpX = 0;
        double aHp = passaAlta > 0 ? 1.0 / (1.0 + 2 * Math.PI * passaAlta / Taxa) : 1;
        for (int k = 0; k < n; k++)
        {
            int i = i0 + k;
            if (i < 0) continue;
            if (i >= b.Length) break;
            double t = k / (double)Taxa;
            double x = r.NextDouble() * 2 - 1;
            double corte = passaBaixa != null ? passaBaixa(t) : Taxa / 2.0;
            double aLp = 1 - Math.Exp(-2 * Math.PI * Math.Min(corte, Taxa * 0.45) / Taxa);
            lp += aLp * (x - lp);
            double y = lp;
            if (passaAlta > 0)
            {
                hpY = aHp * (hpY + y - hpX);
                hpX = y;
                y = hpY;
            }
            b[i] += (float)(vol * Envelope(t, dur, ataque, soltura, decai) * y);
        }
    }

    // Ruído passando por um passa-faixa (chiados, pneus)
    public static void FaixaRuido(float[] b, double ini, double dur, double vol, double centro, double q, double ataque,
                                  double soltura, int semente)
    {
        Random r = new Random(semente);
        Biquad f = Biquad.PassaFaixa(centro, q);
        int i0 = (int)(ini * Taxa), n = (int)(dur * Taxa);
        for (int k = 0; k < n; k++)
        {
            int i = i0 + k;
            if (i < 0) continue;
            if (i >= b.Length) break;
            double t = k / (double)Taxa;
            double y = f.Passar(r.NextDouble() * 2 - 1) * 3;
            b[i] += (float)(vol * Envelope(t, dur, ataque, soltura, 0) * y);
        }
    }

    // Voz simples: pulsos (serra) passando por dois formantes que mudam com o tempo
    public static void Voz(float[] b, double ini, double dur, Func<double, double> f0, Func<double, double> f1,
                           Func<double, double> f2, double vol)
    {
        int i0 = (int)(ini * Taxa), n = (int)(dur * Taxa);
        double fase = 0;
        Biquad a = Biquad.PassaFaixa(f1(0), 5), c = Biquad.PassaFaixa(f2(0), 7);
        for (int k = 0; k < n; k++)
        {
            int i = i0 + k;
            if (i >= b.Length) break;
            double t = k / (double)Taxa;
            if (k % 64 == 0)
            {
                a.Ajustar(Math.Max(150, f1(t)), 5);
                c.Ajustar(Math.Max(300, f2(t)), 7);
            }
            fase += f0(t) * (1 + 0.02 * Math.Sin(2 * Math.PI * 6 * t)) / Taxa;
            fase -= Math.Floor(fase);
            double pulso = 2 * fase - 1;
            double y = a.Passar(pulso) * 2.2 + c.Passar(pulso) * 1.4;
            b[i] += (float)(vol * Envelope(t, dur, 0.02, 0.1, 0) * y);
        }
    }

    public static void Modular(float[] b, Func<double, double> ganho)
    {
        for (int i = 0; i < b.Length; i++) b[i] *= (float)ganho(i / (double)Taxa);
    }

    public static void PassaBaixa(float[] b, double corte)
    {
        double a = 1 - Math.Exp(-2 * Math.PI * corte / Taxa), y = 0;
        for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = (float)y; }
    }

    public static void Saturar(float[] b, double ganho)
    {
        for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * ganho);
    }

    // Loop sem emenda: o pedaço extra do fim é misturado no começo
    public static void FecharLoop(ref float[] b, double cruzamento)
    {
        int m = (int)(cruzamento * Taxa);
        int n = b.Length - m;
        if (m <= 0 || n <= m) return;
        float[] r = new float[n];
        Array.Copy(b, r, n);
        for (int i = 0; i < m; i++)
        {
            double k = i / (double)m;
            r[i] = (float)(b[i] * k + b[n + i] * (1 - k));
        }
        b = r;
    }

    // Normaliza para o pico pedido e tira estalo no começo/fim
    public static float[] Fim(float[] b, double pico)
    {
        double max = 0;
        foreach (float v in b) max = Math.Max(max, Math.Abs(v));
        if (max < 1e-6) return b;
        double g = pico / max;
        for (int i = 0; i < b.Length; i++) b[i] = (float)(b[i] * g);
        return b;
    }

    public static byte[] Wav(float[] b, int taxa = Taxa)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter w = new BinaryWriter(ms))
        {
            int dados = b.Length * 2;
            w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            w.Write(36 + dados);
            w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            w.Write(16);
            w.Write((short)1);       // PCM
            w.Write((short)1);       // mono
            w.Write(taxa);
            w.Write(taxa * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            w.Write(dados);
            foreach (float v in b)
            {
                double c = Math.Max(-1, Math.Min(1, v));
                w.Write((short)Math.Round(c * 32767));
            }
            return ms.ToArray();
        }
    }

    // Filtro biquad (fórmulas do "Audio EQ Cookbook")
    public class Biquad
    {
        private double b0, b1, b2, a1, a2, x1, x2, y1, y2;

        public static Biquad PassaFaixa(double centro, double q)
        {
            Biquad f = new Biquad();
            f.Ajustar(centro, q);
            return f;
        }

        public void Ajustar(double centro, double q)
        {
            double w = 2 * Math.PI * Math.Min(centro, Taxa * 0.45) / Taxa;
            double alfa = Math.Sin(w) / (2 * q);
            double a0 = 1 + alfa;
            b0 = alfa / a0;
            b1 = 0;
            b2 = -alfa / a0;
            a1 = -2 * Math.Cos(w) / a0;
            a2 = (1 - alfa) / a0;
        }

        public double Passar(double x)
        {
            double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x;
            y2 = y1; y1 = y;
            return y;
        }
    }
}
