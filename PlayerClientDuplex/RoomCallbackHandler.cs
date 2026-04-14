using System.Collections.Generic;
using System.Windows;
using SharedContracts;

namespace PlayerClientDuplex
{
    public class RoomCallbackHandler : IGamingLobbyCallback
    {
        private RoomPage _roomPage;
        private PrivateChatPage _privateChatPage;

        public void SetRoomPage(RoomPage page) => _roomPage = page;

        public void OnNewMessage(ChatMessage msg)
        {
            string _username = _roomPage.getUsername();
            // Determine if it's private for this client
            if (!string.IsNullOrEmpty(msg.To))
            {
                if (msg.To == _username || msg.From == _username)
                    _privateChatPage?.AddMessage(msg); // call the correct PrivateChatPage
            }
            else
            {
                _roomPage?.AddMessage(msg); // public messages go to room
            }
        }


        public void OnUserListChanged(string roomName, List<string> users)
        {
            if (_roomPage != null && _roomPage.RoomName == roomName)
                _roomPage.UpdateUserList(users);
        }

        public void OnFileShared(FileMeta fileMeta)
        {
            if (_roomPage != null && _roomPage.RoomName == fileMeta.Room)
            {
                _roomPage.lstMessages.Dispatcher.Invoke(() =>
                {
                    _roomPage.lstMessages.Items.Add($"{fileMeta.Uploader} shared: {fileMeta.FileName}");
                    _roomPage.lstMessages.ScrollIntoView(_roomPage.lstMessages.Items[_roomPage.lstMessages.Items.Count - 1]);
                    _roomPage.AddFile(fileMeta);
                });
            }
        }
    }
}
