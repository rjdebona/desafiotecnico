using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using LancamentoService.API; // partial Program

namespace LancamentoService.Tests.IntegrationTests
{
    public class FluxoDeCaixaAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public FluxoDeCaixaAuthIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

        [Fact(DisplayName = nameof(GetFluxos_WithoutToken_ReturnsUnauthorized))]
        public async Task GetFluxos_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/FluxoDeCaixa");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = nameof(GetFluxos_WithValidToken_ReturnsOk))]
        public async Task GetFluxos_WithValidToken_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var loginPayload = new { Username = "admin", Password = "password" };
            var json = JsonSerializer.Serialize(loginPayload);
            var tokenResp = await client.PostAsync("/auth/token", new StringContent(json, Encoding.UTF8, "application/json"));
            var tokenBody = await tokenResp.Content.ReadAsStringAsync();
            Assert.True(tokenResp.IsSuccessStatusCode, $"Token endpoint failed: {tokenResp.StatusCode} Body: {tokenBody}");
            var tokenJson = JsonDocument.Parse(tokenBody);
            var token = tokenJson.RootElement.GetProperty("access_token").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/FluxoDeCaixa");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
