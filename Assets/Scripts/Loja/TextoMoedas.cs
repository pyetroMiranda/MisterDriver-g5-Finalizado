using UnityEngine;
using TMPro;

// Mostra o saldo de moedas num texto. Serve no menu, na loja e no HUD das fases.
[RequireComponent(typeof(TMP_Text))]
public class TextoMoedas : MonoBehaviour
{
    [SerializeField] private string prefixo = "";       // ex.: "Moedas: "

    private TMP_Text texto;

    private void Awake()
    {
        texto = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Loja.AoMudarMoedas += Atualizar;
        Atualizar(Loja.Moedas);
    }

    private void OnDisable()
    {
        Loja.AoMudarMoedas -= Atualizar;
    }

    private void Atualizar(int saldo)
    {
        texto.text = prefixo + saldo;
    }
}
