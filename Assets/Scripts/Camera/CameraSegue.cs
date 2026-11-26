using UnityEngine;

// Câmera que segue o carro do jogador: o capô fica sempre para cima na tela e o carro fica perto do centro,
// com o carro inteiro (frente e traseira) visível. Andando rápido, a câmera olha um pouco mais à frente
// e se afasta um pouco (zoom), para dar tempo de ver o que vem pela frente. De ré, olha um pouco para trás.
public class CameraSegue : MonoBehaviour
{
    public Transform alvo;                    // o carro

    [Header("Posição")]
    [Tooltip("Quanto a câmera olha à frente do carro na velocidade máxima.")]
    public float distanciaFrente = 1.6f;
    [Tooltip("Quanto a câmera olha à frente com o carro parado (0 = carro bem no centro).")]
    public float distanciaParado = 0.4f;
    [Tooltip("Quão rápido a câmera ajusta o quanto olha à frente.")]
    public float suavizacaoPosicao = 4f;
    [Tooltip("Quão rápido a câmera gira nas curvas.")]
    public float suavizacaoRotacao = 4f;

    [Header("Distância (zoom)")]
    [Tooltip("Metade da altura da tela em unidades do mundo. Maior = câmera mais longe.")]
    public float tamanhoCamera = 6.5f;
    [Tooltip("Quanto a câmera se afasta a mais na velocidade máxima.")]
    public float zoomVelocidade = 0.8f;
    [Tooltip("Velocidade considerada 'máxima' para o olhar à frente e o zoom.")]
    public float velocidadeReferencia = 10f;
    [Tooltip("Quão rápido o zoom acompanha a velocidade.")]
    public float suavizacaoZoom = 1.5f;

    public bool spriteApontaParaDireita = false; // marque se o capô do sprite aponta para a direita

    private Camera cam;
    private CarroController carro;
    private float olharFrente;   // quanto olha à frente agora (suavizado; negativo = de ré)

    void Start()
    {
        cam = GetComponent<Camera>();

        // Se o carro não foi ligado no Inspector, procura sozinho
        if (alvo == null)
        {
            CarroController achado = FindFirstObjectByType<CarroController>();
            if (achado != null) alvo = achado.transform;
        }
        if (alvo != null) carro = alvo.GetComponent<CarroController>();

        // Câmera 2D precisa ficar atrás do mapa (Z negativo); em Z = 0 ela não mostra nada
        if (transform.position.z > -1f)
        {
            Vector3 p = transform.position;
            p.z = -10f;
            transform.position = p;
        }

        olharFrente = distanciaParado;
        if (cam != null && cam.orthographic) cam.orthographicSize = tamanhoCamera;
    }

    void LateUpdate()
    {
        if (alvo == null) return;
        float dt = Time.deltaTime;

        // Direção da frente do carro
        Vector2 frente = spriteApontaParaDireita ? (Vector2)alvo.right : (Vector2)alvo.up;

        // Quão rápido o carro está (0 = parado, 1 = velocidade máxima); negativo = de ré
        float velocidade = carro != null ? carro.VelocidadeComSinal : 0f;
        float fator = Mathf.Clamp(velocidade / Mathf.Max(0.1f, velocidadeReferencia), -1f, 1f);

        // --- ROTAÇÃO: o capô fica sempre para cima na tela ---
        float anguloAlvo = alvo.eulerAngles.z;
        if (spriteApontaParaDireita) anguloAlvo -= 90f;

        float anguloAtual = transform.eulerAngles.z;
        float novoAngulo = Mathf.LerpAngle(anguloAtual, anguloAlvo, suavizacaoRotacao * dt);
        transform.rotation = Quaternion.Euler(0f, 0f, novoAngulo);

        // --- POSIÇÃO: o carro perto do centro, olhando um pouco à frente conforme a velocidade ---
        // (suaviza só o "olhar à frente", não a posição: assim o carro não escapa para a borda da tela)
        float olharDesejado = fator >= 0f
            ? Mathf.Lerp(distanciaParado, distanciaFrente, fator)
            : Mathf.Lerp(distanciaParado, -distanciaFrente * 0.6f, -fator);
        olharFrente = Mathf.Lerp(olharFrente, olharDesejado, suavizacaoPosicao * dt);

        Vector3 posicao = alvo.position + (Vector3)(frente * olharFrente);
        posicao.z = transform.position.z;
        transform.position = posicao;

        // --- ZOOM: afasta um pouco quando está rápido ---
        if (cam != null && cam.orthographic)
        {
            float tamanhoDesejado = tamanhoCamera + zoomVelocidade * Mathf.Abs(fator);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, tamanhoDesejado, suavizacaoZoom * dt);
        }
    }
}
