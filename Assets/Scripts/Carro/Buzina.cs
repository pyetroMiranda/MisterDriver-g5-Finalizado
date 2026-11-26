using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Buzina : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;      // item da loja que libera este poder
    [SerializeField] private bool comprado = true;   // só vale se Item Loja estiver vazio (para testar)

    [Header("Efeito")]
    [SerializeField] private float alcance = 15f;       // raio em que os carros ouvem a buzina
    [SerializeField] private float duracaoParada = 3f;    // segundos que o trânsito fica parado
    [SerializeField] private float recarga = 15f;         // segundos até poder buzinar de novo

    [Header("Som (opcional)")]
    [SerializeField] private AudioSource fonteAudio;
    [SerializeField] private AudioClip somBuzina;

    [Header("Eventos")]
    public UnityEvent aoAtivar;
    public UnityEvent aoFicarPronto;

    private float tempoRecarga;

    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public string Nome => "Buzina";
    public string Tecla => "H";
    public string Info => "";
    public bool Ativo => false;
    public bool Pronto => Comprado && tempoRecarga <= 0f;

    // 0 a 1: quanto da recarga já foi feita (1 = pronta)
    public float Progresso => 1f - tempoRecarga / Mathf.Max(recarga, 0.01f);

    private void Update()
    {
        // Não buzina com o jogo pausado (tutorial, pause, game over)
        if (Time.timeScale == 0f) return;

        if (tempoRecarga > 0f)
        {
            tempoRecarga -= Time.deltaTime;
            if (tempoRecarga <= 0f)
            {
                tempoRecarga = 0f;
                aoFicarPronto?.Invoke();
            }
        }

        if (ApertouBuzina())
        {
            if (Pronto) Buzinar();
            else if (!Comprado) Debug.Log("Buzina ainda não foi comprada na loja");
            else Debug.Log("Buzina em recarga: faltam " + tempoRecarga.ToString("0.0") + " s");
        }
    }

    private void Buzinar()
    {
        tempoRecarga = recarga;

        if (fonteAudio != null && somBuzina != null)
            fonteAudio.PlayOneShot(somBuzina);

        Vector2 posicao = transform.position;
        CarroTrafego[] carros = FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None);

        int parados = 0;
        float maisProximo = float.MaxValue;
        foreach (CarroTrafego carro in carros)
        {
            float distancia = Vector2.Distance(posicao, carro.transform.position);
            maisProximo = Mathf.Min(maisProximo, distancia);

            if (distancia <= alcance)
            {
                carro.Parar(duracaoParada);
                parados++;
            }
        }

        Debug.Log("Buzina! Carros na cena: " + carros.Length + " | parados: " + parados +
                  " | mais próximo a " + maisProximo.ToString("0.0") + " (alcance " + alcance + ")");

        aoAtivar?.Invoke();
    }

    private bool ApertouBuzina()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && t.hKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.H);
#endif
    }

    // Mostra o alcance da buzina na aba Scene quando o carro está selecionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, alcance);
    }
}
