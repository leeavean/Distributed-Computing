using SharedContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace GamingLobbyServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class GamingLobbyService : IGamingLobbyService
    {
        // For duplex clients: track callbacks
        private readonly object _duplexLock = new object();
        private readonly Dictionary<string, IGamingLobbyCallback> _duplexClients = new Dictionary<string, IGamingLobbyCallback>();

        // ------------------ USER MANAGEMENT ------------------
        public bool Login(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            var info = new PlayerInfo { Username = username, LastSeenUtc = DateTime.UtcNow };
            bool added = ServerState.ConnectedPlayers.TryAdd(username, info);
            Console.WriteLine($"Login attempt '{username}' => {(added ? "OK" : "REJECTED")}");
            return added;
        }

        public void Logout(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            ServerState.ConnectedPlayers.TryRemove(username, out _);
            // Remove from any rooms
            foreach (var room in ServerState.Rooms.Values)
            {
                if (room.PlayerList.TryRemove(username, out _))
                {
                    BroadcastUserListChange(room.RoomName);
                }
            }
            lock (_duplexLock) _duplexClients.Remove(username);
            Console.WriteLine($"Logout: {username}");
        }

        // ------------------ ROOMS ------------------
        public bool CreateRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return false;
            var room = new Room { RoomName = roomName };
            bool added = ServerState.Rooms.TryAdd(roomName, room);
            Console.WriteLine($"CreateRoom '{roomName}': {(added ? "OK" : "EXISTS")}");
            return added;
        }

        public bool JoinRoom(string username, string roomName)
        {
            if (!ServerState.ConnectedPlayers.ContainsKey(username)) return false;
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return false;
            room.PlayerList.TryAdd(username, new PlayerInfo { Username = username, LastSeenUtc = DateTime.UtcNow });
            BroadcastUserListChange(roomName);
            Console.WriteLine($"{username} joined {roomName}");
            return true;
        }

        public void LeaveRoom(string username, string roomName)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return;
            if (room.PlayerList.TryRemove(username, out _))
            {
                BroadcastUserListChange(roomName);
                Console.WriteLine($"{username} left {roomName}");
            }
        }

        public List<RoomInfo> ListRooms()
        {
            return ServerState.Rooms.Values
                .Select(r => new RoomInfo
                {
                    RoomName = r.RoomName,
                    PlayerList = r.PlayerList.Values.ToList()
                })
                .ToList();
        }

        // ------------------ MESSAGING ------------------
        public bool SendMessage(ChatMessage msg)
        {
            if (msg == null) return false;
            if (!ServerState.Rooms.TryGetValue(msg.Room, out var room)) return false;
            // Validate sender in room
            if (!room.PlayerList.ContainsKey(msg.From)) return false;

            msg.TimestampUtc = DateTime.UtcNow;
            room.MessageHistory.Add(msg);
            // If public: broadcast to participants (duplex if possible)
            BroadcastMessageToRoom(room.RoomName, msg);
            Console.WriteLine($"[{msg.Room}] {msg.From} -> {(string.IsNullOrEmpty(msg.To) ? "ALL" : msg.To)}: {(msg.IsFileLink ? $"FILE:{msg.FileName}" : msg.Text)}");
            return true;
        }

        public List<ChatMessage> GetMessages(string roomName, DateTime sinceUtc)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return new List<ChatMessage>();
            return room.MessageHistory.Where(m => m.TimestampUtc > sinceUtc).ToList();
        }

        public List<ChatMessage> GetPrivateMessages(string userA, string userB, DateTime sinceUtc)
        {
            // Search room(s) where both are present
            var list = new List<ChatMessage>();
            foreach (var room in ServerState.Rooms.Values)
            {
                if (room.PlayerList.ContainsKey(userA) && room.PlayerList.ContainsKey(userB))
                {
                    list.AddRange(room.MessageHistory.Where(m =>
                        m.TimestampUtc > sinceUtc &&
                        ((m.From == userA && m.To == userB) || (m.From == userB && m.To == userA))
                    ));
                }
            }
            return list.OrderBy(m => m.TimestampUtc).ToList();
        }

        // ------------------ FILE STREAMING ------------------
        public string UploadFile(FileMeta meta, byte[] fileData)
        {
            if (meta == null || string.IsNullOrWhiteSpace(meta.Uploader) || string.IsNullOrWhiteSpace(meta.Room))
                throw new FaultException("Invalid metadata");

            if (!ServerState.Rooms.TryGetValue(meta.Room, out var room))
                throw new FaultException("Room not found");

            if (!room.PlayerList.ContainsKey(meta.Uploader))
                throw new FaultException("Uploader not in room");

            var ext = Path.GetExtension(meta.FileName ?? "").ToLowerInvariant();
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".txt" };
            if (!allowed.Contains(ext)) throw new FaultException("File type not allowed");

            var fileId = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(ServerState.SharedFilesPath, fileId);

            File.WriteAllBytes(path, fileData);  // <-- save byte array directly

            meta.FileId = fileId;
            meta.UploadedUtc = DateTime.UtcNow;
            room.Files.Add(meta);

            var msg = new ChatMessage
            {
                From = meta.Uploader,
                Room = meta.Room,
                TimestampUtc = DateTime.UtcNow,
                IsFileLink = true,
                FileId = meta.FileId,
                FileName = meta.FileName,
                Text = $"{meta.Uploader} shared {meta.FileName}"
            };
            room.MessageHistory.Add(msg);
            BroadcastFileShared(room.RoomName, meta);
            BroadcastMessageToRoom(room.RoomName, msg);
            Console.WriteLine($"File uploaded: {meta.FileName} by {meta.Uploader} in {meta.Room}");
            return meta.FileId;
        }


        public string GetFileId(string roomName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(fileName))
                throw new FaultException("Invalid parameters");

            if (!ServerState.Rooms.TryGetValue(roomName, out var room))
                throw new FaultException("Room not found");

            var file = room.Files.FirstOrDefault(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (file == null)
                throw new FaultException("File not found");

            return file.FileId;
        }

        public Stream DownloadFile(string fileId)
        {
            var path = Path.Combine(ServerState.SharedFilesPath, fileId);
            if (!File.Exists(path)) throw new FaultException("File not found");
            // Open as FileStream for streaming
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public List<FileMeta> ListFiles(string roomName, DateTime sinceUtc)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return new List<FileMeta>();
            return room.Files.Where(f => f.UploadedUtc > sinceUtc).ToList();
        }

        public LobbyUpdateBundle GetUpdates(string username, DateTime sinceUtc)
        {
            var bundle = new LobbyUpdateBundle { ServerTimeUtc = DateTime.UtcNow };
            // find room the user is in (only one room at a time assumed)
            var room = ServerState.Rooms.Values.FirstOrDefault(r => r.PlayerList.ContainsKey(username));
            if (room != null)
            {
                bundle.Messages.AddRange(room.MessageHistory.Where(m => m.TimestampUtc > sinceUtc));
                bundle.Files.AddRange(room.Files.Where(f => f.UploadedUtc > sinceUtc));
                bundle.PlayerList = room.PlayerList.Values.ToList();
            }
            else
            {
                // if not in a room, just send available rooms and no messages
                bundle.PlayerList = ServerState.ConnectedPlayers.Values.ToList();
            }
            return bundle;
        }

        // ------------------ Duplex tracking ------------------
        public bool Register(string username)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IGamingLobbyCallback>();
            lock (_duplexLock)
            {
                _duplexClients[username] = callback;
            }
            Console.WriteLine($"Duplex register: {username}");
            return true;
        }

        public void Unregister(string username)
        {
            lock (_duplexLock)
            {
                _duplexClients.Remove(username);
            }
            Console.WriteLine($"Duplex unregister: {username}");
        }


        // ------------------ Broadcast helpers ------------------
        private void BroadcastMessageToRoom(string roomName, ChatMessage msg)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return;
            List<string> participants = room.PlayerList.Keys.ToList();
            lock (_duplexLock)
            {
                foreach (var p in participants)
                {
                    if (_duplexClients.TryGetValue(p, out var cb))
                    {
                        try
                        {
                            cb.OnNewMessage(msg);
                        }
                        catch { /* ignore callback faults */ }
                    }
                }
            }
        }

        private void BroadcastUserListChange(string roomName)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return;
            var users = room.PlayerList.Keys.ToList();
            lock (_duplexLock)
            {
                foreach (var u in users)
                {
                    if (_duplexClients.TryGetValue(u, out var cb))
                    {
                        try { cb.OnUserListChanged(roomName, users); } catch { }
                    }
                }
            }
        }

        private void BroadcastFileShared(string roomName, FileMeta meta)
        {
            if (!ServerState.Rooms.TryGetValue(roomName, out var room)) return;
            lock (_duplexLock)
            {
                foreach (var p in room.PlayerList.Keys)
                {
                    if (_duplexClients.TryGetValue(p, out var cb))
                    {
                        try { cb.OnFileShared(meta); } catch { }
                    }
                }
            }
        }
    }
}

