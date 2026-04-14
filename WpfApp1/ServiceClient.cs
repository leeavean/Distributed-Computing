using SharedContracts;
using System;
using System.ServiceModel;

namespace WpfApp1
{
    public class ServiceClient
    {
        // Singleton instance
        private static ServiceClient _instance;

        public static ServiceClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceClient();
                }
                return _instance;
            }
        }

        // WCF service client proxy
        private readonly IGamingLobbyService _client;

        private ServiceClient()
        {
            var binding = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = 2147483647,
                MaxBufferSize = 2147483647,
                MaxBufferPoolSize = 2147483647,
                TransferMode = TransferMode.Streamed,
            };

            // Set the reader quotas
            binding.ReaderQuotas.MaxArrayLength = int.MaxValue;   // Very large
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
            binding.ReaderQuotas.MaxDepth = 32;
            binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;


            var endpoint = new EndpointAddress("net.tcp://localhost:9000/GamingLobbyService");
            var channelFactory = new ChannelFactory<IGamingLobbyService>(binding, endpoint);
            _client = channelFactory.CreateChannel();
        }

        // --- User Management ---
        public bool Login(string username) => _client.Login(username);
        public void Logout(string username) => _client.Logout(username);

        // --- Rooms ---
        public bool CreateRoom(string roomName) => _client.CreateRoom(roomName);
        public bool JoinRoom(string username, string roomName) => _client.JoinRoom(username, roomName);
        public void LeaveRoom(string username, string roomName) => _client.LeaveRoom(username, roomName);
        public System.Collections.Generic.List<RoomInfo> ListRooms() => _client.ListRooms();

        // --- Messaging ---
        public bool SendMessage(ChatMessage msg) => _client.SendMessage(msg);
        public System.Collections.Generic.List<ChatMessage> GetMessages(string roomName, DateTime sinceUtc)
            => _client.GetMessages(roomName, sinceUtc);
        public System.Collections.Generic.List<ChatMessage> GetPrivateMessages(string userA, string userB, DateTime sinceUtc)
            => _client.GetPrivateMessages(userA, userB, sinceUtc);

        // --- File Handling ---
        public string UploadFile(FileMeta meta, byte[] fileData) => _client.UploadFile(meta, fileData);
        public System.IO.Stream DownloadFile(string fileId) => _client.DownloadFile(fileId);
        public string GetFileId(string roomName, string fileName) => _client.GetFileId(roomName, fileName);
        public System.Collections.Generic.List<FileMeta> ListFiles(string roomName, DateTime sinceUtc) => _client.ListFiles(roomName, sinceUtc);

        // --- Combined updates for polling client ---
        public LobbyUpdateBundle GetUpdates(string username, DateTime sinceUtc) => _client.GetUpdates(username, sinceUtc);
    }
}

