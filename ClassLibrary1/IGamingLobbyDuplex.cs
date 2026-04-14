using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SharedContracts
{
    [ServiceContract(CallbackContract = typeof(IGamingLobbyCallback))]
    public interface IGamingLobbyDuplex
    {
        // -------- User management --------
        [OperationContract]
        bool Register(string username);

        [OperationContract]
        void Unregister(string username);

        // -------- Room management --------
        [OperationContract]
        bool JoinRoomDuplex(string username, string roomName);

        [OperationContract]
        RoomSnapshot GetRoomSnapshot(string roomName);

        [OperationContract]
        void LeaveRoomDuplex(string username, string roomName);

        [OperationContract]
        bool CreateRoomDuplex(string roomName);

        [OperationContract]
        List<RoomInfo> ListRooms();

        // -------- Messaging --------
        [OperationContract]
        void SendMessageDuplex(ChatMessage msg);

        // -------- File sharing --------
        [OperationContract]
        string UploadFile(FileMeta meta, byte[] fileData);

        [OperationContract]
        byte[] DownloadFile(string fileId);

        [OperationContract]
        List<FileMeta> ListFiles(string roomName);
    }

    // -------- Callback interface --------
    public interface IGamingLobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnNewMessage(ChatMessage msg);

        [OperationContract(IsOneWay = true)]
        void OnUserListChanged(string roomName, List<string> users);

        [OperationContract(IsOneWay = true)]
        void OnFileShared(FileMeta fileMeta);
    }
}
