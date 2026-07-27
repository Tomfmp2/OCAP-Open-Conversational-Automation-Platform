using System.ComponentModel.DataAnnotations;

namespace OCAP.Api.DTOs.Requests;

// Objeto de transferencia para las peticiones de mensajes entrantes
public class IncomingMessageRequest
{
    [Required(ErrorMessage = "El ID de usuario es obligatorio")]
    public Guid? UserId { get; set; }

    [Required(ErrorMessage = "El contenido del mensaje es obligatorio")]
    public string MessageContent { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    public string Provider { get; set; } = string.Empty;
}
