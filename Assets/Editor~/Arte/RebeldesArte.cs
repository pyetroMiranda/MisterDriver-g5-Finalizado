using System.Collections.Generic;
using UnityEngine;

// Veículos da Gangue do Escapamento, no estilo pixel art dos carros do trânsito (vistos de cima, frente para CIMA):
// - carros pichados: os modelos do trânsito pintados de cor escura e cheios de pichações neon;
// - motos: desenhadas aqui mesmo, com o piloto de jaqueta e capacete moicano.
// Usado por MontarHistoriaNivel1 (GangueRebelde).
public static class RebeldesArte
{
    public const string Pasta = "Assets/Imagens/Historia/Rebeldes";
    private const float PPU = 20f;   // igual aos carros do trânsito

    private static readonly Color32 K = PixelArt.Contorno;

    private static readonly Color32[] BasesCarro =
    {
        new Color32(44, 44, 54, 255), new Color32(84, 34, 120, 255), new Color32(30, 86, 64, 255), new Color32(120, 30, 44, 255),
    };

    private static readonly Color32[] Neon =
    {
        new Color32(255, 40, 200, 255), new Color32(120, 255, 60, 255), new Color32(40, 230, 255, 255),
        new Color32(255, 230, 40, 255), new Color32(255, 120, 30, 255),
    };

    // ======================================================================
    // Carros pichados
    // ======================================================================

    public static Sprite[] CarrosPichados(CatalogoCarros catalogo)
    {
        List<CatalogoCarros.Modelo> bases = new List<CatalogoCarros.Modelo>();
        string[] preferidos = { "Hatch", "SUV", "Seda", "Picape", "Compacto", "Esportivo" };
        if (catalogo != null)
        {
            foreach (string id in preferidos)
                foreach (CatalogoCarros.Modelo m in catalogo.modelos)
                    if (m != null && m.id == id && m.lataria != null) bases.Add(m);
            if (bases.Count == 0)
                foreach (CatalogoCarros.Modelo m in catalogo.modelos) if (m != null && m.lataria != null) bases.Add(m);
        }

        List<Sprite> resultado = new List<Sprite>();
        for (int i = 0; i < 4 && bases.Count > 0; i++)
        {
            CatalogoCarros.Modelo m = bases[i % bases.Count];
            Color32[] lat = MontarCarros.LerPixels(m.lataria, out int w, out int h);
            Color32[] det = m.detalhes != null ? MontarCarros.LerPixels(m.detalhes, out _, out _) : PixelArt.NovaTela(w, h);
            Color32[] p = Pichar(lat, det, w, h, BasesCarro[i % BasesCarro.Length], 1234 + i * 77);
            Vector2 pivo = new Vector2(m.lataria.pivot.x / m.lataria.rect.width, m.lataria.pivot.y / m.lataria.rect.height);
            resultado.Add(PixelArt.Salvar(Pasta, "CarroPichado_" + i, p, w, h, default, m.lataria.pixelsPerUnit, pivo));
        }
        return resultado.ToArray();
    }

    // Pinta a lataria (tons de cinza) com a cor de base e espalha pichações por cima; os detalhes (vidros, faróis) ficam por cima
    private static Color32[] Pichar(Color32[] lat, Color32[] det, int w, int h, Color32 baseCor, int semente)
    {
        Random.State antes = Random.state;
        Random.InitState(semente);

        Color32[] p = new Color32[w * h];
        bool[] pintavel = new bool[w * h];
        for (int i = 0; i < p.Length; i++)
        {
            Color32 c = lat[i];
            if (c.a == 0) { p[i] = new Color32(0, 0, 0, 0); continue; }
            float v = c.r / 255f;
            if (v < 0.25f) { p[i] = c; continue; }   // contorno escuro continua
            p[i] = new Color32((byte)(baseCor.r * v), (byte)(baseCor.g * v), (byte)(baseCor.b * v), c.a);
            pintavel[i] = true;
        }

        // Rabiscos (tags) com spray neon
        int tags = 7;
        for (int k = 0; k < tags; k++)
        {
            Color32 cor = Neon[Random.Range(0, Neon.Length)];
            int x = Random.Range(3, w - 3), y = Random.Range(3, h - 3);
            int dx = Random.value < 0.5f ? 1 : -1, dy = Random.Range(-1, 2);
            int passos = Random.Range(8, 18);
            for (int s = 0; s < passos; s++)
            {
                Spray(p, pintavel, w, h, x, y, cor);
                if (s % 3 == 0) Spray(p, pintavel, w, h, x, y + 1, cor);
                if (Random.value < 0.35f) dy = Random.Range(-1, 2);
                if (Random.value < 0.12f) dx = -dx;
                x += dx; y += dy;
            }
        }

        // Estrelinhas e um raio
        for (int k = 0; k < 4; k++)
        {
            Color32 cor = Neon[Random.Range(0, Neon.Length)];
            int x = Random.Range(4, w - 4), y = Random.Range(4, h - 4);
            Spray(p, pintavel, w, h, x, y, cor);
            Spray(p, pintavel, w, h, x - 1, y, cor); Spray(p, pintavel, w, h, x + 1, y, cor);
            Spray(p, pintavel, w, h, x, y - 1, cor); Spray(p, pintavel, w, h, x, y + 1, cor);
        }
        int rx = w / 2 - 3, ry = h / 2;
        int[,] raio = { { 3, 0 }, { 2, 1 }, { 1, 2 }, { 2, 2 }, { 3, 2 }, { 4, 2 }, { 3, 3 }, { 2, 4 }, { 1, 5 } };
        for (int i = 0; i < raio.GetLength(0); i++) Spray(p, pintavel, w, h, rx + raio[i, 0], ry + raio[i, 1], Neon[3]);

        // Detalhes por cima (vidros, faróis, pneus)
        for (int i = 0; i < p.Length; i++)
            if (det[i].a > 0) p[i] = det[i];

        Random.state = antes;
        return p;
    }

    // y conta de cima para baixo (como os outros desenhos)
    private static void Spray(Color32[] p, bool[] pintavel, int w, int h, int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        int i = (h - 1 - y) * w + x;
        if (pintavel[i]) p[i] = c;
    }

    // ======================================================================
    // Motos
    // ======================================================================

    public static Sprite[] Motos()
    {
        return new[]
        {
            Moto("MotoRebelde_0", new Color32(255, 40, 200, 255), new Color32(250, 250, 250, 255), new Color32(255, 40, 200, 255),
                 new Color32(40, 40, 52, 255)),
            Moto("MotoRebelde_1", new Color32(120, 255, 60, 255), new Color32(230, 60, 60, 255), new Color32(40, 230, 255, 255),
                 new Color32(90, 56, 34, 255)),
        };
    }

    private static Sprite Moto(string nome, Color32 tanque, Color32 capacete, Color32 moicano, Color32 jaqueta)
    {
        HistoriaArte.Tela t = new HistoriaArte.Tela(16, 38);
        Color32 pneu = new Color32(26, 26, 30, 255), pneuClaro = new Color32(70, 70, 80, 255);
        Color32 metal = new Color32(170, 176, 190, 255), tacha = new Color32(210, 210, 220, 255);

        // Roda da frente e farol
        t.Ret(6, 0, 4, 9, pneu); t.Ret(7, 1, 2, 7, pneuClaro);
        t.Ret(6, 8, 4, 2, new Color32(255, 240, 160, 255));
        // Guidão com retrovisores
        t.Ret(1, 10, 14, 2, metal);
        t.Ret(0, 10, 2, 2, K); t.Ret(14, 10, 2, 2, K);
        t.Ret(1, 8, 2, 2, metal); t.Ret(13, 8, 2, 2, metal);
        // Tanque pichado
        t.Ret(4, 12, 8, 7, tanque);
        t.Ret(5, 13, 1, 4, new Color32(255, 255, 255, 255));
        t.P(9, 14, K); t.P(10, 16, K);
        t.Borda(4, 12, 8, 7, K);
        // Piloto: braços até o guidão, jaqueta com tachas e o capacete moicano
        t.Ret(2, 12, 2, 6, jaqueta); t.Ret(12, 12, 2, 6, jaqueta);
        t.Ret(2, 18, 12, 9, jaqueta);
        t.Borda(2, 18, 12, 9, K);
        for (int x = 3; x < 13; x += 3) { t.P(x, 19, tacha); t.P(x, 25, tacha); }
        for (int y = 14; y < 26; y++)
            for (int x = 2; x < 14; x++)
            {
                float dx = x - 7.5f, dy = y - 20f;
                float d = dx * dx + dy * dy;
                if (d <= 14f) t.P(x, y, capacete);
                else if (d <= 22f) t.P(x, y, K);
            }
        t.Ret(7, 16, 2, 8, moicano);
        t.Ret(7, 15, 2, 1, K);
        t.P(5, 18, new Color32(255, 255, 255, 255));
        // Banco, escapamento e roda de trás
        t.Ret(5, 27, 6, 3, new Color32(60, 40, 30, 255));
        t.Ret(12, 26, 2, 8, metal); t.Ret(12, 34, 2, 1, new Color32(60, 60, 70, 255));
        t.Ret(6, 29, 4, 9, pneu); t.Ret(7, 30, 2, 7, pneuClaro);
        return HistoriaArte.SalvarComPpu(t, Pasta, nome, PPU, new Vector2(0.5f, 0.5f));
    }
}
