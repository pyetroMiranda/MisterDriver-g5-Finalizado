using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A TV do quarto do Jason com o videogame: ao examinar, o Jason comenta o TURBO RACER 3000 e mostra o recorde dele.
// Nas falas, {recorde} vira o recorde salvo e {tonhao} o recorde do Tonhão.
public class ObjetoTurbo : Interagivel
{
    [SerializeField] private List<Fala> falas = new List<Fala>();

    public void DefinirFalas(List<Fala> novas) => falas = novas;   // usado pela ferramenta do Editor

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        CaixaDialogo dialogo = CaixaDialogo.Instancia;
        if (dialogo == null) yield break;

        List<Fala> lista = new List<Fala>();
        foreach (Fala f in falas)
        {
            string texto = f.texto.Replace("{recorde}", Videogame.Recorde.ToString())
                                  .Replace("{tonhao}", Videogame.RecordeTonhao.ToString());
            lista.Add(new Fala(f.quem, texto, f.emote, f.tremer));
        }
        yield return dialogo.Conversar(lista);
    }
}
