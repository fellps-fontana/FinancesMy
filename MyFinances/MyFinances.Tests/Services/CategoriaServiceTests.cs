using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using MyFinances.Services;

namespace MyFinances.Tests.Services;

public class CategoriaServiceTests
{
    private readonly Mock<ICategoriaRepository> _mockRepository;
    private readonly CategoriaService _service;

    public CategoriaServiceTests()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _service = new CategoriaService(_mockRepository.Object);
    }

    #region Regra 1: Criar categoria raiz (sem parentId) — qualquer tipo aceito

    [Fact]
    public async Task Criar_CategoriaRaiz_Despesa_Sucesso()
    {
        // Arrange
        var nome = "Alimentacao";
        var tipo = TipoCategoria.Despesa;

        // Act
        var resultado = await _service.Criar(nome, tipo);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(nome, resultado.Nome);
        Assert.Equal(tipo, resultado.Tipo);
        Assert.Null(resultado.ParentId);
        Assert.False(resultado.Arquivada);
        _mockRepository.Verify(r => r.Adicionar(It.IsAny<Categoria>()), Times.Once);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Criar_CategoriaRaiz_Receita_Sucesso()
    {
        // Arrange
        var nome = "Salario";
        var tipo = TipoCategoria.Receita;

        // Act
        var resultado = await _service.Criar(nome, tipo);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(nome, resultado.Nome);
        Assert.Equal(tipo, resultado.Tipo);
        Assert.Null(resultado.ParentId);
        Assert.False(resultado.Arquivada);
    }

    #endregion

    #region Regra 2: Criar subcategoria com mesmo tipo do pai — sucesso

    [Fact]
    public async Task Criar_Subcategoria_MesmoTipo_Despesa_Sucesso()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = parentId,
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync(parent);

        // Act
        var resultado = await _service.Criar("Restaurante", TipoCategoria.Despesa, parentId);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Restaurante", resultado.Nome);
        Assert.Equal(TipoCategoria.Despesa, resultado.Tipo);
        Assert.Equal(parentId, resultado.ParentId);
        Assert.False(resultado.Arquivada);
    }

    [Fact]
    public async Task Criar_Subcategoria_MesmoTipo_Receita_Sucesso()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = parentId,
            Nome = "Investimentos",
            Tipo = TipoCategoria.Receita,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync(parent);

        // Act
        var resultado = await _service.Criar("Dividendos", TipoCategoria.Receita, parentId);

        // Assert
        Assert.Equal(parentId, resultado.ParentId);
        Assert.Equal(TipoCategoria.Receita, resultado.Tipo);
    }

    #endregion

    #region Regra 3: Rejeitar criar subcategoria com tipo DIFERENTE do pai

    [Fact]
    public async Task Criar_Subcategoria_TipoDiferente_DespesaSobReceita_Falha()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = parentId,
            Nome = "Receitas",
            Tipo = TipoCategoria.Receita,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync(parent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Criar("Gasto", TipoCategoria.Despesa, parentId)
        );

        Assert.Contains("mesmo tipo da categoria pai", ex.Message);
    }

    [Fact]
    public async Task Criar_Subcategoria_TipoDiferente_ReceitaSobDespesa_Falha()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = parentId,
            Nome = "Despesas",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync(parent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Criar("Renda", TipoCategoria.Receita, parentId)
        );

        Assert.Contains("mesmo tipo da categoria pai", ex.Message);
    }

    #endregion

    #region Regra 4: Rejeitar criar subcategoria de uma subcategoria (hierarquia max 1 nivel)

    [Fact]
    public async Task Criar_SubcategoriaDeSubcategoria_Falha()
    {
        // Arrange
        var avoidParentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Subcategoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = avoidParentId, // tem parent, nao e raiz
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parent.Id))
            .ReturnsAsync(parent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Criar("SubSubcategoria", TipoCategoria.Despesa, parent.Id)
        );

        Assert.Contains("Hierarquia maxima: 1 nivel", ex.Message);
    }

    #endregion

    #region Regra 5: Rejeitar criar/editar com parentId de categoria arquivada

    [Fact]
    public async Task Criar_Subcategoria_ParentArquivada_Falha()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = new Categoria
        {
            Id = parentId,
            Nome = "Categoria Arquivada",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = true, // arquivada!
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync(parent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Criar("Subcategoria", TipoCategoria.Despesa, parentId)
        );

        Assert.Contains("categoria arquivada", ex.Message);
    }

    [Fact]
    public async Task Editar_MudaParentParaArquivada_Falha()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var parentArquivadoId = Guid.NewGuid();

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria A",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var parentArquivado = new Categoria
        {
            Id = parentArquivadoId,
            Nome = "Categoria Arquivada",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = true,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockRepository
            .Setup(r => r.ObterPorId(parentArquivadoId))
            .ReturnsAsync(parentArquivado);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Editar(categoriaId, "Novo Nome", parentArquivadoId)
        );

        Assert.Contains("categoria arquivada", ex.Message);
    }

    #endregion

    #region Regra 6: Listar com filtro arquivada=false por padrao

    [Fact]
    public async Task Listar_SemParametroArquivada_DefaultaParaFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.Listar(null, null, null))
            .ReturnsAsync(new List<Categoria>());

        // Act
        await _service.Listar();

        // Assert
        _mockRepository.Verify(
            r => r.Listar(null, null, null),
            Times.Once,
            "Listar sem parametros deve passar null para arquivo"
        );
    }

    [Fact]
    public async Task Listar_ComParametroArquivadaFalse_FiltroExplicito()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Ativa",
                Tipo = TipoCategoria.Despesa,
                ParentId = null,
                Arquivada = false,
                Subcategorias = new List<Categoria>()
            }
        };

        _mockRepository
            .Setup(r => r.Listar(null, false, null))
            .ReturnsAsync(categorias);

        // Act
        var resultado = await _service.Listar(arquivada: false);

        // Assert
        Assert.Single(resultado);
        Assert.False(resultado.First().Arquivada);
    }

    [Fact]
    public async Task Listar_ComParametroArquivadaTrue_FiltroExplicito()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Arquivada",
                Tipo = TipoCategoria.Despesa,
                ParentId = null,
                Arquivada = true,
                Subcategorias = new List<Categoria>()
            }
        };

        _mockRepository
            .Setup(r => r.Listar(null, true, null))
            .ReturnsAsync(categorias);

        // Act
        var resultado = await _service.Listar(arquivada: true);

        // Assert
        Assert.Single(resultado);
        Assert.True(resultado.First().Arquivada);
    }

    [Fact]
    public async Task Listar_ComFiltroTipo_PassaTipoParaRepository()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.Listar(TipoCategoria.Despesa, null, null))
            .ReturnsAsync(new List<Categoria>());

        // Act
        await _service.Listar(tipo: TipoCategoria.Despesa);

        // Assert
        _mockRepository.Verify(
            r => r.Listar(TipoCategoria.Despesa, null, null),
            Times.Once
        );
    }

    [Fact]
    public async Task Listar_ComFiltroParentId_PassaParentIdParaRepository()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.Listar(null, null, parentId))
            .ReturnsAsync(new List<Categoria>());

        // Act
        await _service.Listar(parentId: parentId);

        // Assert
        _mockRepository.Verify(
            r => r.Listar(null, null, parentId),
            Times.Once
        );
    }

    #endregion

    #region Regra 7: Editar nome e parentId com sucesso quando valido

    [Fact]
    public async Task Editar_MudaNome_Sucesso()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Nome Antigo",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act
        var resultado = await _service.Editar(categoriaId, "Nome Novo", null);

        // Assert
        Assert.Equal("Nome Novo", resultado.Nome);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Editar_MudaParentParaCategoriaRaizValida_Sucesso()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var novoParentId = Guid.NewGuid();

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Subcategoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var novoParent = new Categoria
        {
            Id = novoParentId,
            Nome = "Categoria Raiz",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockRepository
            .Setup(r => r.ObterPorId(novoParentId))
            .ReturnsAsync(novoParent);

        // Act
        var resultado = await _service.Editar(categoriaId, categoria.Nome, novoParentId);

        // Assert
        Assert.Equal(novoParentId, resultado.ParentId);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Editar_RemoveParent_Sucesso()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Subcategoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = parentId,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act
        var resultado = await _service.Editar(categoriaId, "Novo Nome", null);

        // Assert
        Assert.Null(resultado.ParentId);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 8: Rejeitar editar com parentId == id (auto-referencia)

    [Fact]
    public async Task Editar_ParentIdIgualId_AutoReferencia_Falha()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Editar(categoriaId, "Novo Nome", categoriaId)
        );

        Assert.Contains("nao pode ser parent de si mesma", ex.Message);
    }

    #endregion

    #region Regra 9: Rejeitar editar de categoria com subcategorias pra vira-la filha de outra

    [Fact]
    public async Task Editar_CategoriaComSubcategorias_ViraFilha_Falha()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var novoParentId = Guid.NewGuid();

        var subcategoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Subcategoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = categoriaId,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria Com Subcategorias",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria> { subcategoria }
        };

        var novoParent = new Categoria
        {
            Id = novoParentId,
            Nome = "Nova Categoria Raiz",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockRepository
            .Setup(r => r.ObterPorId(novoParentId))
            .ReturnsAsync(novoParent);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Editar(categoriaId, "Novo Nome", novoParentId)
        );

        Assert.Contains("possui subcategorias", ex.Message);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 10: Arquivar cascata — seta Arquivada=true na categoria E em todas as subcategorias

    [Fact]
    public async Task Arquivar_CategoriaRaiz_SemSubcategorias_Sucesso()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act
        await _service.Arquivar(categoriaId);

        // Assert
        Assert.True(categoria.Arquivada);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Arquivar_CategoriaRaiz_ComSubcategorias_ArquivaEm_Cascata()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var subcategoria1Id = Guid.NewGuid();
        var subcategoria2Id = Guid.NewGuid();

        var subcategoria1 = new Categoria
        {
            Id = subcategoria1Id,
            Nome = "Subcategoria 1",
            Tipo = TipoCategoria.Despesa,
            ParentId = categoriaId,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var subcategoria2 = new Categoria
        {
            Id = subcategoria2Id,
            Nome = "Subcategoria 2",
            Tipo = TipoCategoria.Despesa,
            ParentId = categoriaId,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria Com Subcategorias",
            Tipo = TipoCategoria.Despesa,
            ParentId = null,
            Arquivada = false,
            Subcategorias = new List<Categoria> { subcategoria1, subcategoria2 }
        };

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act
        await _service.Arquivar(categoriaId);

        // Assert
        Assert.True(categoria.Arquivada);
        Assert.True(subcategoria1.Arquivada);
        Assert.True(subcategoria2.Arquivada);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 11: Nome duplicado permitido (teste positivo simples)

    [Fact]
    public async Task Criar_NomeDuplicado_Permitido()
    {
        // Arrange
        var nome = "Alimentacao";
        var tipo = TipoCategoria.Despesa;

        // Act
        var categoria1 = await _service.Criar(nome, tipo);
        var categoria2 = await _service.Criar(nome, tipo);

        // Assert
        Assert.NotEqual(categoria1.Id, categoria2.Id);
        Assert.Equal(categoria1.Nome, categoria2.Nome);
        Assert.Equal(categoria1.Tipo, categoria2.Tipo);
    }

    #endregion

    #region Casos de borda e regressao

    [Fact]
    public async Task Criar_CategoriaInexistente_ObterPorId_Falha()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(parentId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Criar("Subcategoria", TipoCategoria.Despesa, parentId)
        );

        Assert.Equal(parentId, ex.CategoriaId);
    }

    [Fact]
    public async Task Editar_CategoriaInexistente_Falha()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Editar(categoriaId, "Novo Nome", null)
        );

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    [Fact]
    public async Task Arquivar_CategoriaInexistente_Falha()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Arquivar(categoriaId)
        );

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    #endregion
}
