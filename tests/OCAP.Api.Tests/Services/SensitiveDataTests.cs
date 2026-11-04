using FluentAssertions;
using OCAP.Api.Services;

namespace OCAP.Api.Tests.Services;

public class SensitiveDataTests
{
    [Theory]
    [InlineData("Mi tarjeta es 4111 1111 1111 1111.", "Mi tarjeta es [tarjeta oculta].")]
    [InlineData("4111-1111-1111-1111", "[tarjeta oculta]")]
    [InlineData("Mi clave es Secreta123 gracias", "Mi clave es [oculto] gracias")]
    [InlineData("contraseña: hola", "contraseña: [oculto]")]
    [InlineData("PIN=4821", "PIN=[oculto]")]
    public void OcultaLosDatosSensibles(string text, string expected) =>
        SensitiveData.Mask(text).Should().Be(expected);

    [Theory]
    [InlineData("Mi pedido es 1234 5678 9012 3456")] // no pasa Luhn
    [InlineData("Mi celular es 3001234567")]
    [InlineData("La clave es la paciencia")]
    [InlineData("Quiero 2 docenas de empanadas")]
    public void NoTocaElTextoNormal(string text) =>
        SensitiveData.Mask(text).Should().Be(text);
}
