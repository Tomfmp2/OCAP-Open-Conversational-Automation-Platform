using OCAP.Core.Entities;
using OCAP.Core.Events;
using OCAP.Core.Ports;
using OCAP.Core.ValueObjects;

namespace OCAP.Application.UseCases;

public class ReceiveMessageUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;

    public ReceiveMessageUseCase(
        IUserRepository userRepository, 
        IConversationRepository conversationRepository, 
        IMessageRepository messageRepository)
    {
        _userRepository = userRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task ExecuteAsync(Guid userId, string messageContent, string provider, CancellationToken cancellationToken = default) =>
        await ReceiveAsync(userId, messageContent, provider, externalContactId: null, channelConnectionId: null, cancellationToken);

    /// <summary>
    /// Registra el mensaje entrante en la conversación abierta del contacto (o en una nueva) y la devuelve.
    /// Con <paramref name="externalContactId"/> la conversación queda asociada a su canal, para responder desde el panel.
    /// </summary>
    public async Task<Conversation> ReceiveAsync(
        Guid userId,
        string messageContent,
        string provider,
        string? externalContactId,
        Guid? channelConnectionId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validar mensaje
        var content = new MessageContent(messageContent);
        var identifier = new UserIdentifier(userId.ToString(), provider);

        // 2. Buscar usuario
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }
        
        if (user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("User is not active.");
        }

        // 3. Crear conversación si no existe (buscar conversacion activa)
        var conversation = await _conversationRepository.GetByUserIdAsync(userId, cancellationToken);
        if (conversation == null || conversation.Status == ConversationStatus.Closed)
        {
            conversation = new Conversation(Guid.NewGuid(), userId);
        }
        else
        {
            conversation.UpdateActivity();
        }

        if (!string.IsNullOrWhiteSpace(externalContactId))
        {
            conversation.AttachChannel(provider, externalContactId, channelConnectionId);
        }
        await _conversationRepository.SaveAsync(conversation, cancellationToken);

        // 4. Registrar mensaje
        var message = new Message(Guid.NewGuid(), conversation.Id, content, SenderType.User);
        await _messageRepository.SaveAsync(message, cancellationToken);
        return conversation;
    }

    /// <summary>Máximo que admite un mensaje guardado (y que aceptan WhatsApp y Telegram en un solo envío).</summary>
    public const int MaxMessageLength = 4096;

    /// <summary>Guarda en la conversación la respuesta del agente o de un operador (recortada a <see cref="MaxMessageLength"/>).</summary>
    public async Task RecordReplyAsync(Guid conversationId, string text, SenderType sender, CancellationToken cancellationToken = default)
    {
        var content = text.Length > MaxMessageLength ? text[..MaxMessageLength] : text;
        await _messageRepository.SaveAsync(new Message(Guid.NewGuid(), conversationId, new MessageContent(content), sender), cancellationToken);
    }
}
