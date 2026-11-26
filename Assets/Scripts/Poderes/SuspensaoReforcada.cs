// Suspensão Reforçada: enquanto está ligada, os buracos não freiam o carro nem tiram pontos.
// (O Buraco.cs verifica se este poder está Ativo.)
public class SuspensaoReforcada : PoderAtivo
{
    public override string Nome => "Suspensão";

    private void Reset()
    {
        duracao = 8f;
        recarga = 25f;
    }

    protected override void AoAtivar() { }
}
