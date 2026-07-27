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

    public async Task ExecuteAsync(Guid userId, string messageContent, string provider, CancellationToken cancellationToken = default)
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
            await _conversationRepository.SaveAsync(conversation, cancellationToken);
        }
        else
        {
            conversation.UpdateActivity();
            await _conversationRepository.SaveAsync(conversation, cancellationToken);
        }

        // 4. Registrar mensaje
        var message = new Message(Guid.NewGuid(), conversation.Id, content, SenderType.User);
        await _messageRepository.SaveAsync(message, cancellationToken);

        // 5. Generar evento correspondiente (opcionalmente gestionado por un mediador, por ahora se delega)
        // conversation events are registered internally
    }
}
