using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace SharedContracts
{
    [ServiceContract]
    public interface IGamingLobbyService
    {
        // User management
        [OperationContract] bool Login(string username);
        [OperationContract] void Logout(string username);

        // Rooms
        [OperationContract] bool CreateRoom(string roomName);
        [OperationContract] bool JoinRoom(string username, string roomName);
        [OperationContract] void LeaveRoom(string username, string roomName);
        [OperationContract] List<RoomInfo> ListRooms();

        // Messaging
        [OperationContract] bool SendMessage(ChatMessage msg);
        [OperationContract] List<ChatMessage> GetMessages(string roomName, DateTime sinceUtc);
        [OperationContract] List<ChatMessage> GetPrivateMessages(string userA, string userB, DateTime sinceUtc);


        [OperationContract]
        string UploadFile(FileMeta meta, byte[] fileData);


        [OperationContract]
        Stream DownloadFile(string fileId);

        [OperationContract]
        string GetFileId(string roomName, string fileName);


        [OperationContract]
        List<FileMeta> ListFiles(string roomName, DateTime sinceUtc);

        // Convenience: combined updates for polling client
        [OperationContract]
        LobbyUpdateBundle GetUpdates(string username, DateTime sinceUtc);


    }
}
