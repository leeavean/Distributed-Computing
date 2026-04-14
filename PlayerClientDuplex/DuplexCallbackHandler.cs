using System;
using System.Collections.Generic;
using System.Linq;
using PlayerClientDuplex;
using SharedContracts;

public class DuplexCallbackHandler : IGamingLobbyCallback
{
    private RoomPage _page;

    public DuplexCallbackHandler(RoomPage page) => _page = page;

    public void SetPage(RoomPage page) => _page = page;

    public void OnNewMessage(ChatMessage msg)
    {
        if (_page == null || _page.RoomName != msg.Room) return;

        _page.Dispatcher.BeginInvoke(new Action(() =>
        {
            _page.lstMessages.Items.Add(msg.IsFileLink
                ? msg.From + " shared file: " + msg.FileName
                : msg.From + ": " + msg.Text);

            // Scroll to last item safely
            if (_page.lstMessages.Items.Count > 0)
                _page.lstMessages.ScrollIntoView(_page.lstMessages.Items[_page.lstMessages.Items.Count - 1]);
        }));
    }

    public void OnUserListChanged(string roomName, List<string> users)
    {
        if (_page == null || _page.RoomName != roomName) return;

        _page.Dispatcher.BeginInvoke(new Action(() =>
        {
            _page.lstUsers.ItemsSource = users;
        }));
    }

    public void OnFileShared(FileMeta fileMeta)
    {
        if (_page == null || _page.RoomName != fileMeta.Room) return;

        _page.Dispatcher.BeginInvoke(new Action(() =>
        {
            var files = _page.lstFiles.Items.Cast<string>().ToList();
            if (!files.Contains(fileMeta.FileName))
                files.Add(fileMeta.FileName);
            _page.lstFiles.ItemsSource = files;
        }));
    }

}
