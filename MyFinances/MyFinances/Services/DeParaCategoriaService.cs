using MyFinances.Exceptions;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class DeParaCategoriaService : IDeParaCategoriaService
{
    private readonly IDeParaCategoriaRepository _deParaCategoriaRepository;
    private readonly ICategoriaRepository _categoriaRepository;

    public DeParaCategoriaService(
        IDeParaCategoriaRepository deParaCategoriaRepository,
        ICategoriaRepository categoriaRepository)
    {
        _deParaCategoriaRepository = deParaCategoriaRepository;
        _categoriaRepository = categoriaRepository;
    }

    public async Task<DeParaCategoria> Criar(string categoriaPierre, Guid categoriaId)
    {
        await ValidarCategoriaPierreNaoExiste(categoriaPierre);
        await ValidarCategoriaExiste(categoriaId);

        var deParaCategoria = new DeParaCategoria
        {
            Id = Guid.NewGuid(),
            CategoriaPierre = categoriaPierre,
            CategoriaId = categoriaId
        };

        await _deParaCategoriaRepository.Adicionar(deParaCategoria);
        await _deParaCategoriaRepository.Salvar();

        return deParaCategoria;
    }

    public async Task<IEnumerable<DeParaCategoria>> Listar(string? categoriaPierre = null)
    {
        return await _deParaCategoriaRepository.Listar(categoriaPierre);
    }

    public async Task<DeParaCategoria> Editar(Guid id, Guid novaCategoriaId)
    {
        var deParaCategoria = await ObterDeParaCategoriaOuFalhar(id);

        await ValidarCategoriaExiste(novaCategoriaId);

        deParaCategoria.CategoriaId = novaCategoriaId;

        await _deParaCategoriaRepository.Salvar();

        return deParaCategoria;
    }

    public async Task Excluir(Guid id)
    {
        var deParaCategoria = await ObterDeParaCategoriaOuFalhar(id);

        await _deParaCategoriaRepository.Remover(deParaCategoria);
        await _deParaCategoriaRepository.Salvar();
    }

    private async Task ValidarCategoriaPierreNaoExiste(string categoriaPierre)
    {
        var existente = await _deParaCategoriaRepository.ObterPorCategoriaPierre(categoriaPierre);

        if (existente != null)
        {
            throw new InvalidOperationException($"Ja existe um vinculo para a categoria Pierre '{categoriaPierre}'.");
        }
    }

    private async Task ValidarCategoriaExiste(Guid categoriaId)
    {
        var categoria = await _categoriaRepository.ObterPorId(categoriaId);

        if (categoria == null)
        {
            throw new CategoriaNaoEncontradaException(categoriaId);
        }
    }

    private async Task<DeParaCategoria> ObterDeParaCategoriaOuFalhar(Guid id)
    {
        var deParaCategoria = await _deParaCategoriaRepository.ObterPorId(id);

        if (deParaCategoria == null)
        {
            throw new DeParaCategoriaNaoEncontradoException(id);
        }

        return deParaCategoria;
    }
}
