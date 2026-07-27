using FluentAssertions;
using OCAP.Providers.Google.Sheets;
using OCAP.Tools.Abstractions;
using OCAP.Tools.Google;

namespace OCAP.Tools.Tests;

public class SheetsToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidParameters_AppendsRowToSpreadsheet()
    {
        // Arrange
        var provider = new InMemorySpreadsheetProvider();
        var tool = new AppendSpreadsheetRowTool(provider);

        var parameters = new Dictionary<string, object>
        {
            ["SpreadsheetId"] = "sheet-12345",
            ["SheetName"] = "Ventas",
            ["Values"] = new List<object> { "Item 1", 100, DateTime.UtcNow.ToString("yyyy-MM-dd") }
        };

        var context = new ToolExecutionContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), parameters);

        // Act
        var result = await tool.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();

        var rows = await provider.ReadRowsAsync("sheet-12345", "Ventas");
        rows.Should().HaveCount(1);
        rows.First().Should().Contain("Item 1");
    }
}
