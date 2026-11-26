using UnityEngine;

// Um ponto da rota dos pedestres (filho de uma RotaPedestres).
public class PontoPedestre : MonoBehaviour
{
    [Tooltip("Marque se o caminho deste ponto até o PRÓXIMO atravessa a rua.\n" +
             "Os pedestres esperam aqui até não ter carro perto e atravessam mais rápido.")]
    public bool atravessarAteOProximo;
}
