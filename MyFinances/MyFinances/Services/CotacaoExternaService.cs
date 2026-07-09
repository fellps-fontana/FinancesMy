using System.Text.Json;
using MyFinances.DTOs.Cotacao;
using MyFinances.Exceptions;

namespace MyFinances.Services;

public class CotacaoExternaService : ICotacaoExternaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CotacaoExternaService> _logger;

    public CotacaoExternaService(HttpClient httpClient, ILogger<CotacaoExternaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _logger.LogError("ERRO: HttpClient para CotacaoExternaService foi resolvido sem BaseAddress configurada. Chamadas a API falharam.");
        }
        else
        {
            _logger.LogInformation("HttpClient para Brapi inicializado com BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
        }
    }

    public async Task<CotacaoHistoricoResponse> ObterHistoricoCotacao(string ticker, string range = "1mo")
    {
        try
        {
            ValidarTicker(ticker);
            var response = await ChamarApiExterna(ticker, range);
            return await ParsearResposta(response, ticker);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (TickerNaoEncontradoException)
        {
            throw;
        }
        catch (CotacaoExternaIndisponibilException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexao ao chamar API Brapi para ticker {Ticker}.", ticker);
            throw new CotacaoExternaIndisponibilException("Nao foi possivel conectar a API de cotacao. Tente novamente.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao desserializar resposta da Brapi para ticker {Ticker}.", ticker);
            throw new CotacaoExternaIndisponibilException("Erro ao processar resposta da API de cotacao.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro nao esperado ao obter cotacao do ticker {Ticker}.", ticker);
            throw new CotacaoExternaIndisponibilException("Erro ao obter cotacao. Tente novamente.", ex);
        }
    }

    private static void ValidarTicker(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentException("Ticker nao pode estar vazio.", nameof(ticker));
        }
    }

    private async Task<HttpResponseMessage> ChamarApiExterna(string ticker, string range)
    {
        var url = $"api/quote/{ticker}?range={range}&interval=1d";
        return await _httpClient.GetAsync(url);
    }

    private async Task<CotacaoHistoricoResponse> ParsearResposta(HttpResponseMessage response, string ticker)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Ticker {Ticker} nao encontrado na API Brapi.", ticker);
            throw new TickerNaoEncontradoException($"Ticker '{ticker}' nao encontrado.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Brapi retornou status {StatusCode} para ticker {Ticker}.", response.StatusCode, ticker);
            throw new CotacaoExternaIndisponibilException($"API Brapi retornou erro {response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        if (!jsonDoc.RootElement.TryGetProperty("results", out var resultsElement))
        {
            _logger.LogError("Resposta da Brapi sem campo 'results' para ticker {Ticker}.", ticker);
            throw new CotacaoExternaIndisponibilException("Formato de resposta inesperado da API Brapi.");
        }

        var pontos = ExtrairPontosCotacao(resultsElement);

        return new CotacaoHistoricoResponse
        {
            Ticker = ticker.ToUpper(),
            Pontos = pontos
        };
    }

    private static List<PontoCotacaoResponse> ExtrairPontosCotacao(JsonElement resultsElement)
    {
        var pontos = new List<PontoCotacaoResponse>();

        if (resultsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in resultsElement.EnumerateArray())
            {
                if (item.TryGetProperty("date", out var dateElement) &&
                    item.TryGetProperty("close", out var closeElement) &&
                    dateElement.TryGetInt64(out var unixTimestamp) &&
                    closeElement.TryGetDecimal(out var preco))
                {
                    var data = UnixTimeStampParaDateTime(unixTimestamp);
                    pontos.Add(new PontoCotacaoResponse { Data = data, Preco = preco });
                }
            }
        }

        return pontos;
    }

    private static DateTime UnixTimeStampParaDateTime(long unixTimestamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimestamp).ToUniversalTime();
        return dateTime;
    }
}
