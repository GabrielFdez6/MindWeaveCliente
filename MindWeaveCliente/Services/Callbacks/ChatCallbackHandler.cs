using System;
using MindWeaveClient.ChatManagerService;

namespace MindWeaveClient.Services.Callbacks
{
    public class ChatCallbackHandler : IChatManagerCallback
    {
        public event Action<ChatMessageDto> OnMessageReceivedEvent;
        public event Action<string> OnSystemMessageReceivedEvent;

        public void receiveLobbyMessage(ChatMessageDto messageDto)
        {
            OnMessageReceivedEvent?.Invoke(messageDto);
        }

        public void receiveSystemMessage(string message)
        {
            OnSystemMessageReceivedEvent?.Invoke(message);
        }

    }
}