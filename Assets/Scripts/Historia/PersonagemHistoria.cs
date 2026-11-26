using UnityEngine;

// Um personagem da história: nome, cor da plaquinha de nome, retrato da caixa de diálogo,
// os desenhos de andar (estilo Pokémon antigo) e o tom da "voz" (o bipe das letras).
// Os arquivos ficam em Assets/Historia/Personagens e são criados por Mr Driver > História > 1.
[CreateAssetMenu(menuName = "Mr Driver/Personagem da história", fileName = "Personagem")]
public class PersonagemHistoria : ScriptableObject
{
    public string nome = "Personagem";
    public Color corNome = new Color(1f, 0.12f, 0.8f);

    [Tooltip("Rosto grande que aparece na caixa de diálogo.")]
    public Sprite retrato;

    [Tooltip("Andar: 0-2 de frente (baixo), 3-5 de costas (cima), 6-8 de lado (olhando para a direita).\n" +
             "Em cada direção: parado, passo 1, passo 2.")]
    public Sprite[] quadros = new Sprite[9];

    [Tooltip("Tom do bipe das letras (1 = normal, maior = mais agudo).")]
    [Range(0.5f, 2f)] public float tomVoz = 1f;

    public Sprite Quadro(Direcao direcao, int passo)
    {
        int linha = direcao == Direcao.Baixo ? 0 : direcao == Direcao.Cima ? 1 : 2;
        int indice = linha * 3 + Mathf.Clamp(passo, 0, 2);
        return quadros != null && indice < quadros.Length ? quadros[indice] : null;
    }
}

public enum Direcao { Baixo, Cima, Esquerda, Direita }

// Balõezinhos que aparecem em cima da cabeça (como o "!" do Pokémon)
public enum Emote { Nenhum, Exclamacao, Interrogacao, Coracao, Sono, Gota }
