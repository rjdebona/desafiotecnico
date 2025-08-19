using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ConsolidacaoService.Tests.IntegrationTests;

public class SaldoDiarioIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public SaldoDiarioIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetSaldoDiario_SemToken_Unauthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/SaldoDiario?data=2025-08-16");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
