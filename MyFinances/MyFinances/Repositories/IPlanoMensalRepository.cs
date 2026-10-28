using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IPlanoMensalRepository
{
    // Include Sublimites. Null se nao existe.
    Task<PlanoMensal?> ObterPorAnoMes(int ano, int mes);

    // Plano de maior (Ano, Mes) estritamente anterior a (ano, mes). Include Sublimites. Null se nao ha.
    Task<PlanoMensal?> ObterMaisRecenteAnteriorA(int ano, int mes);

    // Adiciona e PERSISTE. Se (Ano, Mes) ja existir (geracao concorrente, indice unico), descarta o
    // novo (detach) e devolve o existente com Sublimites. Idempotencia da geracao sob demanda (item 20).
    Task<PlanoMensal> AdicionarOuObterExistente(PlanoMensal plano);

    // Sublimite novo em plano ja rastreado: Add explicito (Id ja preenchido nao pode virar UPDATE).
    Task AdicionarSublimite(PlanoMensalSublimite sublimite);

    Task RemoverSublimite(PlanoMensalSublimite sublimite);

    Task Salvar();
}
