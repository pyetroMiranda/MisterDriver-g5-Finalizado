using UnityEngine;

// Coloca pedestres espalhados pela rota quando o nível começa.
[RequireComponent(typeof(RotaPedestres))]
public class GeradorPedestres : MonoBehaviour
{
    [SerializeField] private Pedestre prefab;
    [SerializeField, Min(0)] private int quantidade = 5;
    [SerializeField] private Vector2 velocidade = new Vector2(0.9f, 1.5f);   // mínima e máxima

    private static Transform pasta;   // um objeto só para guardar todos os pedestres da cena

    private void Start()
    {
        RotaPedestres rota = GetComponent<RotaPedestres>();
        rota.Atualizar();

        if (prefab == null || rota.Quantidade < 2)
        {
            Debug.LogWarning("GeradorPedestres: falta o prefab ou a rota tem menos de 2 pontos.", this);
            return;
        }

        if (pasta == null) pasta = new GameObject("Pedestres (gerados)").transform;

        int trechos = rota.IdaEVolta ? rota.Quantidade - 1 : rota.Quantidade;

        for (int i = 0; i < quantidade; i++)
        {
            // Começa num trecho de calçada (nunca no meio da rua)
            int trecho = Random.Range(0, trechos);
            for (int tentativa = 0; tentativa < 10 && rota.Marcado(trecho); tentativa++)
                trecho = Random.Range(0, trechos);

            int seguinte = (trecho + 1) % rota.Quantidade;
            Vector2 posicao = Vector2.Lerp(rota.Posicao(trecho), rota.Posicao(seguinte), Random.value);

            int sentido = Random.value < 0.5f ? 1 : -1;
            int alvo = sentido > 0 ? seguinte : trecho;

            Pedestre pessoa = Instantiate(prefab, posicao, Quaternion.identity, pasta);
            pessoa.name = "Pedestre " + (i + 1);
            pessoa.Iniciar(rota, alvo, sentido, Random.Range(velocidade.x, velocidade.y));
        }
    }
}
