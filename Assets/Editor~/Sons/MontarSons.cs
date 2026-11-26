using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

// Gera todos os efeitos sonoros e as músicas (SintetizadorSons + CompositorMusicas), importa os .wav
// e monta o banco Assets/Resources/BancoSons.asset, que o jogo usa sozinho em qualquer cena (Sons.cs).
// Também liga a buzina e a mola do carro do jogador aos sons novos.
// Rodar de novo refaz os .wav; volumes e trocas de música feitos no banco pelo Inspector são mantidos.
// Menu: Mr Driver > Sons > Gerar sons e músicas
public static class MontarSons
{
    private const string PastaEfeitos = "Assets/Audios/Gerados/Efeitos";
    private const string PastaMusicas = "Assets/Audios/Gerados/Musicas";
    private const string CaminhoBanco = "Assets/Resources/BancoSons.asset";
    private const string CaminhoMixer = "Assets/MixerMrDriver.mixer";
    private const string CaminhoCarro = "Assets/Prefabs/CarroNissan_0.prefab";

    // Músicas que já existiam no projeto e continuam valendo
    private const string MusicaMenuAntiga = "Assets/Audios/Musicas/AudioMenu.mp3";
    private const string MusicaTutorialAntiga = "Assets/Audios/Musicas/Audio Fase Tutorial.mp3";

    [MenuItem("Mr Driver/Sons/Gerar sons e músicas")]
    private static void Menu()
    {
        EditorUtility.DisplayDialog("Sons", Montar(), "OK");
    }

    public static string Montar()
    {
        CriarPasta(PastaEfeitos);
        CriarPasta(PastaMusicas);
        CriarPasta("Assets/Resources");

        // ---------- Efeitos ----------
        List<SintetizadorSons.Efeito> efeitos = SintetizadorSons.Todos();
        Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        for (int i = 0; i < efeitos.Count; i++)
        {
            SintetizadorSons.Efeito e = efeitos[i];
            EditorUtility.DisplayProgressBar("Sons", "Efeito " + e.id, i / (float)efeitos.Count * 0.5f);
            clips[e.id] = Salvar(PastaEfeitos, e.id, e.gerar(), false);
        }

        // ---------- Músicas ----------
        List<CompositorMusicas.Musica> musicas = CompositorMusicas.Todas();
        for (int i = 0; i < musicas.Count; i++)
        {
            CompositorMusicas.Musica m = musicas[i];
            EditorUtility.DisplayProgressBar("Sons", "Música " + m.id, 0.5f + i / (float)musicas.Count * 0.5f);
            clips[m.id] = Salvar(PastaMusicas, m.id, CompositorMusicas.Renderizar(m), true);
        }
        EditorUtility.ClearProgressBar();

        // ---------- Banco ----------
        BancoSons banco = AssetDatabase.LoadAssetAtPath<BancoSons>(CaminhoBanco);
        if (banco == null)
        {
            banco = ScriptableObject.CreateInstance<BancoSons>();
            AssetDatabase.CreateAsset(banco, CaminhoBanco);
        }

        banco.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(CaminhoMixer);
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(CaminhoMixer))
        {
            if (!(obj is AudioMixerGroup g)) continue;
            if (g.name == "Musica") banco.grupoMusica = g;
            else if (g.name == "SFX") banco.grupoEfeitos = g;
        }

        // Efeitos: mantém volume/variação que o usuário já ajustou
        Dictionary<string, BancoSons.Efeito> antes = new Dictionary<string, BancoSons.Efeito>();
        foreach (BancoSons.Efeito e in banco.efeitos) if (e != null && !string.IsNullOrEmpty(e.id)) antes[e.id] = e;
        banco.efeitos = new List<BancoSons.Efeito>();
        foreach (SintetizadorSons.Efeito e in efeitos)
        {
            bool novo = !antes.TryGetValue(e.id, out BancoSons.Efeito velho);
            banco.efeitos.Add(new BancoSons.Efeito
            {
                id = e.id,
                clip = clips[e.id],
                volume = novo ? e.volume : velho.volume,
                variacaoTom = novo ? Variacao(e) : velho.variacaoTom,
            });
        }

        // Música de cada cena (as que o usuário trocou no banco continuam)
        AudioClip menu = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicaMenuAntiga);
        AudioClip tutorial = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicaTutorialAntiga);
        DefinirMusica(banco, ProgressoHistoria.CenaMenu, menu, 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaTutorial, tutorial, 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaCasa, clips["MusicaCasa"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaGaragem, clips["MusicaCasa"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaAutoescola, clips["MusicaAutoescola"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaExame, clips["MusicaExame"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.PrimeiroNivel, clips["MusicaCidade"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaMercado, clips["MusicaMercado"], 0.5f, clips);
        DefinirMusica(banco, ProgressoHistoria.CenaVideogame, clips["MusicaTurboRacer"], 0.22f, clips);

        banco.musicaPerseguicao = clips["MusicaPerseguicao"];
        banco.vinhetaVitoria = clips["VinhetaVitoria"];
        banco.vinhetaCarteira = clips["VinhetaCarteira"];
        banco.vinhetaDerrota = clips["VinhetaDerrota"];
        EditorUtility.SetDirty(banco);

        string carro = LigarCarro(clips);
        AssetDatabase.SaveAssets();

        return "Sons prontos!\n\n" +
               "• " + efeitos.Count + " efeitos em " + PastaEfeitos + "\n" +
               "• " + musicas.Count + " músicas e vinhetas em " + PastaMusicas + "\n" +
               "• Banco em " + CaminhoBanco + " (troque sons e volumes por lá)\n" +
               "• Menu e Tutorial continuam com as músicas que já existiam\n" +
               carro +
               "\nNão precisa colocar nada nas cenas: o som liga sozinho ao dar Play.";
    }

    private static float Variacao(SintetizadorSons.Efeito e)
    {
        if (e.loop || e.id.StartsWith("Voz")) return 0f;
        if (e.id.StartsWith("Passo") || e.id.StartsWith("Batida") || e.id == "Ai" || e.id == "Latas") return 0.08f;
        if (e.id == "Clique" || e.id == "Navegar") return 0.02f;
        return 0.03f;
    }

    private static void DefinirMusica(BancoSons banco, string cena, AudioClip padrao, float volume, Dictionary<string, AudioClip> gerados)
    {
        BancoSons.MusicaCena m = banco.MusicaDe(cena);
        if (m == null)
        {
            banco.musicas.Add(new BancoSons.MusicaCena { cena = cena, musica = padrao, volume = volume });
            return;
        }
        // Se a música atual é uma das geradas (ou está vazia), atualiza; se o usuário escolheu outra, respeita
        if (m.musica == null || gerados.ContainsValue(m.musica)) m.musica = padrao;
    }

    // A buzina e a mola já tinham espaço para som no carro: agora usam os novos
    private static string LigarCarro(Dictionary<string, AudioClip> clips)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoCarro);
        if (raiz == null) return "";
        try
        {
            AudioSource fonte = raiz.GetComponent<AudioSource>();
            if (fonte == null) fonte = raiz.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.volume = 0.8f;
            BancoSons banco = AssetDatabase.LoadAssetAtPath<BancoSons>(CaminhoBanco);
            if (banco != null) fonte.outputAudioMixerGroup = banco.grupoEfeitos;

            int ligados = 0;
            Buzina buzina = raiz.GetComponent<Buzina>();
            if (buzina != null)
            {
                SerializedObject d = new SerializedObject(buzina);
                d.FindProperty("fonteAudio").objectReferenceValue = fonte;
                d.FindProperty("somBuzina").objectReferenceValue = clips["Buzina"];
                d.ApplyModifiedPropertiesWithoutUndo();
                ligados++;
            }
            MolaParaChoque mola = raiz.GetComponent<MolaParaChoque>();
            if (mola != null)
            {
                SerializedObject d = new SerializedObject(mola);
                d.FindProperty("fonteAudio").objectReferenceValue = fonte;
                d.FindProperty("somEmpurrao").objectReferenceValue = clips["MolaEmpurrao"];
                d.ApplyModifiedPropertiesWithoutUndo();   // o som de ligar fica com o SomCarro
                ligados++;
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoCarro);
            return "• Carro do jogador: buzina e mola ligadas aos sons novos (" + ligados + ")\n";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // ---------- Arquivos ----------

    private static AudioClip Salvar(string pasta, string nome, float[] amostras, bool musica)
    {
        string caminho = pasta + "/" + nome + ".wav";
        File.WriteAllBytes(caminho, SintetizadorSons.Wav(amostras));
        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        AudioImporter imp = (AudioImporter)AssetImporter.GetAtPath(caminho);
        if (imp != null)
        {
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            if (musica)
            {
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                imp.loadInBackground = true;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(caminho);
    }

    private static void CriarPasta(string pasta)
    {
        string[] partes = pasta.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proxima = atual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(proxima)) AssetDatabase.CreateFolder(atual, partes[i]);
            atual = proxima;
        }
    }
}
