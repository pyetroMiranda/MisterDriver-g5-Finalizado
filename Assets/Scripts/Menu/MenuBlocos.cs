using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Menu principal em blocos, tudo no centro da tela (cada bloco é um botão).
// Mantém sempre um bloco selecionado (setas / WASD navegam, ENTER confirma) e escreve
// na barra de baixo a descrição do bloco selecionado. Montado por Mr Driver > Menu > Montar menu em blocos.
public class MenuBlocos : MonoBehaviour
{
    [Header("Blocos (montados pela ferramenta)")]
    [SerializeField] private BlocoMenu primeiro;
    [SerializeField] private BlocoMenu blocoJogar;
    [SerializeField] private BlocoMenu blocoLoja;
    [SerializeField] private TMP_Text textoDescricao;
    [SerializeField] private CanvasGroup grupo;

    [Header("Som ao trocar de bloco (opcional)")]
    [SerializeField] private AudioSource fonte;
    [SerializeField, Range(0f, 1f)] private float volume = 0.25f;

    private BlocoMenu ultimo;
    private AudioClip tique;
    private bool tocarSom;

    private void Awake()
    {
        tique = CriarTique();
    }

    private void OnEnable()
    {
        AtualizarTextos();
        StartCoroutine(Entrar());
    }

    private IEnumerator Entrar()
    {
        tocarSom = false;
        yield return null;   // espera o EventSystem e os blocos ficarem prontos
        Selecionar(ultimo != null && ultimo.isActiveAndEnabled ? ultimo : primeiro);
        tocarSom = true;

        if (grupo == null) yield break;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.SmoothStep(0f, 1f, t / 0.3f);
            yield return null;
        }
        grupo.alpha = 1f;
    }

    private void Update()
    {
        // Clique fora de um bloco não pode deixar o menu sem nada selecionado
        EventSystem es = EventSystem.current;
        if (es != null && (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy))
            Selecionar(ultimo != null ? ultimo : primeiro);
    }

    private void Selecionar(BlocoMenu bloco)
    {
        if (bloco == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(bloco.gameObject);
    }

    public void Focou(BlocoMenu bloco)
    {
        if (textoDescricao != null) textoDescricao.text = bloco.Descricao;
        if (tocarSom && bloco != ultimo && fonte != null && tique != null) fonte.PlayOneShot(tique, volume);
        ultimo = bloco;
    }

    private void AtualizarTextos()
    {
        if (blocoJogar != null)
        {
            string sub;
            if (ProgressoHistoria.HistoriaConcluida) sub = "ESCOLHER NÍVEL";
            else if (ProgressoHistoria.Comecou) sub = "CONTINUAR HISTÓRIA";
            else sub = "NOVA HISTÓRIA";
            blocoJogar.DefinirSubtitulo(sub);
        }
        if (blocoLoja != null) blocoLoja.DefinirSubtitulo(Loja.Moedas + " MOEDAS");
    }

    // "Tic" curtinho criado pelo código
    private static AudioClip CriarTique()
    {
        const int taxa = 22050;
        int n = Mathf.RoundToInt(taxa * 0.03f);
        float[] dados = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / taxa;
            dados[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1250f * t)) * (1f - (float)i / n) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("TiqueMenu", n, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }
}
