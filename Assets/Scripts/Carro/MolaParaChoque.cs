using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Poder "mola-de-para-choque": cria um campo de força temporário ao redor do carro.
// Carros do trânsito que encostam na aura são empurrados para longe.
public class MolaParaChoque : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;      // item da loja que libera este poder
    [SerializeField] private bool comprado = true;   // só vale se Item Loja estiver vazio (para testar)

    [Header("Campo de força")]
    [SerializeField] private float raio = 4f;             // tamanho da aura, a partir do centro do carro
    [SerializeField] private float duracao = 5f;          // segundos com o campo ligado
    [SerializeField] private float recarga = 20f;         // segundos até poder usar de novo

    [Header("Empurrão")]
    [SerializeField] private float forcaEmpurrao = 10f;   // velocidade com que o carro é jogado longe
    [SerializeField] private float tempoAtordoado = 1.5f; // segundos que o carro fica parado depois do empurrão

    [Header("Visual (opcional)")]
    [SerializeField] private SpriteRenderer aura;         // um círculo filho do carro
    [SerializeField, Range(0f, 1f)] private float alphaAura = 0.35f;
    [SerializeField] private float velocidadePulso = 6f;
    [SerializeField] private float tempoTransicao = 0.2f; // aura aparece e some suavemente

    [Header("Som (opcional)")]
    [SerializeField] private AudioSource fonteAudio;
    [SerializeField] private AudioClip somAtivar;
    [SerializeField] private AudioClip somEmpurrao;

    [Header("Eventos")]
    public UnityEvent aoAtivar;
    public UnityEvent aoEncerrar;
    public UnityEvent aoFicarPronto;

    private bool ativa;
    private float tempoRestante;
    private float tempoRecarga;
    private float intensidade;
    private CarroTrafego[] carros;

    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public string Nome => "Mola";
    public string Tecla => "M";
    public string Info => "";
    public bool Ativo => ativa;
    public bool Ativa => ativa;
    public bool Pronto => Comprado && !ativa && tempoRecarga <= 0f;

    // 0 a 1: enquanto ativa, mostra o que sobra; enquanto recarrega, mostra o quanto já carregou
    public float Progresso => ativa
        ? tempoRestante / Mathf.Max(duracao, 0.01f)
        : 1f - tempoRecarga / Mathf.Max(recarga, 0.01f);

    private void Start()
    {
        // Se o campo Aura apontar para um prefab da pasta Project (e não para um objeto da cena),
        // nada apareceria na tela e o script estaria alterando o próprio arquivo do prefab.
        if (aura != null && !aura.gameObject.scene.IsValid())
        {
            Debug.LogWarning("Mola-de-para-choque: o campo Aura aponta para o prefab '" + aura.name +
                             "' da pasta Project. Coloque a bolha DENTRO do carro (na Hierarchy) " +
                             "e arraste esse objeto da Hierarchy para o campo Aura.", this);
            aura = null;
        }

        DefinirAlphaAura(0f);
        if (aura != null) aura.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Não funciona com o jogo pausado (tutorial, pause, game over)
        if (Time.timeScale == 0f) return;

        float dt = Time.deltaTime;

        if (ApertouMola())
        {
            if (Pronto) Ativar();
            else if (!Comprado) Debug.Log("Mola-de-para-choque ainda não foi comprada na loja");
            else if (ativa) Debug.Log("Mola-de-para-choque já está ligada");
            else Debug.Log("Mola-de-para-choque em recarga: faltam " + tempoRecarga.ToString("0.0") + " s");
        }

        if (ativa)
        {
            tempoRestante -= dt;
            if (tempoRestante <= 0f) Encerrar();
        }
        else if (tempoRecarga > 0f)
        {
            tempoRecarga -= dt;
            if (tempoRecarga <= 0f)
            {
                tempoRecarga = 0f;
                aoFicarPronto?.Invoke();
            }
        }

        AtualizarAura(dt);
    }

    private void FixedUpdate()
    {
        if (!ativa || carros == null) return;

        Vector2 centro = transform.position;

        foreach (CarroTrafego carro in carros)
        {
            if (carro == null || !carro.PodeSerEmpurrado) continue;

            // Usa a borda do carro do trânsito (não só o centro), assim a aura pega o para-choque
            Vector2 pontoMaisProximo = carro.transform.position;
            Collider2D colisor = carro.GetComponentInChildren<Collider2D>();
            if (colisor != null) pontoMaisProximo = colisor.ClosestPoint(centro);

            if (Vector2.Distance(centro, pontoMaisProximo) > raio) continue;

            Vector2 direcao = (Vector2)carro.transform.position - centro;
            if (direcao.sqrMagnitude < 0.0001f) direcao = transform.up;

            carro.Empurrar(direcao.normalized * forcaEmpurrao);
            carro.Parar(tempoAtordoado);

            if (fonteAudio != null && somEmpurrao != null)
                fonteAudio.PlayOneShot(somEmpurrao);

            Debug.Log("Mola-de-para-choque empurrou " + carro.name);
        }
    }

    private void Ativar()
    {
        ativa = true;
        tempoRestante = duracao;

        // Os carros não mudam durante a fase, então basta procurar uma vez por ativação
        carros = FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None);

        if (fonteAudio != null && somAtivar != null)
            fonteAudio.PlayOneShot(somAtivar);

        Debug.Log("Mola-de-para-choque ligada por " + duracao + " s (carros na cena: " + carros.Length + ")");

        if (aura == null)
            Debug.LogWarning("Mola-de-para-choque: o campo Aura está vazio, então a bolha não vai aparecer. " +
                             "Arraste a sprite da bolha para o campo Aura.", this);
        else
            Debug.Log("Aura: " + aura.name + " | Order in Layer: " + aura.sortingOrder +
                      " | escala no mundo: " + aura.transform.lossyScale, aura);
        aoAtivar?.Invoke();
    }

    private void Encerrar()
    {
        ativa = false;
        tempoRestante = 0f;
        tempoRecarga = recarga;   // a recarga começa quando o campo desliga

        aoEncerrar?.Invoke();
    }

    private void AtualizarAura(float dt)
    {
        intensidade = Mathf.MoveTowards(intensidade, ativa ? 1f : 0f, dt / Mathf.Max(tempoTransicao, 0.01f));
        if (aura == null) return;

        // Liga o objeto da aura só enquanto ela aparece (funciona mesmo se ele começar desativado)
        bool visivel = intensidade > 0f;
        if (aura.gameObject.activeSelf != visivel) aura.gameObject.SetActive(visivel);
        if (!aura.enabled) aura.enabled = true;

        // Pisca rápido quando está quase acabando, para o jogador perceber
        float velocidade = ativa && tempoRestante < 1f ? velocidadePulso * 3f : velocidadePulso;
        float pulso = 0.75f + 0.25f * Mathf.Sin(Time.time * velocidade);
        DefinirAlphaAura(alphaAura * intensidade * pulso);
    }

    private void DefinirAlphaAura(float alpha)
    {
        if (aura == null) return;
        Color c = aura.color;
        c.a = alpha;
        aura.color = c;
    }

    private bool ApertouMola()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && t.mKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.M);
#endif
    }

    // Mostra o tamanho da aura na aba Scene quando o carro está selecionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
