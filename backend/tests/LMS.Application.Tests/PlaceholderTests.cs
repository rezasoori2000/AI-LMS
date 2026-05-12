// Unit tests for Application layer content services live here.
// Services use ILmsDbContext directly (no MediatR/CQRS in this codebase).
// Use a fake/in-memory ILmsDbContext implementation — no database required.
//
// Example (Phase 2+):
//   public class GradeServiceTests
//   {
//       [Fact]
//       public async Task CreateAsync_ValidRequest_ReturnsDtoWithCorrectFields() { ... }
//   }
