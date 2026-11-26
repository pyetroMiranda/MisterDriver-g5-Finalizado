using UnityEngine;

// Na tela de Níveis: mostra o bloco do minijogo TURBO RACER 3000 só depois que a história foi terminada
// (antes disso, aparece a dica de sempre no lugar). Montado por Mr Driver > Menu > Montar telas do menu.
public class AlternarMinijogo : MonoBehaviour
{
    [SerializeField] private GameObject minijogo;
    [SerializeField] private GameObject dica;

    private void OnEnable()
    {
        bool liberado = Videogame.Desbloqueado;
        if (minijogo != null) minijogo.SetActive(liberado);
        if (dica != null) dica.SetActive(!liberado);
    }

    public void Abrir() => Videogame.AbrirMinijogo();
}
