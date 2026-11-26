using System;
using System.Collections.Generic;
using UnityEngine;

// Carros e cores que o Jason pode escolher na garagem de casa.
// Montado por Mr Driver > História > 6. Carros da garagem (Assets/Historia/CatalogoCarros.asset).
// Dá para mudar nomes, descrições e cores direto no Inspector.
[CreateAssetMenu(menuName = "Mr Driver/Catálogo de carros", fileName = "CatalogoCarros")]
public class CatalogoCarros : ScriptableObject
{
    [Serializable]
    public class Modelo
    {
        [Tooltip("Identificador salvo na escolha do jogador. Não mude depois de lançar o jogo.")]
        public string id = "Kicks";
        public string nome = "Nissan Kicks";
        [TextArea(2, 3)] public string descricao;

        [Tooltip("Lataria em tons de cinza: recebe a cor escolhida.")]
        public Sprite lataria;
        [Tooltip("Vidros, faróis, pneus... (não recebem cor).")]
        public Sprite detalhes;

        [Tooltip("Versões pequenas, no tamanho da casa (o carro parado na garagem do Jason).")]
        public Sprite latariaCasa;
        public Sprite detalhesCasa;

        [Tooltip("Mostra as rodas separadas do carro do jogador (só o desenho original usa).")]
        public bool mostrarRodas;
    }

    [Serializable]
    public class Cor
    {
        public string nome = "Branco";
        public Color cor = Color.white;
    }

    public List<Modelo> modelos = new List<Modelo>();
    public List<Cor> cores = new List<Cor>();

    public int Quantidade => modelos.Count;

    public int IndiceDe(string id)
    {
        for (int i = 0; i < modelos.Count; i++)
            if (modelos[i] != null && modelos[i].id == id) return i;
        return 0;
    }

    public Modelo ModeloDe(string id) => modelos.Count == 0 ? null : modelos[IndiceDe(id)];

    public Cor CorDe(int indice) => cores.Count == 0 ? null : cores[Mathf.Clamp(indice, 0, cores.Count - 1)];

    public Color CorOu(int indice)
    {
        Cor c = CorDe(indice);
        return c != null ? c.cor : Color.white;
    }
}
