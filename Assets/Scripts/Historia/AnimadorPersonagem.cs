using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mostra o desenho certo do personagem (direção + passo) e cuida da ordem de desenho:
// quem está mais embaixo na tela aparece na frente. Também mostra os balõezinhos ("!", "?", "...").
public class AnimadorPersonagem : MonoBehaviour
{
    [SerializeField] private PersonagemHistoria personagem;
    [SerializeField] private SpriteRenderer corpo;
    [SerializeField] private SpriteRenderer balao;
    [Tooltip("Na mesma ordem do enum Emote (o primeiro, Nenhum, fica vazio).")]
    [SerializeField] private Sprite[] emotes;
    [SerializeField] private Direcao direcao = Direcao.Baixo;

    private static readonly List<AnimadorPersonagem> todos = new List<AnimadorPersonagem>();

    private int passo;          // 0 parado, 1 ou 2 no meio do passo
    private bool proximoPe;     // alterna o pé a cada passo
    private Coroutine emoteAtual;

    public PersonagemHistoria Personagem => personagem;

    public Direcao Direcao
    {
        get => direcao;
        set { direcao = value; Atualizar(); }
    }

    // O ator deste personagem que está na cena (para as falas mostrarem balõezinhos)
    public static AnimadorPersonagem De(PersonagemHistoria p)
    {
        foreach (AnimadorPersonagem a in todos)
            if (a != null && a.personagem == p) return a;
        return null;
    }

    private void OnEnable() => todos.Add(this);
    private void OnDisable() => todos.Remove(this);

    private void Awake()
    {
        if (corpo == null) corpo = GetComponentInChildren<SpriteRenderer>();
        if (balao != null) balao.gameObject.SetActive(false);
        Atualizar();
    }

    private void LateUpdate()
    {
        // Quanto mais para baixo (linha maior do mapa), mais na frente
        int ordem = 100 + Mathf.RoundToInt(-transform.position.y * 10f) + 5;
        if (corpo != null) corpo.sortingOrder = ordem;
        if (balao != null) balao.sortingOrder = 1000;
    }

    // t vai de 0 a 1 durante um passo: na primeira metade mostra o pé, na segunda volta a ficar parado
    public void ProgressoPasso(float t)
    {
        int novo = t < 0.5f ? (proximoPe ? 1 : 2) : 0;
        if (novo != passo) { passo = novo; Atualizar(); }
    }

    public void TerminouPasso()
    {
        proximoPe = !proximoPe;
        passo = 0;
        Atualizar();
    }

    private void Atualizar()
    {
        if (corpo == null || personagem == null) return;
        Sprite s = personagem.Quadro(direcao, passo);
        if (s != null) corpo.sprite = s;
        corpo.flipX = direcao == Direcao.Esquerda;   // o desenho de lado olha para a direita
    }

    // ---------- Balõezinhos ----------

    public void MostrarEmote(Emote emote, float duracao = 1.1f)
    {
        if (balao == null || emotes == null || (int)emote >= emotes.Length || emotes[(int)emote] == null) return;
        if (emoteAtual != null) StopCoroutine(emoteAtual);
        Sons.TocarEm("Emote_" + emote, transform.position, 1f, 1f, 30f);
        emoteAtual = StartCoroutine(RotinaEmote(emotes[(int)emote], duracao));
    }

    private IEnumerator RotinaEmote(Sprite sprite, float duracao)
    {
        balao.sprite = sprite;
        balao.gameObject.SetActive(true);
        Transform tr = balao.transform;

        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float entrada = Mathf.Clamp01(t / 0.15f);
            tr.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, entrada) * (1f + 0.15f * Mathf.Sin(entrada * Mathf.PI));
            yield return null;
        }
        balao.gameObject.SetActive(false);
        emoteAtual = null;
    }
}
