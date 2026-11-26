using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Linha grossa na interface (usada para a rota do GPS no minimapa), com contorno escuro e juntas arredondadas.
public class LinhaUI : MaskableGraphic
{
    [SerializeField] private float largura = 6f;
    [SerializeField] private float larguraContorno = 3f;
    [SerializeField] private Color corContorno = new Color(0.07f, 0f, 0.1f, 0.9f);

    private readonly List<Vector2> pontos = new List<Vector2>();

    public void Definir(List<Vector2> novos)
    {
        pontos.Clear();
        if (novos != null) pontos.AddRange(novos);
        SetVerticesDirty();
    }

    public void Limpar()
    {
        if (pontos.Count == 0) return;
        pontos.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (pontos.Count < 2) return;
        Desenhar(vh, largura + larguraContorno * 2f, corContorno);
        Desenhar(vh, largura, color);
    }

    private void Desenhar(VertexHelper vh, float grossura, Color cor)
    {
        float meia = grossura * 0.5f;
        for (int i = 1; i < pontos.Count; i++)
        {
            Vector2 a = pontos[i - 1], b = pontos[i];
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) continue;
            Vector2 n = new Vector2(-d.y, d.x).normalized * meia;
            Quad(vh, a - n, a + n, b + n, b - n, cor);
        }

        // Juntas e pontas redondas
        foreach (Vector2 p in pontos) Disco(vh, p, meia, cor);
    }

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color cor)
    {
        int i = vh.currentVertCount;
        vh.AddVert(a, cor, Vector4.zero);
        vh.AddVert(b, cor, Vector4.zero);
        vh.AddVert(c, cor, Vector4.zero);
        vh.AddVert(d, cor, Vector4.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    private static void Disco(VertexHelper vh, Vector2 centro, float raio, Color cor)
    {
        const int lados = 10;
        int c = vh.currentVertCount;
        vh.AddVert(centro, cor, Vector4.zero);
        for (int k = 0; k <= lados; k++)
        {
            float a = k * Mathf.PI * 2f / lados;
            vh.AddVert(centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio, cor, Vector4.zero);
            if (k > 0) vh.AddTriangle(c, c + k, c + k + 1);
        }
    }
}
