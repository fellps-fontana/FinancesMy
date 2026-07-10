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

        if (parentId.HasValue && categoria.Subcategorias.Any())
        {
            throw new InvalidOperationException("Nao e permitido vincular uma categoria que possui subcategorias. Remova as subcategorias ou arquive-as antes.");
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
        await ValidarParent(parentId, novoTipo, isEdicao: false);
    }

    private async Task ValidarParentParaEditar(Guid parentId, TipoCategoria tipoAtual)
    {
        await ValidarParent(parentId, tipoAtual, isEdicao: true);
    }

    private async Task ValidarParent(Guid parentId, TipoCategoria tipoCategoria, bool isEdicao)
    {
        var parent = await ObterCategoriaOuFalhar(parentId);

        if (parent.Arquivada)
        {
            var mensagem = isEdicao ? "Nao e permitido vincular a uma categoria arquivada." : "Nao e permitido criar subcategoria de uma categoria arquivada.";
            throw new InvalidOperationException(mensagem);
        }

        if (parent.ParentId.HasValue)
        {
            throw new InvalidOperationException("Nao e permitido vincular a uma subcategoria. Hierarquia maxima: 1 nivel.");
        }

        if (tipoCategoria != parent.Tipo)
        {
            var mensagem = isEdicao
                ? $"Categoria deve manter o mesmo tipo do parent. Tipo atual: {tipoCategoria}, tipo do parent: {parent.Tipo}"
                : $"Subcategoria deve ter o mesmo tipo da categoria pai. Tipo do pai: {parent.Tipo}, tipo informado: {tipoCategoria}";
            throw new InvalidOperationException(mensagem);
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
