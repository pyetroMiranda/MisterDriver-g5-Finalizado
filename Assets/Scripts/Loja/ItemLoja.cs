using UnityEngine;

// Um item que pode ser comprado na loja (poder, visual, melhoria...).
// Crie novos itens em: Project > botão direito > Create > Mr Driver > Item da Loja
[CreateAssetMenu(fileName = "NovoItem", menuName = "Mr Driver/Item da Loja")]
public class ItemLoja : ScriptableObject
{
    public enum Categoria { Poder, Visual, Melhoria }

    [Tooltip("Identificador único usado para salvar a compra. NÃO mude depois que o jogo for lançado, " +
             "senão quem já comprou perde o item. Se ficar vazio, usa o nome do arquivo.")]
    [SerializeField] private string id;

    [Header("Exibição")]
    [SerializeField] private string nome = "Novo item";
    [SerializeField, TextArea(2, 4)] private string descricao;
    [SerializeField] private Sprite icone;
    [SerializeField] private Categoria categoria = Categoria.Poder;

    [Header("Preço")]
    [SerializeField, Min(0)] private int preco = 100;
    [SerializeField] private bool gratisNoInicio = false;   // o jogador já começa com ele

    [Header("Níveis (melhorias)")]
    [Tooltip("1 = compra uma vez só. Mais que 1 = pode melhorar várias vezes.")]
    [SerializeField, Min(1)] private int niveisMaximos = 1;
    [Tooltip("Quanto o preço sobe a cada nível já comprado.")]
    [SerializeField, Min(0)] private int aumentoPorNivel = 0;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Nome => nome;
    public string Descricao => descricao;
    public Sprite Icone => icone;
    public Categoria Tipo => categoria;
    public int Preco => preco;
    public bool GratisNoInicio => gratisNoInicio;
    public int NiveisMaximos => Mathf.Max(1, niveisMaximos);
    public int AumentoPorNivel => aumentoPorNivel;
    public bool TemNiveis => NiveisMaximos > 1;

    public bool Comprado => Loja.Comprado(this);
    public int Nivel => Loja.Nivel(this);

    private void OnValidate()
    {
        // Preenche o id na primeira vez, para não ficar dependendo do nome do arquivo
        if (string.IsNullOrWhiteSpace(id)) id = name;
    }
}
