using System.Text.RegularExpressions;
using Xunit;

namespace OCAP.UnitTests;

/// <summary>
/// La organización de cada conexión (RLS) la fija un interceptor de EF al abrirla. Abrir la conexión del DbContext a
/// mano (GetDbConnection().OpenAsync) se salta ese interceptor y deja la petición sin organización: los canales
/// dejaron de responder por esto. Hay que usar Database.OpenConnectionAsync.
/// </summary>
public class DbConnectionUsageTests
{
    [Fact]
    public void NoCodeOpensTheDbContextConnectionDirectly()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "OCAP.slnx"))) root = root.Parent;
        Assert.NotNull(root);

        var offenders = Directory.EnumerateFiles(Path.Combine(root!.FullName, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"connection\.Open(Async)?\(") && File.ReadAllText(f).Contains("GetDbConnection()"))
            .Select(f => Path.GetRelativePath(root.FullName, f))
            .ToList();

        // La conexión del DbContext se abre con Database.OpenConnectionAsync para que se aplique RLS.
        Assert.Empty(offenders);
    }
}
