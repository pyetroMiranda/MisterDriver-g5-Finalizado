using System.Collections;
using UnityEngine;

// Algo da casa com que o Jason pode interagir (olhando para ele e apertando ENTER / ESPAÇO / E / Z).
// Ocupa uma ou mais células da MapaCasa (vazio = a célula onde o objeto está).
public abstract class Interagivel : MonoBehaviour
{
    [SerializeField] protected bool bloqueia = true;
    [SerializeField] protected Vector2Int[] celulas = new Vector2Int[0];
    [Tooltip("Interage sozinho quando o Jason tenta andar para cima dele (ex.: a porta).")]
    [SerializeField] private bool interagirAoEsbarrar;

    protected MapaCasa mapa;

    public virtual bool Bloqueia => bloqueia;
    public bool InteragirAoEsbarrar => interagirAoEsbarrar;

    protected virtual void Awake()
    {
        mapa = FindFirstObjectByType<MapaCasa>();
    }

    protected virtual void OnEnable() { if (mapa != null) mapa.Registrar(this); }
    protected virtual void OnDisable() { if (mapa != null) mapa.Remover(this); }

    public virtual bool Ocupa(Vector2Int c)
    {
        if (celulas == null || celulas.Length == 0) return MapaCasa.ParaCelula(transform.position) == c;
        foreach (Vector2Int celula in celulas)
            if (celula == c) return true;
        return false;
    }

    public abstract IEnumerator Interagir(JogadorCasa jogador);

    // Usados pela ferramenta do Editor que monta a casa
    public void DefinirCelulas(Vector2Int[] novas) => celulas = novas;
    public void DefinirBloqueio(bool bloquear, bool esbarrar) { bloqueia = bloquear; interagirAoEsbarrar = esbarrar; }

    // O Jason terminou um passo em cima deste objeto (só acontece se ele não bloquear)
    public virtual void Pisou(JogadorCasa jogador) { }

    // Mostra as células na aba Scene
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
        if (celulas == null || celulas.Length == 0)
            Gizmos.DrawWireCube(MapaCasa.ParaMundo(MapaCasa.ParaCelula(transform.position)), Vector3.one * 0.9f);
        else
            foreach (Vector2Int c in celulas) Gizmos.DrawWireCube(MapaCasa.ParaMundo(c), Vector3.one * 0.9f);
    }
}
