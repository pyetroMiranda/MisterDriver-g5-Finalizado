using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// O Jason andando a pé dentro de casa, estilo Pokémon antigo:
// setas / WASD andam de quadradinho em quadradinho (um toque rápido só vira), SHIFT corre,
// ENTER / ESPAÇO / E / Z conversa ou examina o que está na frente.
[RequireComponent(typeof(MovimentoGrade))]
public class JogadorCasa : MonoBehaviour
{
    [SerializeField] private float multiplicadorCorrida = 1.8f;
    [Tooltip("Segurar a tecla menos que isso só vira o Jason, sem andar.")]
    [SerializeField] private float tempoParaAndar = 0.09f;

    private MovimentoGrade movimento;
    private MapaCasa mapa;
    private bool ocupado;
    private bool continuando;   // acabou de dar um passo e a tecla continua apertada
    private float segurando;

    public bool Travado { get; set; }
    public Vector2Int Celula => movimento.Celula;
    public MovimentoGrade Movimento => movimento;

    private void Awake()
    {
        movimento = GetComponent<MovimentoGrade>();
        mapa = FindFirstObjectByType<MapaCasa>();
    }

    private void Update()
    {
        if (Travado || ocupado || movimento.Andando || Time.timeScale == 0f ||
            CaixaDialogo.AlgumaAberta || TransicaoTela.Carregando || TelaGaragem.Aberta)
        {
            segurando = 0f;
            return;
        }

        if (ApertouInteragir())
        {
            Interagivel alvo = mapa != null ? mapa.InteragivelEm(movimento.Frente) : null;
            if (alvo != null) StartCoroutine(Rodar(alvo));
            return;
        }

        if (!DirecaoSegurada(out Direcao d))
        {
            segurando = 0f;
            continuando = false;
            return;
        }

        // Toque rápido numa direção nova: só vira (igual ao Pokémon)
        if (!continuando && d != movimento.Olhando)
        {
            movimento.Virar(d);
            segurando = 0f;
            return;
        }

        segurando += Time.deltaTime;
        if (!continuando && segurando < tempoParaAndar) return;

        if (movimento.PodeAndar(d)) StartCoroutine(Andar(d));
        else Esbarrar(d);
    }

    private IEnumerator Andar(Direcao d)
    {
        ocupado = true;
        yield return movimento.Passo(d, Correndo() ? multiplicadorCorrida : 1f);
        ocupado = false;
        continuando = true;

        Interagivel chao = mapa != null ? mapa.InteragivelEm(movimento.Celula) : null;
        if (chao != null) chao.Pisou(this);
    }

    private static float ultimoEsbarrao;

    private void Esbarrar(Direcao d)
    {
        if (Time.unscaledTime - ultimoEsbarrao > 0.4f)
        {
            ultimoEsbarrao = Time.unscaledTime;
            Sons.Tocar("Esbarrao");
        }
        movimento.Virar(d);
        continuando = false;
        segurando = 0f;

        Interagivel alvo = mapa != null ? mapa.InteragivelEm(movimento.Frente) : null;
        if (alvo != null && alvo.InteragirAoEsbarrar) StartCoroutine(Rodar(alvo));
    }

    private IEnumerator Rodar(Interagivel alvo)
    {
        ocupado = true;
        continuando = false;
        yield return alvo.Interagir(this);
        ocupado = false;
    }

    // ---------- Teclado ----------

    private static bool DirecaoSegurada(out Direcao d)
    {
        d = Direcao.Baixo;
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        if (t == null) return false;
        if (t.upArrowKey.isPressed || t.wKey.isPressed) { d = Direcao.Cima; return true; }
        if (t.downArrowKey.isPressed || t.sKey.isPressed) { d = Direcao.Baixo; return true; }
        if (t.leftArrowKey.isPressed || t.aKey.isPressed) { d = Direcao.Esquerda; return true; }
        if (t.rightArrowKey.isPressed || t.dKey.isPressed) { d = Direcao.Direita; return true; }
#else
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) { d = Direcao.Cima; return true; }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) { d = Direcao.Baixo; return true; }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) { d = Direcao.Esquerda; return true; }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) { d = Direcao.Direita; return true; }
#endif
        return false;
    }

    private static bool ApertouInteragir()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && (t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame ||
                             t.spaceKey.wasPressedThisFrame || t.eKey.wasPressedThisFrame ||
                             t.zKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
               Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Z);
#endif
    }

    private static bool Correndo()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && (t.leftShiftKey.isPressed || t.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}
