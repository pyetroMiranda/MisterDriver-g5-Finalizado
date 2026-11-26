using UnityEngine;

// Aplica no carro as melhorias compradas na loja (cada uma pode ter vários níveis).
// Roda antes dos outros scripts, para o carro já começar o nível melhorado.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CarroController))]
public class MelhoriasCarro : MonoBehaviour
{
    [Header("Itens da loja (por nível comprado)")]
    [SerializeField] private ItemLoja motor;             // + velocidade máxima
    [SerializeField] private ItemLoja aceleracao;        // + aceleração
    [SerializeField] private ItemLoja freios;            // + força do freio
    [SerializeField] private ItemLoja pneus;             // + curva
    [SerializeField] private ItemLoja nitroLongo;        // + segundos de nitro
    [SerializeField] private ItemLoja nitroRecarga;      // - segundos de recarga do nitro
    [SerializeField] private ItemLoja cursoDirecao;      // + pontos na carteira

    [Header("Quanto cada nível dá")]
    [SerializeField] private float velocidadePorNivel = 0.08f;   // +8%
    [SerializeField] private float aceleracaoPorNivel = 0.15f;   // +15%
    [SerializeField] private float freioPorNivel = 0.25f;        // +25%
    [SerializeField] private float curvaPorNivel = 0.08f;        // +8%
    [SerializeField] private float segundosNitroPorNivel = 1f;
    [SerializeField] private float recargaNitroPorNivel = 5f;
    [SerializeField] private int pontosPorNivel = 1;

    private void Awake()
    {
        CarroController carro = GetComponent<CarroController>();
        carro.MelhoriaVelocidade = 1f + velocidadePorNivel * Nivel(motor);
        carro.MelhoriaAceleracao = 1f + aceleracaoPorNivel * Nivel(aceleracao);
        carro.MelhoriaFreio = 1f + freioPorNivel * Nivel(freios);
        carro.MelhoriaCurva = 1f + curvaPorNivel * Nivel(pneus);

        Nitro nitro = GetComponent<Nitro>();
        if (nitro != null)
        {
            nitro.BonusDuracao = segundosNitroPorNivel * Nivel(nitroLongo);
            nitro.ReducaoRecarga = recargaNitroPorNivel * Nivel(nitroRecarga);
        }

        CarteiraMotorista carteira = GetComponent<CarteiraMotorista>();
        if (carteira != null) carteira.DefinirBonusPontos(pontosPorNivel * Nivel(cursoDirecao));
    }

    private static int Nivel(ItemLoja item) => item != null ? Loja.Nivel(item) : 0;
}
