using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Todos os sons e músicas do jogo num lugar só (Assets/Resources/BancoSons.asset).
// Montado por Mr Driver > Sons > Gerar sons e músicas; dá para trocar qualquer som ou volume no Inspector.
// O jogo toca os efeitos pelo id: Sons.Tocar("Moeda").
[CreateAssetMenu(menuName = "Mr Driver/Banco de sons")]
public class BancoSons : ScriptableObject
{
    [Serializable]
    public class Efeito
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Tooltip("Variação aleatória do tom a cada vez que toca (0,05 = ±5%).")]
        [Range(0f, 0.3f)] public float variacaoTom = 0.04f;
    }

    [Serializable]
    public class MusicaCena
    {
        [Tooltip("Nome exato da cena.")]
        public string cena;
        public AudioClip musica;
        [Range(0f, 1f)] public float volume = 0.5f;
    }

    [Header("Mixer (volumes do menu Opções)")]
    public AudioMixer mixer;
    public AudioMixerGroup grupoMusica;
    public AudioMixerGroup grupoEfeitos;

    [Header("Efeitos")]
    public List<Efeito> efeitos = new List<Efeito>();

    [Header("Música de cada cena")]
    public List<MusicaCena> musicas = new List<MusicaCena>();

    [Header("Músicas especiais")]
    [Tooltip("Volta do mercado com a Gangue do Escapamento nas ruas (e a baderna dentro do mercado).")]
    public AudioClip musicaPerseguicao;
    [Range(0f, 1f)] public float volumePerseguicao = 0.5f;

    [Header("Vinhetas (tocam uma vez)")]
    public AudioClip vinhetaVitoria;
    public AudioClip vinhetaCarteira;
    public AudioClip vinhetaDerrota;
    [Range(0f, 1f)] public float volumeVinhetas = 0.65f;

    public Efeito Achar(string id)
    {
        foreach (Efeito e in efeitos)
            if (e != null && e.id == id) return e;
        return null;
    }

    public MusicaCena MusicaDe(string cena)
    {
        foreach (MusicaCena m in musicas)
            if (m != null && m.cena == cena) return m;
        return null;
    }

    // A música é do jogo? (para calar os tocadores antigos que ficaram nas cenas)
    public bool EhMusica(AudioClip clip)
    {
        if (clip == null) return false;
        if (clip == musicaPerseguicao || clip == vinhetaVitoria || clip == vinhetaCarteira || clip == vinhetaDerrota) return true;
        foreach (MusicaCena m in musicas)
            if (m != null && m.musica == clip) return true;
        return false;
    }
}
