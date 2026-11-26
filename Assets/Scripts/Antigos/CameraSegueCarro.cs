using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSegueCarro : MonoBehaviour
{
    [SerializeField] private Transform carro;
    [SerializeField] private SpriteRenderer mapa;
    [SerializeField] private float suavizacao = 0.15f;
    [SerializeField] private bool acompanharRotacao = true;
    [SerializeField] private float suavizacaoRotacao = 0.25f;

    private Camera cameraJogo;
    private float velocidadeRotacao;
    private Vector3 velocidadeSuavizacao;

    private void Start()
    {
        cameraJogo = GetComponent<Camera>();

        if (carro == null || mapa == null)
        {
            Debug.LogError(
                "Preencha os campos Carro e Mapa da câmera.",
                this
            );

            enabled = false;
            return;
        }

        if (!cameraJogo.orthographic)
        {
            Debug.LogError("A câmera precisa ser Orthographic.", this);
            enabled = false;
            return;
        }

        transform.rotation = Quaternion.Euler(
            0f, 0f,
            acompanharRotacao ? carro.eulerAngles.z : 0f
        );

        transform.position = LimitarPosicao(new Vector3(
            carro.position.x, carro.position.y, -10f
        ));
    }

    private void LateUpdate()
    {
        AtualizarRotacao();

        Vector3 destino = LimitarPosicao(new Vector3(
            carro.position.x, carro.position.y, -10f
        ));

        Vector3 posicaoSuave = Vector3.SmoothDamp(
            transform.position,
            destino,
            ref velocidadeSuavizacao,
            suavizacao
        );

        // Confere novamente depois da suavização,
        // pois a rotação muda o espaço ocupado pela visão.
        Vector3 posicaoLimitada = LimitarPosicao(posicaoSuave);

        if (!Mathf.Approximately(posicaoSuave.x, posicaoLimitada.x))
            velocidadeSuavizacao.x = 0f;

        if (!Mathf.Approximately(posicaoSuave.y, posicaoLimitada.y))
            velocidadeSuavizacao.y = 0f;

        transform.position = posicaoLimitada;
    }

    private Vector3 LimitarPosicao(Vector3 posicao)
    {
        Bounds limites = mapa.bounds;

        float meiaAltura = cameraJogo.orthographicSize;
        float meiaLargura = meiaAltura * cameraJogo.aspect;

        float angulo = transform.eulerAngles.z * Mathf.Deg2Rad;
        float seno = Mathf.Abs(Mathf.Sin(angulo));
        float cosseno = Mathf.Abs(Mathf.Cos(angulo));

        float margemX = meiaLargura * cosseno + meiaAltura * seno;
        float margemY = meiaLargura * seno + meiaAltura * cosseno;

        float minimoX = limites.min.x + margemX;
        float maximoX = limites.max.x - margemX;
        float minimoY = limites.min.y + margemY;
        float maximoY = limites.max.y - margemY;

        posicao.x = minimoX <= maximoX
            ? Mathf.Clamp(posicao.x, minimoX, maximoX)
            : limites.center.x;

        posicao.y = minimoY <= maximoY
            ? Mathf.Clamp(posicao.y, minimoY, maximoY)
            : limites.center.y;

        return posicao;
    }

    private void AtualizarRotacao()
    {
        float destino = acompanharRotacao
            ? carro.eulerAngles.z
            : 0f;

        float angulo = Mathf.SmoothDampAngle(
            transform.eulerAngles.z,
            destino,
            ref velocidadeRotacao,
            suavizacaoRotacao
        );

        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }
}