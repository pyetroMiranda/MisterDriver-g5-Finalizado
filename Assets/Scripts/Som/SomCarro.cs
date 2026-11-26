using UnityEngine;

// Sons do carro do jogador (o Sons coloca este componente sozinho no carro de cada cena).
// Só lê o estado público do CarroController e dos poderes, não muda nada no carro:
// - motor em loop que sobe de tom com a velocidade (com "marchas") e ronca mais ao acelerar;
// - chiado de pneu ao derrapar ou puxar o freio de mão; freada forte; bipe de ré (sensor de ré do Kicks);
// - batidas (carro, parede, cone), buraco, perder pontos e a vinheta de carteira suspensa;
// - cada poder: nitro, campo de força, GPS, câmera lenta, ABS, suspensão, advogado e o aviso de poder pronto.
[RequireComponent(typeof(CarroController))]
public class SomCarro : MonoBehaviour
{
    private CarroController carro;
    private CarteiraMotorista carteira;
    private AudioSource motor, derrapagem, nitroLoop;
    private float volMotor, volDerrapagem, volNitro;
    private float tomMotor = 0.75f, derrapagemAtual;

    private bool freandoAntes, freioMaoAntes;
    private float ultimoFreio = -10f, ultimaBatida = -10f, proximoRe;

    private IPoder[] poderes;
    private bool[] ativoAntes, prontoAntes;
    private float[] progressoAntes;
    private Nitro nitro;
    private bool pausado;

    private void Start()
    {
        carro = GetComponent<CarroController>();
        carteira = GetComponent<CarteiraMotorista>();

        motor = Sons.CriarLoop(gameObject, "MotorLoop");
        derrapagem = Sons.CriarLoop(gameObject, "DerrapagemLoop");
        nitroLoop = Sons.CriarLoop(gameObject, "NitroLoop");
        volMotor = Sons.VolumeDe("MotorLoop");
        volDerrapagem = Sons.VolumeDe("DerrapagemLoop");
        volNitro = Sons.VolumeDe("NitroLoop");

        if (carteira != null)
        {
            carteira.aoPerderPontos.AddListener(_ => Sons.Tocar("PerdeuPontos"));
            carteira.aoSuspenderCarteira.AddListener(() => Sons.TocarVinheta(Sons.Vinheta.Derrota));
        }

        poderes = GetComponents<IPoder>();
        ativoAntes = new bool[poderes.Length];
        prontoAntes = new bool[poderes.Length];
        progressoAntes = new float[poderes.Length];
        for (int i = 0; i < poderes.Length; i++)
        {
            ativoAntes[i] = poderes[i].Ativo;
            prontoAntes[i] = poderes[i].Pronto;
            progressoAntes[i] = poderes[i].Progresso;
            if (poderes[i] is Nitro n) nitro = n;
        }
    }

    private void OnDisable()
    {
        if (motor != null) motor.volume = 0f;
        if (derrapagem != null) derrapagem.volume = 0f;
        if (nitroLoop != null) nitroLoop.volume = 0f;
    }

    private void Update()
    {
        if (carro == null) return;

        // Jogo parado (pausa, diálogo, telas): os loops ficam em silêncio
        bool parado = Time.timeScale <= 0.001f || !carro.enabled;
        if (parado != pausado)
        {
            pausado = parado;
            if (pausado) OnDisable();
        }
        if (pausado) return;

        float dt = Time.unscaledDeltaTime;
        float v = carro.VelocidadeAtual;
        float k = Mathf.Clamp01(v / 10f);

        Motor(v, dt);
        Derrapagem(v, k, dt);
        Freios(v, k);
        Re();
        Poderes();
    }

    // ---------- Motor ----------

    private void Motor(float v, float dt)
    {
        if (motor == null) return;
        // Marchas: o tom sobe dentro de cada faixa de velocidade e cai um pouco quando "troca"
        float[] limites = { 0f, 3f, 6f, 8.5f, 13f };
        float alvo = 0.72f;
        if (v > 0.15f)
        {
            int marcha = 0;
            while (marcha < limites.Length - 2 && v > limites[marcha + 1]) marcha++;
            float frac = Mathf.Clamp01((v - limites[marcha]) / (limites[marcha + 1] - limites[marcha]));
            alvo = 0.85f + 0.08f * marcha + 0.55f * frac;
        }
        if (carro.Acelerando) alvo += 0.06f;
        if (nitro != null && nitro.Ativo) alvo += 0.15f;
        tomMotor = Mathf.Lerp(tomMotor, alvo, dt * 6f);

        float volume = 0.45f + 0.35f * Mathf.Clamp01(v / 10f) + (carro.Acelerando ? 0.15f : 0f);
        motor.pitch = tomMotor;
        motor.volume = Mathf.Lerp(motor.volume, volMotor * Mathf.Clamp01(volume), dt * 8f);
    }

    // ---------- Pneus ----------

    private void Derrapagem(float v, float k, float dt)
    {
        if (derrapagem == null) return;
        float alvo = 0f;
        if (carro.EstaDerrapando && v > 2f) alvo = Mathf.Clamp01((v - 2f) / 6f);
        if (carro.FreioDeMaoAtivo && v > 1f) alvo = Mathf.Max(alvo, 0.8f);
        derrapagemAtual = Mathf.MoveTowards(derrapagemAtual, alvo, dt * (alvo > derrapagemAtual ? 8f : 4f));
        derrapagem.volume = volDerrapagem * derrapagemAtual;
        derrapagem.pitch = 0.9f + 0.25f * k;
    }

    private void Freios(float v, float k)
    {
        bool freando = carro.Freando && carro.VelocidadeComSinal > 5f;
        if (freando && !freandoAntes && Time.time - ultimoFreio > 1.2f)
        {
            ultimoFreio = Time.time;
            Sons.Tocar("Freio", 0.4f + 0.6f * k);
        }
        freandoAntes = freando;

        bool freioMao = carro.FreioDeMaoAtivo;
        if (freioMao && !freioMaoAntes && v > 0.5f) Sons.Tocar("FreioDeMao");
        freioMaoAntes = freioMao;
    }

    // Bipe do sensor de ré
    private void Re()
    {
        if (carro.VelocidadeComSinal < -0.3f)
        {
            if (Time.time >= proximoRe)
            {
                proximoRe = Time.time + 0.55f;
                Sons.Tocar("Re");
            }
        }
        else proximoRe = Time.time + 0.15f;
    }

    // ---------- Poderes ----------

    private void Poderes()
    {
        if (poderes == null) return;
        for (int i = 0; i < poderes.Length; i++)
        {
            IPoder p = poderes[i];
            if (p == null || (p is MonoBehaviour mb && mb == null)) continue;
            bool ativo = p.Ativo, pronto = p.Pronto;
            float progresso = p.Progresso;

            if (p is Advogado)
            {
                if (progresso < progressoAntes[i] - 0.001f) Sons.Tocar("Advogado");   // anulou pontos de uma batida
            }
            else if (!(p is Buzina))   // a buzina toca o próprio som
            {
                if (ativo && !ativoAntes[i]) Sons.Tocar(SomAoLigar(p));
                if (!ativo && ativoAntes[i]) { string s = SomAoDesligar(p); if (s != null) Sons.Tocar(s); }
                if (pronto && !prontoAntes[i] && !ativo && p.Comprado && !string.IsNullOrEmpty(p.Tecla)) Sons.Tocar("PoderPronto");
            }

            ativoAntes[i] = ativo;
            prontoAntes[i] = pronto;
            progressoAntes[i] = progresso;
        }

        if (nitroLoop != null)
        {
            float alvo = nitro != null && nitro.Ativo ? volNitro : 0f;
            nitroLoop.volume = Mathf.MoveTowards(nitroLoop.volume, alvo, Time.unscaledDeltaTime * 3f);
        }
    }

    private static string SomAoLigar(IPoder p)
    {
        if (p is Nitro) return "NitroInicio";
        if (p is MolaParaChoque) return "MolaAtivar";
        if (p is GPS) return "GPSLiga";
        if (p is CameraLenta) return "CameraLentaInicio";
        if (p is FreioABS) return "ABS";
        if (p is SuspensaoReforcada) return "Suspensao";
        return "PoderPronto";
    }

    private static string SomAoDesligar(IPoder p)
    {
        if (p is GPS) return "GPSDesliga";
        if (p is CameraLenta) return "CameraLentaFim";
        return null;
    }

    // ---------- Batidas e buracos ----------

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (Time.time - ultimaBatida < 0.25f || carro == null) return;
        float forca = Mathf.Max(colisao.relativeVelocity.magnitude, carro.VelocidadeAtual);
        if (forca < 0.8f) return;
        ultimaBatida = Time.time;

        Collider2D outro = colisao.collider;
        float vol = Mathf.Clamp01(0.35f + forca / 10f);
        if (outro.GetComponentInParent<ConeAutoescola>() != null || outro.GetComponentInParent<ConeObstaculo>() != null)
            Sons.Tocar("Cone", vol);
        else if (outro.GetComponentInParent<CarroTrafego>() != null || outro.GetComponentInParent<CarroRebelde>() != null)
            Sons.Tocar(forca > 5f ? "Batida" : "BatidaLeve", vol);
        else
            Sons.Tocar(forca > 3f ? "BatidaParede" : "BatidaLeve", vol);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (carro == null || carro.VelocidadeAtual < 1f) return;
        if (outro.GetComponentInParent<Buraco>() != null)
            Sons.Tocar("Buraco", Mathf.Clamp01(0.5f + carro.VelocidadeAtual / 12f));
        else if (outro.GetComponentInParent<ConeObstaculo>() != null)
            Sons.Tocar("Cone");
    }
}
