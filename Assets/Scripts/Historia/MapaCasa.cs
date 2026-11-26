using System.Collections.Generic;
using UnityEngine;

// Grade da casa (estilo Pokémon): cada quadradinho é 1 unidade. A linha 0 fica em cima.
// Célula (x, y) fica na posição do mundo (x, -y). '.' = chão livre, qualquer outra letra = bloqueado.
// Montado por Mr Driver > História > 2. Criar cena da casa.
public class MapaCasa : MonoBehaviour
{
    [SerializeField] private string[] colisao = new string[0];

    private readonly List<Interagivel> interagiveis = new List<Interagivel>();
    private readonly List<MovimentoGrade> moveis = new List<MovimentoGrade>();

    public int Largura => colisao.Length > 0 ? colisao[0].Length : 0;
    public int Altura => colisao.Length;

    // Retângulo do mapa no mundo (a câmera usa para não mostrar fora da casa)
    public Rect Limites => new Rect(-0.5f, -Altura + 0.5f, Largura, Altura);

    public void DefinirColisao(string[] linhas) => colisao = linhas;   // usado pela ferramenta do Editor

    public static Vector3 ParaMundo(Vector2Int c) => new Vector3(c.x, -c.y, 0f);
    public static Vector2Int ParaCelula(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(-p.y));

    public static Vector2Int Vetor(Direcao d)
    {
        switch (d)
        {
            case Direcao.Cima: return new Vector2Int(0, -1);
            case Direcao.Baixo: return new Vector2Int(0, 1);
            case Direcao.Esquerda: return new Vector2Int(-1, 0);
            default: return new Vector2Int(1, 0);
        }
    }

    public void Registrar(Interagivel i) { if (!interagiveis.Contains(i)) interagiveis.Add(i); }
    public void Remover(Interagivel i) => interagiveis.Remove(i);
    public void Registrar(MovimentoGrade m) { if (!moveis.Contains(m)) moveis.Add(m); }
    public void Remover(MovimentoGrade m) => moveis.Remove(m);

    public bool Livre(Vector2Int c, MovimentoGrade quem)
    {
        if (c.y < 0 || c.y >= Altura || c.x < 0 || c.x >= colisao[c.y].Length) return false;
        if (colisao[c.y][c.x] != '.') return false;

        foreach (Interagivel i in interagiveis)
            if (i != null && i.isActiveAndEnabled && i.Bloqueia && i.Ocupa(c)) return false;

        foreach (MovimentoGrade m in moveis)
            if (m != null && m != quem && m.isActiveAndEnabled && (m.Celula == c || m.Destino == c)) return false;

        return true;
    }

    public Interagivel InteragivelEm(Vector2Int c)
    {
        foreach (Interagivel i in interagiveis)
            if (i != null && i.isActiveAndEnabled && i.Ocupa(c)) return i;
        return null;
    }

    // Mostra a grade na aba Scene (vermelho = bloqueado)
    private void OnDrawGizmosSelected()
    {
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < colisao[y].Length; x++)
            {
                if (colisao[y][x] == '.') continue;
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                Gizmos.DrawCube(ParaMundo(new Vector2Int(x, y)), new Vector3(0.95f, 0.95f, 0.01f));
            }
    }
}
