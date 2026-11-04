using FluentAssertions;
using OCAP.Core.Privacy;

namespace OCAP.Api.Tests.Services;

public class SensitiveDataTests
{
    [Theory]
    [InlineData("Mi tarjeta es 4111 1111 1111 1111.", "Mi tarjeta es [tarjeta oculta].")]
    [InlineData("4111-1111-1111-1111", "[tarjeta oculta]")]
    [InlineData("Mi clave es Secreta123 gracias", "Mi clave es [oculto] gracias")]
    [InlineData("contraseña: hola", "contraseña: [oculto]")]
    [InlineData("PIN=4821", "PIN=[oculto]")]
    [InlineData("tarjeta 4111 1111 1111 1111 123", "tarjeta [tarjeta oculta]")] // pegada a su CVV
    [InlineData("99 4111.1111.1111.1111", "[tarjeta oculta]")] // otra cifra delante y puntos
    [InlineData("mi pin 4821", "mi pin [oculto]")]
    [InlineData("mi cvv de 123", "mi cvv de [oculto]")]
    [InlineData("my password is Hola123", "my password is [oculto]")]
    [InlineData("password: dos palabras", "password: [oculto]")]
    [InlineData("la contraseña de mi correo es hola123", "la contraseña de mi correo es [oculto]")]
    [InlineData("contrasen\u0303a: hola", "contraseña: [oculto]")] // tilde como carácter aparte
    public void OcultaLosDatosSensibles(string text, string expected) =>
        SensitiveData.Mask(text).Should().Be(expected);

    [Theory]
    [InlineData("Mi pedido es 1234 5678 9012 3456")] // no pasa Luhn
    [InlineData("Mi celular es 3001234567")]
    [InlineData("La clave es la paciencia")]
    [InlineData("Quiero 2 docenas de empanadas")]
    [InlineData("El pin de la camisa es grande")]
    public void NoTocaElTextoNormal(string text) =>
        SensitiveData.Mask(text).Should().Be(text);
}
