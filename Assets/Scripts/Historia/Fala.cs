using System;
using UnityEngine;

// Uma fala de diálogo. Sem personagem = narrador (texto em itálico, sem retrato).
[Serializable]
public class Fala
{
    public PersonagemHistoria quem;
    [TextArea(2, 4)] public string texto;

    [Tooltip("Balãozinho em cima da cabeça de quem fala (se ele estiver na cena).")]
    public Emote emote;

    [Tooltip("A caixa treme (para gritos).")]
    public bool tremer;

    public Fala() { }

    public Fala(PersonagemHistoria quem, string texto, Emote emote = Emote.Nenhum, bool tremer = false)
    {
        this.quem = quem;
        this.texto = texto;
        this.emote = emote;
        this.tremer = tremer;
    }
}
