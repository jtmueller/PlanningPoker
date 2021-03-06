﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanningPoker.Hubs
{
    public class RoomUser
    {
        public string ConnectionId { get; set; }
        public string Username { get; set; }
        public int Vote { get; set; }
    }

    public class PokerHub : Hub
    {
        private readonly static Dictionary<string, string> connections = new Dictionary<string, string>();

        // rooms -> Key: roomName -> Value: list of RoomUser instances
        private readonly static Dictionary<string, List<RoomUser>> rooms = new Dictionary<string, List<RoomUser>>();

        private string getUsername(string connectionId)
        {
            var kvp = connections.SingleOrDefault(c => c.Key == connectionId);
            if (!kvp.Equals(default(KeyValuePair<string, string>)))
                return kvp.Value;
            return null;
        }

        private string currentUsername
        {
            get { return getUsername(Context.ConnectionId); }
        }

        private void removeRoomUser(string roomName)
        {
            var list = rooms[roomName];
            var user = list.Find(u => u.ConnectionId == Context.ConnectionId);
            list.Remove(user);

            if (list.Count() == 0)
            {
                rooms.Remove(roomName);
                Clients.All.listRooms(rooms.Keys);
            }
        }

        private void updateVoteValue(string roomName, int cardValue)
        {
            var list = rooms[roomName];
            var user = list.Find(u => u.ConnectionId == Context.ConnectionId);
            user.Vote = cardValue;
        }

        private void resetVotes(string roomName)
        {
            var list = rooms[roomName];
            list.ForEach(user => user.Vote = -1);
        }
        
        private void updateRoomUsers(string roomName)
        {
            Clients.Group(roomName).updateRoomUsers(rooms[roomName]);
        }

        public void Login(string username)
        {
            if (connections.ContainsKey(Context.ConnectionId))
                connections[Context.ConnectionId] = username;
            else
                connections.Add(Context.ConnectionId, username);

            Clients.All.updateUserConnections(connections.Values);
            Clients.AllExcept(Context.ConnectionId).userConnect(username);
        }

        public string GetUsername()
        {
            return currentUsername;
        }

        public async void ConnectToRoom(string roomName)
        {
            var user = new RoomUser { ConnectionId = Context.ConnectionId, Username = getUsername(Context.ConnectionId), Vote = -1 };
            if (rooms.Keys.Contains(roomName))
            {
                rooms[roomName].Add(user);
            } else
            {
                rooms.Add(roomName, new List<RoomUser> { user });
                Clients.All.listRooms(rooms.Keys);
            }
            await Groups.Add(Context.ConnectionId, roomName);
            //TODO: notify single user added to room?
            updateRoomUsers(roomName);
        }

        public async void DisconnectFromRoom(string roomName)
        {
            await Groups.Remove(Context.ConnectionId, roomName);
            removeRoomUser(roomName);
            if (rooms.ContainsKey(roomName))
                updateRoomUsers(roomName);
            //else
                //TODO: notify single user leaving room?
        }

        public void ListRooms()
        {
            Clients.Caller.listRooms(rooms.Keys);
        }

        public void UpdateDescription(string roomName, string description)
        {
            Clients.Group(roomName).descriptionUpdated(description);
        }

        public void RequestVotes(string roomName)
        {
            Clients.Group(roomName).voteRequested();
        }

        public void SubmitVote(string roomName, int cardValue)
        {
            updateVoteValue(roomName, cardValue);
            updateRoomUsers(roomName);
        }

        public void ResetVotes(string roomName)
        {
            resetVotes(roomName);
            updateRoomUsers(roomName);
        }

        public override Task OnConnected()
        {
            connections.Add(Context.ConnectionId, "TBD");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            connections.Remove(Context.ConnectionId);
            Clients.All.updateUserConnections(connections.Values);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            if (!connections.ContainsKey(Context.ConnectionId))
            {
                Clients.All.updateUserConnections(connections.Values);
                string un = (currentUsername == null) ? "TBD" : currentUsername;
                connections.Add(Context.ConnectionId, un);
            }

            return base.OnReconnected();
        }
    }
}
