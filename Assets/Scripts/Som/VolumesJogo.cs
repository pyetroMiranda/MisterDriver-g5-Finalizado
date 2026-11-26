using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumesJogo : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderSFX;

    private const string ChaveMusica = "VolumeMusica";
    private const string ChaveSFX = "VolumeSFX";

    private void Start()
    {
        float musica = PlayerPrefs.GetFloat(ChaveMusica, 0.8f);
        float sfx = PlayerPrefs.GetFloat(ChaveSFX, 0.8f);

        sliderMusica.SetValueWithoutNotify(musica);
        sliderSFX.SetValueWithoutNotify(sfx);

        AplicarVolume(ChaveMusica, musica);
        AplicarVolume(ChaveSFX, sfx);

        sliderMusica.onValueChanged.AddListener(AlterarMusica);
        sliderSFX.onValueChanged.AddListener(AlterarSFX);
    }

    private void OnDestroy()
    {
        sliderMusica.onValueChanged.RemoveListener(AlterarMusica);
        sliderSFX.onValueChanged.RemoveListener(AlterarSFX);
    }

    private void AlterarMusica(float valor)
    {
        AplicarVolume(ChaveMusica, valor);
        PlayerPrefs.SetFloat(ChaveMusica, valor);
        PlayerPrefs.Save();
    }

    private void AlterarSFX(float valor)
    {
        AplicarVolume(ChaveSFX, valor);
        PlayerPrefs.SetFloat(ChaveSFX, valor);
        PlayerPrefs.Save();
    }

    private void AplicarVolume(string parametro, float valor)
    {
        // O mixer recebe decibéis: 1 = 0 dB; 0 = silêncio.
        float decibeis = valor <= 0.0001f
            ? -80f
            : Mathf.Log10(valor) * 20f;

        mixer.SetFloat(parametro, decibeis);
    }
}
