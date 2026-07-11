using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using MyFinances.Services;

namespace MyFinances.Tests.Services;

public class DeParaCategoriaServiceTests
{
    private readonly Mock<IDeParaCategoriaRepository> _mockDeParaCategoriaRepository;
    private readonly Mock<ICategoriaRepository> _mockCategoriaRepository;
    private readonly DeParaCategoriaService _service;

    public DeParaCategoriaServiceTests()
    {
        _mockDeParaCategoriaRepository = new Mock<IDeParaCategoriaRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _service = new DeParaCategoriaService(
            _mockDeParaCategoriaRepository.Object,
            _mockCategoriaRepository.Object);
    }

    #region Regra 1: Criar vinculo com CategoriaId existente e CategoriaPierre novo

    [Fact]
    public async Task Criar_ComCategoriaExistenteEPierreNovo_Sucesso()
    {
        // Arrange
        var categoriaPierre = "alimentacao_pierre";
        var categoriaId = Guid.NewGuid();
        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorCategoriaPierre(categoriaPierre))
            .ReturnsAsync((DeParaCategoria?)null);

        // Act
        var resultado = await _service.Criar(categoriaPierre, categoriaId);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(categoriaPierre, resultado.CategoriaPierre);
        Assert.Equal(categoriaId, resultado.CategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Adicionar(It.IsAny<DeParaCategoria>()), Times.Once);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 2: Rejeitar Criar com CategoriaId inexistente

    [Fact]
    public async Task Criar_ComCategoriaInexistente_LancaExcecao()
    {
        // Arrange
        var categoriaPierre = "alimentacao_pierre";
        var categoriaId = Guid.NewGuid();

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Criar(categoriaPierre, categoriaId));

        Assert.Equal(categoriaId, excecao.CategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Adicionar(It.IsAny<DeParaCategoria>()), Times.Never);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 3: Rejeitar Criar com CategoriaPierre duplicado

    [Fact]
    public async Task Criar_ComCategoriaPierreDuplicado_LancaExcecao()
    {
        // Arrange
        var categoriaPierre = "alimentacao_pierre";
        var categoriaId = Guid.NewGuid();
        var vinculoExistente = new DeParaCategoria
        {
            Id = Guid.NewGuid(),
            CategoriaPierre = categoriaPierre,
            CategoriaId = Guid.NewGuid()
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorCategoriaPierre(categoriaPierre))
            .ReturnsAsync(vinculoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.Criar(categoriaPierre, categoriaId));

        Assert.Contains("Ja existe um vinculo", excecao.Message);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Adicionar(It.IsAny<DeParaCategoria>()), Times.Never);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 4: Listar todos os vinculos

    [Fact]
    public async Task Listar_SemFiltro_RetornaListaVazia()
    {
        // Arrange
        _mockDeParaCategoriaRepository
            .Setup(r => r.Listar(null))
            .ReturnsAsync(new List<DeParaCategoria>());

        // Act
        var resultado = await _service.Listar();

        // Assert
        Assert.Empty(resultado);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Listar(null), Times.Once);
    }

    [Fact]
    public async Task Listar_SemFiltro_RetornaListaComVinculos()
    {
        // Arrange
        var vinculos = new List<DeParaCategoria>
        {
            new DeParaCategoria
            {
                Id = Guid.NewGuid(),
                CategoriaPierre = "alimentacao_pierre",
                CategoriaId = Guid.NewGuid()
            },
            new DeParaCategoria
            {
                Id = Guid.NewGuid(),
                CategoriaPierre = "transporte_pierre",
                CategoriaId = Guid.NewGuid()
            }
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.Listar(null))
            .ReturnsAsync(vinculos);

        // Act
        var resultado = await _service.Listar();

        // Assert
        Assert.NotEmpty(resultado);
        Assert.Equal(2, resultado.Count());
        _mockDeParaCategoriaRepository.Verify(
            r => r.Listar(null), Times.Once);
    }

    #endregion

    #region Regra 5: Listar filtrando por CategoriaPierre

    [Fact]
    public async Task Listar_ComFiltroCategoriaPierre_RetornaVinculos()
    {
        // Arrange
        var categoriaPierre = "alimentacao_pierre";
        var vinculos = new List<DeParaCategoria>
        {
            new DeParaCategoria
            {
                Id = Guid.NewGuid(),
                CategoriaPierre = categoriaPierre,
                CategoriaId = Guid.NewGuid()
            }
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.Listar(categoriaPierre))
            .ReturnsAsync(vinculos);

        // Act
        var resultado = await _service.Listar(categoriaPierre);

        // Assert
        Assert.Single(resultado);
        Assert.Equal(categoriaPierre, resultado.First().CategoriaPierre);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Listar(categoriaPierre), Times.Once);
    }

    [Fact]
    public async Task Listar_ComFiltroCategoriaPierre_RetornaVazioSeNaoEncontrar()
    {
        // Arrange
        var categoriaPierre = "categoria_inexistente";

        _mockDeParaCategoriaRepository
            .Setup(r => r.Listar(categoriaPierre))
            .ReturnsAsync(new List<DeParaCategoria>());

        // Act
        var resultado = await _service.Listar(categoriaPierre);

        // Assert
        Assert.Empty(resultado);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Listar(categoriaPierre), Times.Once);
    }

    #endregion

    #region Regra 6: Editar trocando CategoriaId com sucesso

    [Fact]
    public async Task Editar_ComCategoriaNovaExistente_Sucesso()
    {
        // Arrange
        var vinculoId = Guid.NewGuid();
        var novaCategoriaId = Guid.NewGuid();
        var vinculoExistente = new DeParaCategoria
        {
            Id = vinculoId,
            CategoriaPierre = "alimentacao_pierre",
            CategoriaId = Guid.NewGuid()
        };
        var novaCategoria = new Categoria
        {
            Id = novaCategoriaId,
            Nome = "Alimentacao Fora",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorId(vinculoId))
            .ReturnsAsync(vinculoExistente);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(novaCategoriaId))
            .ReturnsAsync(novaCategoria);

        // Act
        var resultado = await _service.Editar(vinculoId, novaCategoriaId);

        // Assert
        Assert.Equal(vinculoId, resultado.Id);
        Assert.Equal(novaCategoriaId, resultado.CategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 7: Rejeitar Editar com CategoriaId novo inexistente

    [Fact]
    public async Task Editar_ComCategoriaNovaInexistente_LancaExcecao()
    {
        // Arrange
        var vinculoId = Guid.NewGuid();
        var novaCategoriaId = Guid.NewGuid();
        var vinculoExistente = new DeParaCategoria
        {
            Id = vinculoId,
            CategoriaPierre = "alimentacao_pierre",
            CategoriaId = Guid.NewGuid()
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorId(vinculoId))
            .ReturnsAsync(vinculoExistente);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(novaCategoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Editar(vinculoId, novaCategoriaId));

        Assert.Equal(novaCategoriaId, excecao.CategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 8: Rejeitar Editar/Excluir com id de vinculo inexistente

    [Fact]
    public async Task Editar_ComVinculoInexistente_LancaExcecao()
    {
        // Arrange
        var vinculoId = Guid.NewGuid();
        var novaCategoriaId = Guid.NewGuid();

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorId(vinculoId))
            .ReturnsAsync((DeParaCategoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<DeParaCategoriaNaoEncontradoException>(
            () => _service.Editar(vinculoId, novaCategoriaId));

        Assert.Equal(vinculoId, excecao.DeParaCategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Excluir_ComVinculoInexistente_LancaExcecao()
    {
        // Arrange
        var vinculoId = Guid.NewGuid();

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorId(vinculoId))
            .ReturnsAsync((DeParaCategoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<DeParaCategoriaNaoEncontradoException>(
            () => _service.Excluir(vinculoId));

        Assert.Equal(vinculoId, excecao.DeParaCategoriaId);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Remover(It.IsAny<DeParaCategoria>()), Times.Never);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 9: Excluir remove a linha fisicamente (hard delete)

    [Fact]
    public async Task Excluir_ComVinculoExistente_Sucesso()
    {
        // Arrange
        var vinculoId = Guid.NewGuid();
        var vinculoExistente = new DeParaCategoria
        {
            Id = vinculoId,
            CategoriaPierre = "alimentacao_pierre",
            CategoriaId = Guid.NewGuid()
        };

        _mockDeParaCategoriaRepository
            .Setup(r => r.ObterPorId(vinculoId))
            .ReturnsAsync(vinculoExistente);

        // Act
        await _service.Excluir(vinculoId);

        // Assert
        _mockDeParaCategoriaRepository.Verify(
            r => r.Remover(vinculoExistente), Times.Once);
        _mockDeParaCategoriaRepository.Verify(
            r => r.Salvar(), Times.Once);
    }

    #endregion
}
