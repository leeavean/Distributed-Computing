using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SharedContracts;

namespace GamingLobbyServer
{
    public class Room
    {
        public string RoomName { get; set; }
        public ConcurrentDictionary<string, PlayerInfo> PlayerList { get; } = new ConcurrentDictionary<string, PlayerInfo>();
        public List<ChatMessage> MessageHistory { get; } = new List<ChatMessage>();
        public List<FileMeta> Files { get; } = new List<FileMeta>();
    }

    public static class ServerState
    {
        public static ConcurrentDictionary<string, PlayerInfo> ConnectedPlayers { get; } = new ConcurrentDictionary<string, PlayerInfo>();
        public static ConcurrentDictionary<string, Room> Rooms { get; } = new ConcurrentDictionary<string, Room>();

        // Defines the folder path where all shared files will be stored & combines the server’s base directory with a "SharedFiles" subfolder.
        public static string SharedFilesPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SharedFiles");

        // Static constructor runs once when the class is first accessed.
        
        static ServerState()
        {
            //    Ensures that the SharedFiles directory exists so file uploads won’t fail.
            if (!Directory.Exists(SharedFilesPath)) Directory.CreateDirectory(SharedFilesPath);

            if (!Directory.Exists(SharedFilesPath))
                Directory.CreateDirectory(SharedFilesPath);

            // Pre-existing players
            ConnectedPlayers.TryAdd("Scorpion", new PlayerInfo { Username = "Scorpion", LastSeenUtc = DateTime.UtcNow });
            ConnectedPlayers.TryAdd("SubZero", new PlayerInfo { Username = "SubZero", LastSeenUtc = DateTime.UtcNow });
            ConnectedPlayers.TryAdd("Raiden", new PlayerInfo { Username = "Raiden", LastSeenUtc = DateTime.UtcNow });

            // Pre-existing lobbies
            var lobby1 = new Room { RoomName = "MortalArena" };
            lobby1.PlayerList.TryAdd("Scorpion", ConnectedPlayers["Scorpion"]);
            lobby1.PlayerList.TryAdd("SubZero", ConnectedPlayers["SubZero"]);
            Rooms.TryAdd(lobby1.RoomName, lobby1);

            var lobby2 = new Room { RoomName = "ThunderClash" };
            lobby2.PlayerList.TryAdd("Raiden", ConnectedPlayers["Raiden"]);
            Rooms.TryAdd(lobby2.RoomName, lobby2);
        }

       // Returns a sorted list of all lobby room names currently on the server
       // Used by clients to display available rooms in the lobby.
        public static IEnumerable<string> RoomList() => Rooms.Keys.OrderBy(n => n);
    }
}
