using UnityEngine;

// Tela da loja: cria um cartão para cada item da lista.
// Para colocar um item novo na loja, basta adicioná-lo na lista "Itens" no Inspector.
public class LojaUI : MonoBehaviour
{
    [SerializeField] private ItemLoja[] itens;
    [SerializeField] private ItemLojaUI modeloCartao;   // um cartão de exemplo (fica escondido)
    [SerializeField] private Transform conteudo;        // onde os cartões são criados (ex.: Content da Scroll View)

    private bool montado;

    public ItemLoja[] Itens => itens;

    private void Start()
    {
        Montar();
        BotoesTesteMoedas.Criar(this);   // + moedas e resetar poderes, só no Editor / Development Build
    }

    private void Montar()
    {
        if (montado) return;

        if (modeloCartao == null || conteudo == null)
        {
            Debug.LogError("Preencha os campos Modelo Cartao e Conteudo da LojaUI.", this);
            enabled = false;
            return;
        }

        modeloCartao.gameObject.SetActive(false);

        foreach (ItemLoja item in itens)
        {
            if (item == null) continue;

            ItemLojaUI cartao = Instantiate(modeloCartao, conteudo);
            cartao.name = "Cartao " + item.Nome;
            cartao.gameObject.SetActive(true);
            cartao.Configurar(item);
        }

        montado = true;
    }
}
