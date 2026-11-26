using System.Collections;
using UnityEngine;

// Cone do pátio da autoescola. O carro consegue empurrar e derrubar; cada cone derrubado é uma falta
// na MissaoExame. Depois de um tempo parado (e com o carro longe) ele volta sozinho para o lugar.
[RequireComponent(typeof(Rigidbody2D))]
public class ConeAutoescola : MonoBehaviour
{
    [SerializeField] private float tempoParaVoltar = 3.5f;
    [SerializeField] private float distanciaLivre = 4f;     // o carro precisa estar longe para o cone voltar

    private Rigidbody2D corpo;
    private Vector2 posicaoInicial;
    private float rotacaoInicial;
    private bool derrubado;
    private bool voltando;
    private float ultimoToque;
    private MissaoExame missao;
    private Transform carro;

    private void Start()
    {
        corpo = GetComponent<Rigidbody2D>();
        posicaoInicial = corpo.position;
        rotacaoInicial = corpo.rotation;
        missao = FindFirstObjectByType<MissaoExame>();
        CarroController c = FindFirstObjectByType<CarroController>();
        if (c != null) carro = c.transform;
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.gameObject.GetComponentInParent<CarroController>() == null) return;

        ultimoToque = Time.time;
        if (derrubado || voltando) return;

        derrubado = true;
        if (missao != null) missao.RegistrarFalta(true);
    }

    private void OnCollisionStay2D(Collision2D colisao)
    {
        if (colisao.gameObject.GetComponentInParent<CarroController>() != null) ultimoToque = Time.time;
    }

    private void Update()
    {
        if (!derrubado || voltando) return;
        if (Time.time - ultimoToque < tempoParaVoltar) return;
        if (corpo.linearVelocity.sqrMagnitude > 0.01f) return;
        if (carro != null && Vector2.Distance(carro.position, posicaoInicial) < distanciaLivre) return;

        StartCoroutine(Voltar());
    }

    private IEnumerator Voltar()
    {
        voltando = true;
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.linearVelocity = Vector2.zero;
        corpo.angularVelocity = 0f;

        Vector2 de = corpo.position;
        float rotDe = corpo.rotation;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.6f);
            corpo.MovePosition(Vector2.Lerp(de, posicaoInicial, k));
            corpo.MoveRotation(Mathf.LerpAngle(rotDe, rotacaoInicial, k));
            yield return new WaitForFixedUpdate();
        }

        corpo.position = posicaoInicial;
        corpo.rotation = rotacaoInicial;
        corpo.bodyType = RigidbodyType2D.Dynamic;
        derrubado = false;
        voltando = false;
    }
}
