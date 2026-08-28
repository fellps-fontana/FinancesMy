using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyFinances.Services;

// regra-de-negocio.md item 15: materializa as ocorrencias FUTURAS de
// RecebivelRecorrente. Roda 1x/dia (e uma vez no startup). BackgroundService e
// singleton; o gerador e os repositorios sao scoped -- por isso abre um escopo
// de DI proprio por execucao via IServiceScopeFactory. Esta e a extensao
// CONSCIENTE da restricao "sem job na v1" do item 6 -- vale SO para recebivel
// recorrente.
public class RecebivelRecorrenteMaterializacaoJob : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecebivelRecorrenteMaterializacaoJob> _logger;

    public RecebivelRecorrenteMaterializacaoJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RecebivelRecorrenteMaterializacaoJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ESQUELETO: no-op ate levi implementar o loop (PeriodicTimer 24h + 1 execucao
    // no startup, abrindo escopo de DI e chamando MaterializarTodosAtivosAsync).
    // NAO pode lancar aqui -- ExecuteAsync que falha no startup derruba o host.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.CompletedTask;
}
