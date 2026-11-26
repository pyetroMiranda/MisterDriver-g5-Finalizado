using UnityEngine;

// Câmera da casa: segue o Jason com suavidade e não mostra o lado de fora do mapa.
[RequireComponent(typeof(Camera))]
public class CameraCasa : MonoBehaviour
{
    [SerializeField] private Transform alvo;
    [SerializeField] private float suavizacao = 8f;
    [SerializeField] private Vector2 deslocamento = new Vector2(0f, 0.3f);

    private Camera cam;
    private MapaCasa mapa;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mapa = FindFirstObjectByType<MapaCasa>();
    }

    private void Start()
    {
        if (alvo != null) transform.position = Limitar((Vector2)alvo.position + deslocamento);
    }

    private void LateUpdate()
    {
        if (alvo == null) return;
        Vector3 desejado = Limitar((Vector2)alvo.position + deslocamento);
        transform.position = Vector3.Lerp(transform.position, desejado, 1f - Mathf.Exp(-suavizacao * Time.unscaledDeltaTime));
    }

    private Vector3 Limitar(Vector2 p)
    {
        if (mapa != null && cam.orthographic)
        {
            Rect r = mapa.Limites;
            float meiaAltura = cam.orthographicSize;
            float meiaLargura = meiaAltura * cam.aspect;

            p.x = r.width <= meiaLargura * 2f ? r.center.x : Mathf.Clamp(p.x, r.xMin + meiaLargura, r.xMax - meiaLargura);
            p.y = r.height <= meiaAltura * 2f ? r.center.y : Mathf.Clamp(p.y, r.yMin + meiaAltura, r.yMax - meiaAltura);
        }
        return new Vector3(p.x, p.y, -10f);
    }
}
