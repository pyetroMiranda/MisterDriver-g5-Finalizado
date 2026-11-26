using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Minimapa (radar estilo GTA). Serve para qualquer nível: ao começar, "fotografa" o cenário fixo (sem carros
// nem pessoas) e transforma num mapa estilizado: ruas limpas, calçadas e grama escuras e casas em destaque.
// Gira junto com a câmera, aproxima/afasta com a velocidade e nunca mostra além dos limites do nível.
// Mostra o jogador, a chegada (presa na borda quando está longe), a rota do GPS e os carros do trânsito.
// Some sozinho quando o jogo está pausado (Time.timeScale = 0) e em cenas sem carro do jogador.
public class Minimapa : MonoBehaviour
{
    [Header("Partes (montado pela ferramenta)")]
    [SerializeField] private RectTransform raiz;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform mascara;
    [SerializeField] private RectTransform giro;
    [SerializeField] private RawImage mapa;
    [SerializeField] private LinhaUI rota;
    [SerializeField] private RectTransform pontos;
    [SerializeField] private Image modeloPonto;
    [SerializeField] private RectTransform jogador;
    [SerializeField] private RectTransform destino;
    [SerializeField] private RectTransform norte;
    [SerializeField] private float raioNorte = 150f;
    [SerializeField] private TMP_Text texto;

    [Header("Zoom (metros do centro até a borda)")]
    [SerializeField] private float alcanceParado = 55f;
    [SerializeField] private float alcanceRapido = 80f;

    [Header("Mapa")]
    [Tooltip("Resolução do mapa gerado (pixels por metro).")]
    [SerializeField] private float pixelsPorUnidade = 6f;
    [Tooltip("Objetos com estes nomes (no objeto ou na imagem) aparecem como casas em destaque.")]
    [SerializeField] private string[] nomesDeCasas = { "casa", "house", "predio", "prédio", "edificio", "edifício", "building" };
    [Tooltip("Marque para escolher os limites do mapa à mão (senão, usa tudo o que tem no cenário).")]
    [SerializeField] private bool usarLimitesManuais;
    [SerializeField] private Rect limitesManuais = new Rect(-50f, -50f, 100f, 100f);
    [SerializeField] private bool mostrarTrafego = true;
    [SerializeField] private bool mostrarRotaGPS = true;

    [Header("Cores do mapa")]
    [SerializeField] private Color32 corFora = new Color32(14, 8, 24, 255);
    [SerializeField] private Color32 corRua = new Color32(118, 114, 150, 255);
    [SerializeField] private Color32 corBordaRua = new Color32(206, 200, 240, 255);
    [SerializeField] private Color32 corCalcada = new Color32(64, 58, 90, 255);
    [SerializeField] private Color32 corGrama = new Color32(30, 70, 56, 255);
    [SerializeField] private Color32 corContornoCasa = new Color32(18, 0, 24, 255);

    [Header("Posição")]
    [Tooltip("Se a lista de poderes (lado esquerdo) encostar no mapa, ele vai para o canto superior direito.")]
    [SerializeField] private bool fugirDosPoderes = true;
    [SerializeField] private Vector2 posicaoAlternativa = new Vector2(-30f, -200f);

    private CarroController carro;
    private Transform chegada;
    private Rect limites;
    private bool pronto;
    private float alcance;
    private float alfa;
    private string nomeNivel;
    private LineRenderer linhaGPS;
    private readonly List<Image> pontosTrafego = new List<Image>();
    private readonly List<Vector2> pontosRota = new List<Vector2>();
    private Vector3[] bufferGPS = new Vector3[64];

    // ---------- Começo ----------

    private void Start()
    {
        carro = FindFirstObjectByType<CarroController>();
        if (carro == null || mapa == null || mascara == null)
        {
            gameObject.SetActive(false);   // menu ou cena sem carro
            return;
        }

        ChegadaNivel fim = FindFirstObjectByType<ChegadaNivel>();
        if (fim != null) chegada = fim.transform;
        else
        {
            ChegadaTutorial tutorial = FindFirstObjectByType<ChegadaTutorial>();
            if (tutorial != null) chegada = tutorial.transform;
        }

        nomeNivel = NomeDoNivel(SceneManager.GetActiveScene().name);
        alcance = alcanceParado;
        if (grupo != null) grupo.alpha = 0f;
        if (modeloPonto != null) modeloPonto.gameObject.SetActive(false);
        if (destino != null) destino.gameObject.SetActive(chegada != null);

        StartCoroutine(Montar());
        if (fugirDosPoderes) StartCoroutine(FugirDosPoderes());
    }

    private static string NomeDoNivel(string cena)
    {
        string n = cena.Replace("_", " ").Trim();
        if (n.StartsWith("Nivel")) n = "NÍVEL " + n.Substring(5).Trim();
        return n.ToUpperInvariant();
    }

    // ---------- Geração do mapa ----------

    private Camera cameraFoto;
    private readonly List<Renderer> escondidos = new List<Renderer>();

    private IEnumerator Montar()
    {
        List<SpriteRenderer> casas = new List<SpriteRenderer>();
        List<Tilemap> mapas = new List<Tilemap>();
        if (!CalcularLimites(casas, mapas))
        {
            Debug.LogWarning("Minimapa: não achei nada no cenário para desenhar.", this);
            gameObject.SetActive(false);
            yield break;
        }

        float ppu = Mathf.Min(pixelsPorUnidade, 4096f / Mathf.Max(limites.width, limites.height));
        int w = Mathf.Max(8, Mathf.CeilToInt(limites.width * ppu));
        int h = Mathf.Max(8, Mathf.CeilToInt(limites.height * ppu));
        limites.size = new Vector2(w / ppu, h / ppu);   // ajusta para caber pixels inteiros

        // Câmera temporária que fotografa o nível inteiro uma vez
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        GameObject obj = new GameObject("CameraMinimapa (temporária)");
        cameraFoto = obj.AddComponent<Camera>();
        cameraFoto.orthographic = true;
        cameraFoto.orthographicSize = limites.height * 0.5f;
        cameraFoto.transform.position = new Vector3(limites.center.x, limites.center.y, -50f);
        cameraFoto.clearFlags = CameraClearFlags.SolidColor;
        cameraFoto.backgroundColor = corFora;
        cameraFoto.cullingMask = ~(1 << 5);   // sem a camada UI
        cameraFoto.depth = -100;
        cameraFoto.allowHDR = false;
        cameraFoto.allowMSAA = false;
        cameraFoto.targetTexture = rt;

        RenderPipelineManager.beginCameraRendering += AntesDaFoto;
        RenderPipelineManager.endCameraRendering += DepoisDaFoto;
        Camera.onPreCull += AntesDaFotoClassico;
        Camera.onPostRender += DepoisDaFotoClassico;

        yield return new WaitForEndOfFrame();

        RenderPipelineManager.beginCameraRendering -= AntesDaFoto;
        RenderPipelineManager.endCameraRendering -= DepoisDaFoto;
        Camera.onPreCull -= AntesDaFotoClassico;
        Camera.onPostRender -= DepoisDaFotoClassico;
        Mostrar();   // garante que nada ficou escondido

        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D foto = new Texture2D(w, h, TextureFormat.RGBA32, false);
        foto.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        foto.Apply(false);
        RenderTexture.active = anterior;

        cameraFoto.targetTexture = null;
        Destroy(obj);
        rt.Release();
        Destroy(rt);

        Color32[] pixels = foto.GetPixels32();
        Destroy(foto);

        Estilizar(pixels, w, h, ppu, casas, mapas);

        Texture2D final = new Texture2D(w, h, TextureFormat.RGBA32, true)
        {
            name = "Minimapa",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 2,
        };
        final.SetPixels32(pixels);
        final.Apply(true, true);
        mapa.texture = final;
        pronto = true;
    }

    // Esconde carros, pessoas, textos e efeitos só durante a foto (a câmera do jogo não é afetada)
    private void AntesDaFoto(ScriptableRenderContext contexto, Camera cam) => Esconder(cam);
    private void DepoisDaFoto(ScriptableRenderContext contexto, Camera cam) { if (cam == cameraFoto) Mostrar(); }
    private void AntesDaFotoClassico(Camera cam) => Esconder(cam);
    private void DepoisDaFotoClassico(Camera cam) { if (cam == cameraFoto) Mostrar(); }

    private void Esconder(Camera cam)
    {
        if (cam != cameraFoto || escondidos.Count > 0) return;
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || Fotografar(r)) continue;
            r.enabled = false;
            escondidos.Add(r);
        }
    }

    private void Mostrar()
    {
        foreach (Renderer r in escondidos) if (r != null) r.enabled = true;
        escondidos.Clear();
    }

    // Só entra no mapa o cenário fixo: imagens e tilemaps que não pertencem a nada que se mexe
    private static bool Fotografar(Renderer r)
    {
        bool tipo = r is TilemapRenderer || (r is SpriteRenderer sr && sr.sprite != null);
        if (!tipo) return false;

        Transform t = r.transform;
        if (t.GetComponentInParent<CarroController>() != null) return false;
        if (t.GetComponentInParent<CarroTrafego>() != null) return false;
        if (t.GetComponentInParent<Pedestre>() != null) return false;
        if (t.GetComponentInParent<Canvas>() != null) return false;
        if (t.GetComponentInParent<Camera>() != null) return false;
        Rigidbody2D corpo = t.GetComponentInParent<Rigidbody2D>();
        if (corpo != null && corpo.bodyType != RigidbodyType2D.Static) return false;
        return true;
    }

    private bool CalcularLimites(List<SpriteRenderer> casas, List<Tilemap> mapas)
    {
        bool achou = false;
        Bounds total = new Bounds();

        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !Fotografar(r)) continue;

            Bounds b;
            if (r is TilemapRenderer tr)
            {
                Tilemap tm = tr.GetComponent<Tilemap>();
                if (tm == null || tm.GetUsedTilesCount() == 0) continue;
                tm.CompressBounds();
                BoundsInt c = tm.cellBounds;
                Vector3 a = tm.CellToWorld(c.min), z = tm.CellToWorld(new Vector3Int(c.xMax, c.yMax, 0));
                b = new Bounds((a + z) * 0.5f, new Vector3(Mathf.Abs(z.x - a.x), Mathf.Abs(z.y - a.y), 0f));
                mapas.Add(tm);
            }
            else
            {
                b = r.bounds;
                if (EhCasa((SpriteRenderer)r)) casas.Add((SpriteRenderer)r);
            }

            if (!achou) { total = b; achou = true; }
            else total.Encapsulate(b);
        }

        limites = usarLimitesManuais ? limitesManuais : new Rect(total.min.x, total.min.y, total.size.x, total.size.y);
        return (achou || usarLimitesManuais) && limites.width > 1f && limites.height > 1f;
    }

    private bool EhCasa(SpriteRenderer sr)
    {
        string nomes = (sr.name + " " + sr.sprite.name + " " + (sr.sprite.texture != null ? sr.sprite.texture.name : "")).ToLowerInvariant();
        foreach (string n in nomesDeCasas)
            if (!string.IsNullOrEmpty(n) && nomes.Contains(n.ToLowerInvariant())) return true;
        return false;
    }

    // ---------- Estilo ----------

    private const byte Nada = 0, Rua = 1, Calcada = 2, Grama = 3;

    private void Estilizar(Color32[] px, int w, int h, float ppu, List<SpriteRenderer> casas, List<Tilemap> mapas)
    {
        Color32[] foto = (Color32[])px.Clone();
        byte[] tipo = new byte[w * h];

        // O que é rua, calçada e grama vem do nome das peças do Tilemap (bem mais limpo que pela cor)
        foreach (Tilemap tm in mapas) Classificar(tm, tipo, w, h, ppu);

        for (int i = 0; i < px.Length; i++)
        {
            switch (tipo[i])
            {
                case Rua: px[i] = corRua; break;
                case Calcada: px[i] = Color32.Lerp(corCalcada, Tom(foto[i]), 0.25f); break;
                case Grama: px[i] = Color32.Lerp(corGrama, Tom(foto[i]), 0.4f); break;
                default: px[i] = Tom(foto[i]); break;
            }
        }

        // Borda clara nas ruas, como no GTA
        int borda = Mathf.Max(1, Mathf.RoundToInt(0.3f * ppu));
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (tipo[y * w + x] != Rua) continue;
                bool perto = false;
                for (int k = 1; k <= borda && !perto; k++)
                    perto = NaoRua(tipo, w, h, x + k, y) || NaoRua(tipo, w, h, x - k, y) ||
                            NaoRua(tipo, w, h, x, y + k) || NaoRua(tipo, w, h, x, y - k);
                if (perto) px[y * w + x] = corBordaRua;
            }
        }

        // Casas: primeiro todas as sombras, depois a cor de verdade (um pouco mais viva) com contorno
        List<RectInt> retangulos = new List<RectInt>();
        foreach (SpriteRenderer casa in casas)
        {
            RectInt r = Retangulo(casa.bounds, w, h, ppu);
            if (r.width > 0 && r.height > 0) retangulos.Add(r);
        }

        int sombra = Mathf.Max(1, Mathf.RoundToInt(0.6f * ppu));
        bool[] sombreado = new bool[w * h];
        foreach (RectInt r in retangulos)
            for (int y = r.yMin - sombra; y < r.yMax - sombra; y++)
                for (int x = r.xMin + sombra; x < r.xMax + sombra; x++)
                    if (x >= 0 && y >= 0 && x < w && y < h && !sombreado[y * w + x])
                    {
                        sombreado[y * w + x] = true;
                        px[y * w + x] = Color32.Lerp(px[y * w + x], new Color32(0, 0, 0, 255), 0.5f);
                    }

        foreach (RectInt r in retangulos)
        {
            int linha = Mathf.Max(1, Mathf.RoundToInt(0.2f * ppu));
            for (int y = r.yMin; y < r.yMax; y++)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    bool contorno = x < r.xMin + linha || x >= r.xMax - linha || y < r.yMin + linha || y >= r.yMax - linha;
                    px[y * w + x] = contorno ? corContornoCasa : Viva(foto[y * w + x]);
                }
            }
        }
    }

    private static bool NaoRua(byte[] tipo, int w, int h, int x, int y)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return false;   // borda da imagem não conta
        return tipo[y * w + x] != Rua;
    }

    private RectInt Retangulo(Bounds b, int w, int h, float ppu)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt((b.min.x - limites.xMin) * ppu), 0, w);
        int y0 = Mathf.Clamp(Mathf.FloorToInt((b.min.y - limites.yMin) * ppu), 0, h);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((b.max.x - limites.xMin) * ppu), 0, w);
        int y1 = Mathf.Clamp(Mathf.CeilToInt((b.max.y - limites.yMin) * ppu), 0, h);
        return new RectInt(x0, y0, x1 - x0, y1 - y0);
    }

    private void Classificar(Tilemap tm, byte[] tipo, int w, int h, float ppu)
    {
        BoundsInt c = tm.cellBounds;
        if (c.size.x <= 0 || c.size.y <= 0) return;

        Vector3 origem = tm.CellToWorld(Vector3Int.zero);
        Vector3 ex = tm.CellToWorld(Vector3Int.right) - origem;
        Vector3 ey = tm.CellToWorld(Vector3Int.up) - origem;
        if (Mathf.Abs(ex.y) > 0.001f || Mathf.Abs(ey.x) > 0.001f || Mathf.Abs(ex.x) < 0.0001f || Mathf.Abs(ey.y) < 0.0001f)
            return;   // grade girada: usa só a foto

        // Tipo de cada célula
        TileBase[] pecas = tm.GetTilesBlock(c);
        Dictionary<TileBase, byte> cache = new Dictionary<TileBase, byte>();
        byte[] celulas = new byte[c.size.x * c.size.y];
        for (int z = 0; z < c.size.z; z++)
        {
            for (int i = 0; i < celulas.Length; i++)
            {
                TileBase p = pecas[i + z * celulas.Length];
                if (p == null) continue;
                if (!cache.TryGetValue(p, out byte t)) cache[p] = t = TipoDaPeca(p.name);
                if (t != Nada) celulas[i] = t;
            }
        }

        for (int y = 0; y < h; y++)
        {
            float my = limites.yMin + (y + 0.5f) / ppu;
            int cy = Mathf.FloorToInt((my - origem.y) / ey.y) - c.yMin;
            if (cy < 0 || cy >= c.size.y) continue;
            for (int x = 0; x < w; x++)
            {
                float mx = limites.xMin + (x + 0.5f) / ppu;
                int cx = Mathf.FloorToInt((mx - origem.x) / ex.x) - c.xMin;
                if (cx < 0 || cx >= c.size.x) continue;
                byte t = celulas[cx + cy * c.size.x];
                if (t != Nada) tipo[y * w + x] = t;
            }
        }
    }

    private static byte TipoDaPeca(string nome)
    {
        string n = nome.ToLowerInvariant();
        if (n.Contains("cal") || n.Contains("sidewalk") || n.Contains("passeio")) return Calcada;
        if (n.Contains("grama") || n.Contains("grass") || n.Contains("jardim")) return Grama;
        if (n.Contains("rua") || n.Contains("faixa") || n.Contains("asfalto") || n.Contains("road")) return Rua;
        return Nada;
    }

    // Tom de "mapa": menos saturado, mais escuro e puxado para o azul
    private static Color32 Tom(Color32 c)
    {
        float l = (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f);
        float r = Mathf.Lerp(c.r, l, 0.45f) * 0.72f + 6f;
        float g = Mathf.Lerp(c.g, l, 0.45f) * 0.76f + 6f;
        float b = Mathf.Lerp(c.b, l, 0.45f) * 0.9f + 16f;
        return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
    }

    // Cor das casas: a de verdade, um pouco mais viva e clara
    private static Color32 Viva(Color32 c)
    {
        float l = (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f);
        float r = Mathf.LerpUnclamped(l, c.r, 1.2f) * 1.08f;
        float g = Mathf.LerpUnclamped(l, c.g, 1.2f) * 1.08f;
        float b = Mathf.LerpUnclamped(l, c.b, 1.2f) * 1.08f;
        return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
    }

    // ---------- Posição na tela ----------

    // Se a lista de poderes ficar comprida e encostar no mapa, o mapa muda para o canto superior direito
    private IEnumerator FugirDosPoderes()
    {
        yield return null;
        yield return null;
        if (raiz == null) yield break;

        HUDPoderes poderes = FindFirstObjectByType<HUDPoderes>();
        if (poderes == null || !poderes.isActiveAndEnabled) yield break;

        Canvas.ForceUpdateCanvases();
        Rect meu = NaTela(mascara);
        foreach (RectTransform filho in poderes.GetComponentsInChildren<RectTransform>())
        {
            if (filho == poderes.transform || !filho.gameObject.activeInHierarchy || filho.GetComponent<Graphic>() == null) continue;
            if (!NaTela(filho).Overlaps(meu)) continue;

            raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(1f, 1f);
            raiz.anchoredPosition = posicaoAlternativa;
            yield break;
        }
    }

    private static Rect NaTela(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
    }

    // ---------- A cada quadro ----------

    private void LateUpdate()
    {
        if (carro == null) return;

        // Aparece quando o mapa está pronto e some quando o jogo está pausado
        float alvoAlfa = pronto && Time.timeScale > 0f ? 1f : 0f;
        alfa = Mathf.MoveTowards(alfa, alvoAlfa, Time.unscaledDeltaTime * 4f);
        if (grupo != null) grupo.alpha = alfa;
        if (!pronto) return;

        Camera cam = Camera.main;
        float anguloCamera = cam != null ? cam.transform.eulerAngles.z : 0f;
        Quaternion volta = Quaternion.Euler(0f, 0f, -anguloCamera);

        // Mais rápido = vê mais longe
        float rapidez = Mathf.Clamp01(carro.VelocidadeAtual / Mathf.Max(0.01f, carro.VelocidadeMaximaBase));
        alcance = Mathf.Lerp(alcance, Mathf.Lerp(alcanceParado, alcanceRapido, rapidez), 2f * Time.unscaledDeltaTime);

        float raioUI = mascara.rect.width * 0.5f;
        float escala = raioUI / alcance;
        Vector2 posicao = carro.transform.position;
        Vector2 centro = Limitar(posicao, alcance);

        giro.localRotation = volta;
        mapa.rectTransform.sizeDelta = limites.size * escala;
        mapa.rectTransform.anchoredPosition = (limites.center - centro) * escala;

        // Jogador (sai do meio só perto das bordas do nível)
        jogador.anchoredPosition = volta * ((posicao - centro) * escala);
        jogador.localRotation = Quaternion.Euler(0f, 0f, carro.transform.eulerAngles.z - anguloCamera);

        // Norte gira em volta do anel
        if (norte != null) norte.anchoredPosition = volta * (Vector2.up * raioNorte);

        AtualizarChegada(volta, centro, escala, raioUI, posicao);
        AtualizarRota(escala);
        AtualizarTrafego(escala, centro);
    }

    // O centro do mapa nunca deixa aparecer o que está fora do nível
    private Vector2 Limitar(Vector2 p, float raio)
    {
        float x = limites.width > raio * 2f ? Mathf.Clamp(p.x, limites.xMin + raio, limites.xMax - raio) : limites.center.x;
        float y = limites.height > raio * 2f ? Mathf.Clamp(p.y, limites.yMin + raio, limites.yMax - raio) : limites.center.y;
        return new Vector2(x, y);
    }

    private Vector2 NoMapa(Vector2 mundo, float escala) => (mundo - limites.center) * escala;

    private void AtualizarChegada(Quaternion volta, Vector2 centro, float escala, float raioUI, Vector2 posicao)
    {
        string info = nomeNivel;
        if (chegada != null && destino != null)
        {
            Vector2 alvo = chegada.position;
            Vector2 rel = volta * ((alvo - centro) * escala);
            float pulso = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f);

            // Longe: fica preso na borda do radar, apontando a direção
            if (rel.magnitude > raioUI - 10f)
            {
                rel = rel.normalized * (raioUI - 2f);
                destino.localScale = Vector3.one * 0.8f * pulso;
            }
            else destino.localScale = Vector3.one * pulso;
            destino.anchoredPosition = rel;

            float metros = Vector2.Distance(posicao, alvo);
            info += metros < 12f ? "  <color=#FF1ECD>CHEGANDO!</color>" : "  <color=#2DE2E6>" + Mathf.RoundToInt(metros) + " m</color>";
        }

        if (texto != null && texto.text != info) texto.text = info;
    }

    private void AtualizarRota(float escala)
    {
        if (rota == null) return;

        if (linhaGPS == null && mostrarRotaGPS)
        {
            foreach (LineRenderer lr in FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                if (lr.name == "LinhaGPS") { linhaGPS = lr; break; }
        }

        if (!mostrarRotaGPS || linhaGPS == null || !linhaGPS.enabled || linhaGPS.positionCount < 2)
        {
            rota.Limpar();
            return;
        }

        if (bufferGPS.Length < linhaGPS.positionCount) bufferGPS = new Vector3[linhaGPS.positionCount * 2];
        int n = linhaGPS.GetPositions(bufferGPS);
        pontosRota.Clear();
        for (int i = 0; i < n; i++) pontosRota.Add(NoMapa(bufferGPS[i], escala));
        rota.Definir(pontosRota);
    }

    private Color corTrafego = new Color(0f, 0f, 0f, 0f);

    private void AtualizarTrafego(float escala, Vector2 centro)
    {
        if (pontos == null || modeloPonto == null) return;

        int usados = 0;
        if (corTrafego.a == 0f && modeloPonto != null) corTrafego = modeloPonto.color;
        if (mostrarTrafego)
        {
            float limite = alcance * 1.5f;
            foreach (CarroTrafego c in FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None))
            {
                Vector2 p = c.transform.position;
                if ((p - centro).sqrMagnitude > limite * limite) continue;

                if (usados >= pontosTrafego.Count)
                {
                    Image novo = Instantiate(modeloPonto, pontos);
                    novo.name = "Carro";
                    pontosTrafego.Add(novo);
                }
                Image ponto = pontosTrafego[usados++];
                if (!ponto.gameObject.activeSelf) ponto.gameObject.SetActive(true);
                ponto.color = corTrafego;
                ponto.rectTransform.localScale = Vector3.one;
                ponto.rectTransform.anchoredPosition = NoMapa(p, escala);
            }

            // Rebeldes da Gangue do Escapamento: vermelhos e um pouco maiores
            foreach (CarroRebelde r in CarroRebelde.Todos)
            {
                if (r == null) continue;
                Vector2 p = r.transform.position;
                if ((p - centro).sqrMagnitude > limite * limite) continue;

                if (usados >= pontosTrafego.Count)
                {
                    Image novo = Instantiate(modeloPonto, pontos);
                    novo.name = "Carro";
                    pontosTrafego.Add(novo);
                }
                Image ponto = pontosTrafego[usados++];
                if (!ponto.gameObject.activeSelf) ponto.gameObject.SetActive(true);
                ponto.color = new Color(1f, 0.15f, 0.25f, 1f);
                ponto.rectTransform.localScale = Vector3.one * 1.5f;
                ponto.rectTransform.anchoredPosition = NoMapa(p, escala);
            }
        }

        for (int i = usados; i < pontosTrafego.Count; i++)
            if (pontosTrafego[i].gameObject.activeSelf) pontosTrafego[i].gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Mostrar();
    }
}
