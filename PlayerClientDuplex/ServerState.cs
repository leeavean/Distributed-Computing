using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using SharedContracts;

namespace PlayerClientDuplex
{
    // ---------------- Domain Models ----------------
    public class Room
    {
        public string RoomName { get; set; } = string.Empty;

        public ConcurrentDictionary<string, PlayerInfo> PlayerList { get; }
            = new ConcurrentDictionary<string, PlayerInfo>();

        public List<ChatMessage> MessageHistory { get; }
            = new List<ChatMessage>();

        public List<FileMeta> Files { get; }
            = new List<FileMeta>();
    }

    // ---------------- Server State ----------------
    public static class ServerState
    {
        // Track connected players
        public static ConcurrentDictionary<string, PlayerInfo> ConnectedPlayers { get; }
            = new ConcurrentDictionary<string, PlayerInfo>();

        // Track active rooms
        public static ConcurrentDictionary<string, Room> Rooms { get; }
            = new ConcurrentDictionary<string, Room>();

        // Track duplex callbacks
        public static ConcurrentDictionary<string, IGamingLobbyCallback> Callbacks { get; }
            = new ConcurrentDictionary<string, IGamingLobbyCallback>();

        // Shared files folder
        public static string SharedFilesPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SharedFiles");

        // ---------------- Initialization ----------------
        static ServerState()
        {
            EnsureSharedFilesDirectory();

            SeedPlayers();
            SeedRooms();
        }

        private static void EnsureSharedFilesDirectory()
        {
            if (!Directory.Exists(SharedFilesPath))
                Directory.CreateDirectory(SharedFilesPath);
        }

        public static RoomSnapshot GetRoomSnapshot(string roomName)
        {
            if (!Rooms.TryGetValue(roomName, out var room))
                return new RoomSnapshot();

            return new RoomSnapshot
            {
                Users = room.PlayerList.Keys.ToList(), // pre-existing users included
                Messages = room.MessageHistory.ToList(),
                Files = room.Files.ToList()
            };
        }

        private static void SeedPlayers()
        {
            ConnectedPlayers.TryAdd("Alpha", new PlayerInfo { Username = "Alpha", LastSeenUtc = DateTime.UtcNow });
            ConnectedPlayers.TryAdd("Bravo", new PlayerInfo { Username = "Bravo", LastSeenUtc = DateTime.UtcNow });
            ConnectedPlayers.TryAdd("Charlie", new PlayerInfo { Username = "Charlie", LastSeenUtc = DateTime.UtcNow });
        }

        private static void SeedRooms()
        {
            var room1 = new Room { RoomName = "ArenaOne" };
            room1.PlayerList.TryAdd("Alpha", ConnectedPlayers["Alpha"]);
            room1.PlayerList.TryAdd("Bravo", ConnectedPlayers["Bravo"]);
            Rooms.TryAdd(room1.RoomName, room1);

            var room2 = new Room { RoomName = "BattleZone" };
            room2.PlayerList.TryAdd("Charlie", ConnectedPlayers["Charlie"]);
            Rooms.TryAdd(room2.RoomName, room2);
        }

        // ---------------- Helpers ----------------
        public static IEnumerable<string> RoomList()
            => Rooms.Keys.OrderBy(n => n);

        public static void RegisterCallback(string username, IGamingLobbyCallback callback)
        {
            Callbacks[username] = callback;
        }

        public static IGamingLobbyCallback GetCallback(string username)
        {
            Callbacks.TryGetValue(username, out var callback);
            return callback; 
        }
        public static bool JoinRoomDuplex(string username, string roomName)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room))
                return false;

            if (!room.PlayerList.ContainsKey(username))
            {
                room.PlayerList.TryAdd(username, ServerState.ConnectedPlayers[username]);
            }

            // Capture the duplex callback channel for this user
            var callback = OperationContext.Current.GetCallbackChannel<IGamingLobbyCallback>();
            ServerState.RegisterCallback(username, callback);

            // Notify other players in the room
            foreach (var player in room.PlayerList.Values)
            {
                var cb = ServerState.GetCallback(player.Username);
                if (cb != null)   
                {
                    cb.OnUserListChanged(roomName, new List<string>(room.PlayerList.Keys));
                }
            }

            return true;
        }

    }
}