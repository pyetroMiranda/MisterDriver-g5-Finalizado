using System;
using UnityEngine;

// Teclas que podem aparecer nas dicas (cada uma vira um desenho de tecla na tela)
public enum TeclaTutorial { Enter, Espaco, Esc, Cima, Baixo, Esquerda, Direita, W, A, S, D, L, H, M, Ctrl, B, J, K, G }

// Uma dica do tutorial. A lista delas fica no componente TutorialNivel (CanvasDicas) e pode ser
// reordenada, editada, criada ou apagada direto no Inspector.
[Serializable]
public class PassoTutorial
{
    public enum Quando { EmSequencia, AoPassarNoGatilho }

    public string titulo = "Nova dica";
    [TextArea(2, 5)] public string mensagem;

    [Header("Quando aparece")]
    [Tooltip("EmSequencia: aparece na ordem da lista, depois da dica anterior.\n" +
             "AoPassarNoGatilho: aparece quando o carro passa por um GatilhoDica com o mesmo Id.")]
    public Quando quando = Quando.EmSequencia;

    [Tooltip("Só para EmSequencia: segundos de jogo depois que a dica anterior fechou.")]
    public float espera = 1f;

    [Tooltip("Só para AoPassarNoGatilho: o mesmo Id escrito no GatilhoDica da pista.")]
    public string idGatilho;

    [Header("Teclas")]
    [Tooltip("Teclas desenhadas na dica.")]
    public TeclaTutorial[] teclas;

    [Tooltip("Marcado: a dica fecha quando o jogador aperta uma dessas teclas.\nDesmarcado: fecha com ENTER.")]
    public bool fecharComAsTeclas = true;

    [Header("Comportamento")]
    [Tooltip("O jogo vai parando suavemente enquanto a dica está aberta.")]
    public bool pausarJogo = true;

    [Tooltip("Fecha sozinha depois de X segundos (0 = só fecha com tecla).")]
    public float fecharSozinhoApos = 0f;

    [Header("Poder da loja (opcional)")]
    [Tooltip("Se o jogador ainda não comprou este item, a dica avisa que ele pode comprar na loja.")]
    public ItemLoja poder;
}
