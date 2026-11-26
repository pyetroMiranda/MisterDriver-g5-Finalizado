using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SomBotoes : MonoBehaviour
{
    [SerializeField] private AudioClip somClique;

    private AudioSource fonte;

    private void Awake()
    {
        fonte = GetComponent<AudioSource>();
    }

    public void TocarClique()
    {
        if (somClique != null)
            fonte.PlayOneShot(somClique);
    }

    public void TocarCliqueTrocaCena()
    {
        if (somClique == null)
            return;

        GameObject objeto = new GameObject("CliqueTrocaCena");
        DontDestroyOnLoad(objeto);

        AudioSource audio = objeto.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;
        audio.volume = fonte.volume;
        audio.outputAudioMixerGroup = fonte.outputAudioMixerGroup;
        audio.clip = somClique;

        SomBotoes temporario = objeto.AddComponent<SomBotoes>();

        audio.Play();
        temporario.StartCoroutine(
            temporario.RemoverDepois(somClique.length)
        );
    }

    private IEnumerator RemoverDepois(float duracao)
    {
        yield return new WaitForSecondsRealtime(duracao + 0.1f);
        Destroy(gameObject);
    }
}