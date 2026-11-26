using UnityEngine;

public class RastrosPneu : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarroController carro;

    [Header("Pneus traseiros")]
    [SerializeField] private Transform[] pneus;

    [Header("Marca")]
    [SerializeField] private GameObject marcaPrefab;
    [SerializeField] private float distanciaEntreMarcas = 0.1f;
    [SerializeField] private Transform pastaMarcas;   // opcional

    [Header("Condições")]
    [SerializeField] private float velocidadeMinima = 0.4f;
    [SerializeField, Range(0f, 1f)] private float volanteMinimo = 0.5f;
    [SerializeField] private bool marcarTambemEmDerrapagem = true;

    private Vector3[] ultimasPosicoes;

    private void Start()
    {
        ultimasPosicoes = new Vector3[pneus.Length];
        for (int i = 0; i < pneus.Length; i++)
            ultimasPosicoes[i] = pneus[i].position;
    }

    private void LateUpdate()
    {
        bool freioEmCurva = carro.FreioDeMaoAtivo
                            && carro.VelocidadeAtual > velocidadeMinima
                            && Mathf.Abs(carro.Volante) >= volanteMinimo;
        bool derrapando = marcarTambemEmDerrapagem && carro.EstaDerrapando;
        bool deveMarcar = freioEmCurva || derrapando;

        for (int i = 0; i < pneus.Length; i++)
        {
            Vector3 pos = pneus[i].position;

            if (deveMarcar && Vector3.Distance(pos, ultimasPosicoes[i]) >= distanciaEntreMarcas)
            {
                GameObject marca = Instantiate(marcaPrefab, pos, pneus[i].rotation, pastaMarcas);
                marca.transform.position = new Vector3(pos.x, pos.y, 0f);
                ultimasPosicoes[i] = pos;
            }
            else if (!deveMarcar)
            {
                ultimasPosicoes[i] = pos;
            }
        }
    }
}