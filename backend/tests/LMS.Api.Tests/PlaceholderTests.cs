// Integration tests for LMS.Api controllers and middleware live here.
// Uses Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>.
//
// Example (Phase 2+):
//   public class HealthTests(WebApplicationFactory<Program> factory)
//       : IClassFixture<WebApplicationFactory<Program>>
//   {
//       [Fact]
//       public async Task Health_ReturnsHealthy()
//       {
//           var client = factory.CreateClient();
//           var response = await client.GetAsync("/health");
//           response.EnsureSuccessStatusCode();
//       }
//   }
