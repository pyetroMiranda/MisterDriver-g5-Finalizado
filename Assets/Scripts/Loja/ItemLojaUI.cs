using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Um "cartão" de item na tela da loja. Fica no modelo de cartão que a LojaUI copia para cada item.
public class ItemLojaUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text textoNome;
    [SerializeField] private TMP_Text textoDescricao;   // opcional
    [SerializeField] private TMP_Text textoPreco;       // opcional

    [Header("Imagem (opcional)")]
    [SerializeField] private Image imagemIcone;

    [Header("Botão")]
    [SerializeField] private Button botaoComprar;
    [SerializeField] private TMP_Text textoBotao;

    [Header("Textos do botão")]
    [SerializeField] private string textoComprar = "Comprar";
    [SerializeField] private string textoComprado = "Comprado";
    [SerializeField] private string textoSemMoedas = "Sem moedas";
    [SerializeField] private string sufixoPreco = " moedas";
    [SerializeField] private string textoMelhorar = "Melhorar";
    [SerializeField] private string textoMaximo = "Máximo";

    private ItemLoja item;

    public void Configurar(ItemLoja novoItem)
    {
        item = novoItem;

        if (textoDescricao != null) textoDescricao.text = item.Descricao;

        if (imagemIcone != null)
        {
            imagemIcone.sprite = item.Icone;
            imagemIcone.enabled = item.Icone != null;
        }

        if (botaoComprar != null)
        {
            botaoComprar.onClick.RemoveListener(Comprar);
            botaoComprar.onClick.AddListener(Comprar);
        }

        Atualizar();
    }

    private void OnEnable()
    {
        Loja.AoMudarMoedas += AoMudarMoedas;
        Loja.AoComprar += AoComprar;
        Atualizar();
    }

    private void OnDisable()
    {
        Loja.AoMudarMoedas -= AoMudarMoedas;
        Loja.AoComprar -= AoComprar;
    }

    private void AoMudarMoedas(int saldo) => Atualizar();
    private void AoComprar(ItemLoja comprado) => Atualizar();

    private void Comprar()
    {
        if (item != null) Loja.Comprar(item);
    }

    private void Atualizar()
    {
        if (item == null) return;

        int nivel = item.Nivel;
        bool noMaximo = Loja.NoMaximo(item);
        int preco = Loja.PrecoProximo(item);

        // Nome com o nível, para melhorias (ex.: "Motor  Nv 2/5")
        if (textoNome != null)
            textoNome.text = item.TemNiveis ? item.Nome + "  <size=75%>Nv " + nivel + "/" + item.NiveisMaximos + "</size>" : item.Nome;

        if (textoPreco != null) textoPreco.text = noMaximo ? "" : preco + sufixoPreco;

        if (botaoComprar == null) return;
        botaoComprar.interactable = Loja.PodeComprar(item);

        if (textoBotao != null)
        {
            if (noMaximo) textoBotao.text = item.TemNiveis ? textoMaximo : textoComprado;
            else if (Loja.Moedas < preco) textoBotao.text = textoSemMoedas;
            else textoBotao.text = nivel > 0 ? textoMelhorar : textoComprar;
        }
    }
}
