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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        try
        {
            // Executa uma vez no startup, depois a cada intervalo.
            do
            {
                await ExecutarMaterializacao();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Cancelamento normal durante shutdown.
        }
    }

    private async Task ExecutarMaterializacao()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var gerador = scope.ServiceProvider.GetRequiredService<IRecebivelRecorrenteGeradorService>();
            await gerador.MaterializarTodosAtivosAsync(DateOnly.FromDateTime(DateTime.UtcNow.Date));
        }
        catch (Exception ex)
        {
            // clean-code.md: falha de job nao pode quebrar o app -- loga e segue.
            _logger.LogError(ex, "Erro ao materializar recebivel recorrente no job");
        }
    }
}
