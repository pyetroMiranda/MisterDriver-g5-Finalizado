// Freios ABS: aperte a tecla e o carro para na hora, sem derrapar.
public class FreioABS : PoderAtivo
{
    public override string Nome => "Freio ABS";

    private void Reset()
    {
        duracao = 0f;
        recarga = 8f;
    }

    protected override void AoAtivar()
    {
        carro.PararImediatamente();
    }
}
