using UnityEngine;

// Letras em pixel art (uma imagem por caractere), usadas pelo TextoPixel. Criada pela ferramenta do videogame.
public class FontePixel : ScriptableObject
{
    public string caracteres = "";
    public Sprite[] glifos = new Sprite[0];
    [Tooltip("Altura de todas as letras em pixels (inclui espaço para acento e cedilha).")]
    public int altura = 9;
    [Tooltip("Pixels entre uma letra e outra.")]
    public int espaco = 1;

    public Sprite Glifo(char c)
    {
        int i = caracteres.IndexOf(c);
        if (i < 0) i = caracteres.IndexOf(char.ToUpperInvariant(c));
        return i >= 0 && i < glifos.Length ? glifos[i] : null;
    }
}
