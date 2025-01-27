﻿using System.Diagnostics;
using Maple2Storage.Types;
using MapleServer2.Database;
using MapleServer2.Enums;
using MapleServer2.Managers;
using MapleServer2.Network;
using MapleServer2.Packets;
using MapleServer2.Tools;
using MapleServer2.Types;

namespace MapleServer2.Servers.Game;

public class GameSession : Session
{
    protected override PatchType Type => PatchType.Ignore;

    public int ServerTick;
    public int ClientTick;

    public Player Player;

    public FieldManager FieldManager { get; private set; }
    private readonly FieldManagerFactory FieldManagerFactory;

    public GameSession(FieldManagerFactory fieldManagerFactory)
    {
        FieldManagerFactory = fieldManagerFactory;
    }

    public void SendNotice(string message)
    {
        Send(ChatPacket.Send(Player, message, ChatType.NoticeAlert));
    }

    // Called first time when starting a new session
    public void InitPlayer(Player player)
    {
        Debug.Assert(player.FieldPlayer == null, "Not allowed to reinitialize player.");

        Player = player;
        FieldManager = FieldManagerFactory.GetManager(player);
        player.FieldPlayer = FieldManager.RequestCharacter(player);
    }

    public void EnterField(Player player)
    {
        // If moving maps, need to get the FieldManager for new map
        if (player.MapId != FieldManager.MapId || player.InstanceId != FieldManager.InstanceId)
        {
            FieldManager.RemovePlayer(this); // Leave previous field

            if (FieldManagerFactory.Release(FieldManager.MapId, FieldManager.InstanceId, player))
            {
                //If instance is destroyed, reset dungeonSession
                DungeonSession dungeonSession = GameServer.DungeonManager.GetDungeonSessionByInstanceId(FieldManager.InstanceId);
                //check if the destroyed map was a dungeon map
                if (dungeonSession != null && FieldManager.InstanceId == dungeonSession.DungeonInstanceId
                    && dungeonSession.IsDungeonSessionMap(FieldManager.MapId))
                {
                    GameServer.DungeonManager.ResetDungeonSession(player, dungeonSession);
                }
            }

            // Initialize for new Map
            FieldManager = FieldManagerFactory.GetManager(player);
            player.FieldPlayer = FieldManager.RequestCharacter(player);
        }

        FieldManager.AddPlayer(this);
    }

    protected override void EndSession(bool logoutNotice)
    {
        FieldManagerFactory.Release(FieldManager.MapId, FieldManager.InstanceId, Player);

        FieldManager.RemovePlayer(this);
        GameServer.PlayerManager.RemovePlayer(Player);

        Player.OnlineCTS.Cancel();
        Player.OnlineTimeThread = null;

        CoordF safeCoord = Player.SafeBlock;
        safeCoord.Z += Block.BLOCK_SIZE;
        Player.SavedCoord = safeCoord;

        // if session is not changing channels or servers, send the logout message
        if (logoutNotice)
        {
            Player.Session = null;
            GameServer.BuddyManager.SetFriendSessions(Player);

            Player.Party?.CheckOfflineParty(Player);

            Player.Guild?.BroadcastPacketGuild(GuildPacket.MemberLoggedOff(Player));

            Player.UpdateBuddies();

            Player.IsMigrating = false;

            AuthData authData = Player.Account.AuthData;
            authData.OnlineCharacterId = 0;
            DatabaseManager.AuthData.UpdateOnlineCharacterId(authData);
        }

        DatabaseManager.Characters.Update(Player);
    }
}
