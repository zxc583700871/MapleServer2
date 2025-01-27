﻿using MaplePacketLib2.Tools;
using MapleServer2.Constants;

namespace MapleServer2.Packets;

public static class SystemSoundPacket
{
    public static PacketWriter Play(string sound)
    {
        PacketWriter pWriter = PacketWriter.Of(SendOp.PLAY_SYSTEM_SOUND);
        pWriter.WriteUnicodeString(sound);
        return pWriter;
    }
}
