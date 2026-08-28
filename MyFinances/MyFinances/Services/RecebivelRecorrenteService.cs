using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecebivelRecorrenteService : IRecebivelRecorrenteService
{
    private readonly IRecebivelRecorrenteRepository _recebivelRecorrenteRepository;
    private readonly IRecebivelRecorrenteGeradorService _geradorService;

    public RecebivelRecorrenteService(
        IRecebivelRecorrenteRepository recebivelRecorrenteRepository,
        IRecebivelRecorrenteGeradorService geradorService)
    {
        _recebivelRecorrenteRepository = recebivelRecorrenteRepository;
        _geradorService = geradorService;
    }

    public async Task<RecebivelRecorrente> CriarAsync(
        string descricao, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new ArgumentException("Descricao nao pode estar vazia", nameof(descricao));
        }

        var campos = ValidarERecortarCampos(valor, periodicidade, diaVencimento, mesReferencia, diaDaSemana);

        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = descricao,
            Valor = valor,
            Periodicidade = campos.Periodicidade,
            DiaVencimento = campos.DiaVencimento,
            MesReferencia = campos.MesReferencia,
            DiaDaSemana = campos.DiaDaSemana,
            CategoriaId = categoriaId,
            Ativa = true
        };

        await _recebivelRecorrenteRepository.Adicionar(molde);
        await _recebivelRecorrenteRepository.Salvar();

        await _geradorService.MaterializarOcorrenciasAsync(molde.Id, Hoje());

        return molde;
    }

    public async Task<RecebivelRecorrente> EditarAsync(
        Guid id, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId)
    {
        var campos = ValidarERecortarCampos(valor, periodicidade, diaVencimento, mesReferencia, diaDaSemana);

        var molde = await _recebivelRecorrenteRepository.ObterPorId(id);
        if (molde == null)
        {
            throw new RecebivelRecorrenteNaoEncontradoException(id);
        }

        // Item 15: mudar periodicidade/dia_vencimento/mes_referencia/dia_da_semana
        // regenera o conjunto de ocorrencias; mudar so valor/categoria propaga.
        var ancoraMudou =
            campos.Periodicidade != molde.Periodicidade ||
            campos.DiaVencimento != molde.DiaVencimento ||
            campos.MesReferencia != molde.MesReferencia ||
            campos.DiaDaSemana != molde.DiaDaSemana;

        molde.Valor = valor;
        molde.Periodicidade = campos.Periodicidade;
        molde.DiaVencimento = campos.DiaVencimento;
        molde.MesReferencia = campos.MesReferencia;
        molde.DiaDaSemana = campos.DiaDaSemana;
        molde.CategoriaId = categoriaId;

        // Item 15: valor/categoria propagam para ocorrencias ainda PENDENTE;
        // PARCIAL/RECEBIDO nunca sao alteradas (fato consumado).
        foreach (var ocorrencia in molde.Ocorrencias.Where(c => c.Status == StatusContaReceber.Pendente))
        {
            ocorrencia.ValorTotal = valor;
            ocorrencia.CategoriaId = categoriaId;
        }

        await _recebivelRecorrenteRepository.Atualizar(molde);
        await _recebivelRecorrenteRepository.Salvar();

        if (ancoraMudou)
        {
            await _geradorService.RegenerarOcorrenciasAsync(id, Hoje());
        }

        return molde;
    }

    public async Task DesativarAsync(Guid id)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(id);
        if (molde == null)
        {
            throw new RecebivelRecorrenteNaoEncontradoException(id);
        }

        molde.Ativa = false;
        await _recebivelRecorrenteRepository.Atualizar(molde);
        await _recebivelRecorrenteRepository.Salvar();

        await _geradorService.RemoverOcorrenciasPendentesAsync(id);
    }

    public async Task ReativarAsync(Guid id)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(id);
        if (molde == null)
        {
            throw new RecebivelRecorrenteNaoEncontradoException(id);
        }

        molde.Ativa = true;
        await _recebivelRecorrenteRepository.Atualizar(molde);
        await _recebivelRecorrenteRepository.Salvar();

        await _geradorService.MaterializarOcorrenciasAsync(id, Hoje());
    }

    public async Task<RecebivelRecorrente> ObterPorId(Guid id)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(id);
        if (molde == null)
        {
            throw new RecebivelRecorrenteNaoEncontradoException(id);
        }

        return molde;
    }

    public async Task<IEnumerable<RecebivelRecorrente>> Listar(bool? ativaFiltro = null)
    {
        return await _recebivelRecorrenteRepository.Listar(ativaFiltro);
    }

    // stack.md: datas em UTC no banco. Igual ao Job e ao ProjecaoMesService.
    private static DateOnly Hoje() => DateOnly.FromDateTime(DateTime.UtcNow.Date);

    // Item 15: validacao por periodicidade (espelha ValidarDiaVencimentoEValor do
    // ContaFixaService) + recorte dos campos incompativeis para null.
    private static (PeriodicidadeRecebivel Periodicidade, int? DiaVencimento, int? MesReferencia, DiaDaSemana? DiaDaSemana)
        ValidarERecortarCampos(decimal valor, string periodicidade, int? diaVencimento, int? mesReferencia, string? diaDaSemana)
    {
        if (valor <= 0)
        {
            throw new ArgumentException("Valor deve ser maior que zero", nameof(valor));
        }

        var periodicidadeEnum = ConverterPeriodicidade(periodicidade);

        if (diaVencimento.HasValue && (diaVencimento.Value < 1 || diaVencimento.Value > 31))
        {
            throw new ArgumentException("Dia de vencimento deve estar entre 1 e 31", nameof(diaVencimento));
        }

        if (mesReferencia.HasValue && (mesReferencia.Value < 1 || mesReferencia.Value > 12))
        {
            throw new ArgumentException("Mes de referencia deve estar entre 1 e 12", nameof(mesReferencia));
        }

        var precisaDiaVencimento = periodicidadeEnum is PeriodicidadeRecebivel.Mensal or PeriodicidadeRecebivel.Anual;
        if (precisaDiaVencimento && !diaVencimento.HasValue)
        {
            throw new ArgumentException("Dia de vencimento e obrigatorio para periodicidade MENSAL ou ANUAL", nameof(diaVencimento));
        }

        if (periodicidadeEnum == PeriodicidadeRecebivel.Anual && !mesReferencia.HasValue)
        {
            throw new ArgumentException("Mes de referencia e obrigatorio para periodicidade ANUAL", nameof(mesReferencia));
        }

        DiaDaSemana? diaDaSemanaEnum = ConverterDiaDaSemana(diaDaSemana);
        if (periodicidadeEnum == PeriodicidadeRecebivel.Semanal && diaDaSemanaEnum == null)
        {
            throw new ArgumentException("Dia da semana e obrigatorio para periodicidade SEMANAL", nameof(diaDaSemana));
        }

        // Recorte: campo incompativel com a periodicidade fica null (silencioso, item 15).
        var diaVencimentoFinal = precisaDiaVencimento ? diaVencimento : null;
        var mesReferenciaFinal = periodicidadeEnum == PeriodicidadeRecebivel.Anual ? mesReferencia : null;
        var diaDaSemanaFinal = periodicidadeEnum == PeriodicidadeRecebivel.Semanal ? diaDaSemanaEnum : null;

        return (periodicidadeEnum, diaVencimentoFinal, mesReferenciaFinal, diaDaSemanaFinal);
    }

    private static PeriodicidadeRecebivel ConverterPeriodicidade(string? periodicidade)
    {
        if (string.IsNullOrEmpty(periodicidade))
        {
            throw new ArgumentException("Periodicidade nao pode estar vazia", nameof(periodicidade));
        }

        try
        {
            return PeriodicidadeRecebivelExtensions.FromStorageValue(periodicidade.ToUpperInvariant());
        }
        catch
        {
            throw new ArgumentException($"Periodicidade invalida: {periodicidade}", nameof(periodicidade));
        }
    }

    private static DiaDaSemana? ConverterDiaDaSemana(string? diaDaSemana)
    {
        if (string.IsNullOrEmpty(diaDaSemana))
        {
            return null;
        }

        try
        {
            return DiaDaSemanaExtensions.FromStorageValue(diaDaSemana.ToUpperInvariant());
        }
        catch
        {
            throw new ArgumentException($"Dia da semana invalido: {diaDaSemana}", nameof(diaDaSemana));
        }
    }
}
