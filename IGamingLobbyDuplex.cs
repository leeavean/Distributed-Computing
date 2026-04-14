using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SharedContracts
{
    public interface IGamingLobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnNewMessage(ChatMessage msg);

        [OperationContract(IsOneWay = true)]
        void OnUserListChanged(string roomName, List<string> users);

        [OperationContract(IsOneWay = true)]
        void OnFileShared(FileMeta fileMeta);
    }

    [ServiceContract(CallbackContract = typeof(IGamingLobbyCallback))]
    public interface IGamingLobbyDuplex
    {
        [OperationContract] bool Register(string username);
        [OperationContract] void Unregister(string username);

        [OperationContract] bool CreateRoomDuplex(string roomName);
        [OperationContract] bool JoinRoomDuplex(string username, string roomName);
        [OperationContract] bool SendMessageDuplex(ChatMessage msg);
        // We will still use the polling service for streaming file upload/download in this starter.
    }
}
