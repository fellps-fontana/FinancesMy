using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class AssinaturaCartaoService : IAssinaturaCartaoService
{
    private readonly IAssinaturaCartaoRepository _assinaturaCartaoRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly FaturaCicloService _faturaCicloService;

    public AssinaturaCartaoService(
        IAssinaturaCartaoRepository assinaturaCartaoRepository,
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository,
        FaturaCicloService faturaCicloService)
    {
        _assinaturaCartaoRepository = assinaturaCartaoRepository;
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
        _faturaCicloService = faturaCicloService;
    }

    public async Task<AssinaturaCartao> CriarAsync(
        Guid contaId,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId)
    {
        ValidarDadosEntrada(descricao, valor, diaReferencia);

        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            throw new ArgumentException("Conta nao encontrada");
        }

        if (!conta.Ativa)
        {
            throw new ArgumentException("Conta inativa");
        }

        if (conta.Tipo != TipoConta.Cartao)
        {
            throw new ArgumentException("Assinatura de cartao so e permitida para contas do tipo Cartao");
        }

        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CategoriaId = categoriaId,
            Descricao = descricao.Trim(),
            Valor = valor,
            DiaReferencia = diaReferencia,
            Ativa = true
        };

        await _assinaturaCartaoRepository.Adicionar(assinatura);
        await _assinaturaCartaoRepository.Salvar();

        var hoje = DateTime.Today;
        var diaClamp = Math.Min(diaReferencia, DateTime.DaysInMonth(hoje.Year, hoje.Month));
        var dataCompra = new DateOnly(hoje.Year, hoje.Month, diaClamp);

        await GerarCompraAsync(contaId, assinatura.Id, assinatura.Descricao, assinatura.Valor, assinatura.CategoriaId, dataCompra);

        return assinatura;
    }

    public async Task<AssinaturaCartao> EditarAsync(
        Guid id,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId)
    {
        ValidarDadosEntrada(descricao, valor, diaReferencia);

        var assinatura = await _assinaturaCartaoRepository.ObterPorId(id);
        if (assinatura == null)
        {
            throw new AssinaturaCartaoNaoEncontradaException(id);
        }

        var diaMudou = assinatura.DiaReferencia != diaReferencia;

        assinatura.Descricao = descricao.Trim();
        assinatura.Valor = valor;
        assinatura.DiaReferencia = diaReferencia;
        assinatura.CategoriaId = categoriaId;

        await _assinaturaCartaoRepository.Atualizar(assinatura);
        await _assinaturaCartaoRepository.Salvar();

        if (assinatura.Compras != null)
        {
            foreach (var compra in assinatura.Compras)
            {
                if (compra.Fatura != null && compra.Fatura.Status == StatusFatura.Aberta)
                {
                    compra.Valor = valor;
                    compra.Descricao = descricao.Trim();
                    compra.CategoriaId = categoriaId;

                    if (diaMudou)
                    {
                        var diasNoMes = DateTime.DaysInMonth(compra.Data.Year, compra.Data.Month);
                        var novoDia = Math.Min(diaReferencia, diasNoMes);
                        compra.Data = new DateOnly(compra.Data.Year, compra.Data.Month, novoDia);
                    }

                    await _lancamentoRepository.Atualizar(compra);
                }
            }

            await _lancamentoRepository.Salvar();
        }

        return assinatura;
    }

    public async Task DesativarAsync(Guid id)
    {
        var assinatura = await _assinaturaCartaoRepository.ObterPorId(id);
        if (assinatura == null)
        {
            throw new AssinaturaCartaoNaoEncontradaException(id);
        }

        assinatura.Ativa = false;
        await _assinaturaCartaoRepository.Atualizar(assinatura);
        await _assinaturaCartaoRepository.Salvar();
    }

    public async Task ReativarAsync(Guid id)
    {
        var assinatura = await _assinaturaCartaoRepository.ObterPorId(id);
        if (assinatura == null)
        {
            throw new AssinaturaCartaoNaoEncontradaException(id);
        }

        assinatura.Ativa = true;
        await _assinaturaCartaoRepository.Atualizar(assinatura);
        await _assinaturaCartaoRepository.Salvar();

        var hoje = DateTime.Today;
        var existeCompra = await _assinaturaCartaoRepository.ExisteCompraGerada(id, hoje.Year, hoje.Month);
        if (!existeCompra)
        {
            var diaClamp = Math.Min(assinatura.DiaReferencia, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            var dataCompra = new DateOnly(hoje.Year, hoje.Month, diaClamp);

            await GerarCompraAsync(assinatura.ContaId, assinatura.Id, assinatura.Descricao, assinatura.Valor, assinatura.CategoriaId, dataCompra);
        }
    }

    public async Task<AssinaturaCartao> ObterPorIdAsync(Guid id)
    {
        var assinatura = await _assinaturaCartaoRepository.ObterPorId(id);
        if (assinatura == null)
        {
            throw new AssinaturaCartaoNaoEncontradaException(id);
        }

        return assinatura;
    }

    public async Task<IEnumerable<AssinaturaCartao>> ListarAsync(Guid? contaId = null, bool? ativaFiltro = null)
    {
        return await _assinaturaCartaoRepository.Listar(contaId, ativaFiltro);
    }

    public async Task<int> GarantirAssinaturasDoCicloAsync(Guid contaId, int ano, int mes)
    {
        var assinaturas = await _assinaturaCartaoRepository.ListarAtivasPorConta(contaId);
        int totalGeradas = 0;

        foreach (var assinatura in assinaturas)
        {
            if (await _assinaturaCartaoRepository.ExisteCompraGerada(assinatura.Id, ano, mes))
            {
                continue;
            }

            var diasNoMes = DateTime.DaysInMonth(ano, mes);
            var dia = Math.Min(assinatura.DiaReferencia, diasNoMes);
            var dataCompra = new DateOnly(ano, mes, dia);

            await GerarCompraAsync(contaId, assinatura.Id, assinatura.Descricao, assinatura.Valor, assinatura.CategoriaId, dataCompra);
            totalGeradas++;
        }

        return totalGeradas;
    }

    private static void ValidarDadosEntrada(string descricao, decimal valor, int diaReferencia)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new ArgumentException("A descricao e obrigatoria.", nameof(descricao));
        }

        if (valor <= 0)
        {
            throw new ArgumentException("O valor deve ser maior que zero.", nameof(valor));
        }

        if (diaReferencia < 1 || diaReferencia > 31)
        {
            throw new ArgumentException("O dia de referencia deve estar entre 1 e 31.", nameof(diaReferencia));
        }
    }

    private async Task<Lancamento> GerarCompraAsync(
        Guid contaId,
        Guid assinaturaId,
        string descricao,
        decimal valor,
        Guid? categoriaId,
        DateOnly dataCompra)
    {
        var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataCompra);
        if (rejeitada || fatura == null)
        {
            throw new InvalidOperationException(motivo ?? "Nao foi possivel resolver a fatura.");
        }

        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CategoriaId = categoriaId,
            Descricao = descricao,
            Valor = valor,
            Tipo = TipoLancamento.Debit,
            Data = dataCompra,
            Status = StatusLancamento.Pago,
            Manual = true,
            FaturaId = fatura.Id,
            AssinaturaCartaoId = assinaturaId
        };

        await _lancamentoRepository.Adicionar(compra);
        await _lancamentoRepository.Salvar();
        return compra;
    }
}
