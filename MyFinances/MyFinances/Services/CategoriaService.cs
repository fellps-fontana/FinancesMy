using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _categoriaRepository;

    public CategoriaService(ICategoriaRepository categoriaRepository)
    {
        _categoriaRepository = categoriaRepository;
    }

    public async Task<Categoria> Criar(string nome, TipoCategoria tipo, Guid? parentId = null)
    {
        if (parentId.HasValue)
        {
            await ValidarParentParaCriar(parentId.Value, tipo);
        }

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Tipo = tipo,
            ParentId = parentId,
            Arquivada = false
        };

        await _categoriaRepository.Adicionar(categoria);
        await _categoriaRepository.Salvar();

        return categoria;
    }

    public async Task<IEnumerable<Categoria>> Listar(TipoCategoria? tipo = null, bool? arquivada = null, Guid? parentId = null)
    {
        return await _categoriaRepository.Listar(tipo, arquivada, parentId);
    }

    public async Task<Categoria> Editar(Guid id, string nome, Guid? parentId)
    {
        var categoria = await ObterCategoriaOuFalhar(id);

        if (parentId.HasValue && parentId.Value == id)
        {
            throw new InvalidOperationException("Uma categoria nao pode ser parent de si mesma.");
        }

        if (parentId.HasValue)
        {
            await ValidarParentParaEditar(parentId.Value, categoria.Tipo);
        }

        categoria.Nome = nome;
        categoria.ParentId = parentId;

        await _categoriaRepository.Salvar();

        return categoria;
    }

    public async Task Arquivar(Guid id)
    {
        var categoria = await ObterCategoriaOuFalhar(id);

        categoria.Arquivada = true;

        foreach (var subcategoria in categoria.Subcategorias)
        {
            subcategoria.Arquivada = true;
        }

        await _categoriaRepository.Salvar();
    }

    private async Task ValidarParentParaCriar(Guid parentId, TipoCategoria novoTipo)
    {
        var parent = await ObterCategoriaOuFalhar(parentId);

        if (parent.Arquivada)
        {
            throw new InvalidOperationException("Nao e permitido criar subcategoria de uma categoria arquivada.");
        }

        if (parent.ParentId.HasValue)
        {
            throw new InvalidOperationException("Nao e permitido criar subcategoria de uma subcategoria. Hierarquia maxima: 1 nivel.");
        }

        if (novoTipo != parent.Tipo)
        {
            throw new InvalidOperationException($"Subcategoria deve ter o mesmo tipo da categoria pai. Tipo do pai: {parent.Tipo}, tipo informado: {novoTipo}");
        }
    }

    private async Task ValidarParentParaEditar(Guid parentId, TipoCategoria tipoAtual)
    {
        var parent = await ObterCategoriaOuFalhar(parentId);

        if (parent.Arquivada)
        {
            throw new InvalidOperationException("Nao e permitido vincular a uma categoria arquivada.");
        }

        if (parent.ParentId.HasValue)
        {
            throw new InvalidOperationException("Nao e permitido vincular a uma subcategoria. Hierarquia maxima: 1 nivel.");
        }

        if (tipoAtual != parent.Tipo)
        {
            throw new InvalidOperationException($"Categoria deve manter o mesmo tipo do parent. Tipo atual: {tipoAtual}, tipo do parent: {parent.Tipo}");
        }
    }

    private async Task<Categoria> ObterCategoriaOuFalhar(Guid id)
    {
        var categoria = await _categoriaRepository.ObterPorId(id);

        if (categoria == null)
        {
            throw new CategoriaNaoEncontradaException(id);
        }

        return categoria;
    }
}
