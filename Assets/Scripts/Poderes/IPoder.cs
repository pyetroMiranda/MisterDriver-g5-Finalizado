// O que a barra de poderes do HUD precisa saber de cada poder.
public interface IPoder
{
    string Nome { get; }
    string Tecla { get; }       // vazio = poder passivo (não tem tecla)
    string Info { get; }        // texto extra (ex.: "x3", "LIGADO")
    bool Comprado { get; }
    bool Ativo { get; }
    bool Pronto { get; }
    float Progresso { get; }    // 0 a 1: carga da recarga (ou o que sobra do efeito, se ativo)
}
