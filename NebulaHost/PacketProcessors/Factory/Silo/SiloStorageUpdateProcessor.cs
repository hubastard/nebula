﻿using NebulaModel.Attributes;
using NebulaModel.Networking;
using NebulaModel.Packets.Factory.Silo;
using NebulaModel.Packets.Processors;

namespace NebulaHost.PacketProcessors.Factory.Ejector
{
    [RegisterPacketProcessor]
    class SiloStorageUpdateProcessor : IPacketProcessor<SiloStorageUpdatePacket>
    {
        public void ProcessPacket(SiloStorageUpdatePacket packet, NebulaConnection conn)
        {
            SiloComponent[] pool = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.factorySystem?.siloPool;
            if (pool != null && packet.SiloIndex != -1 && packet.SiloIndex < pool.Length && pool[packet.SiloIndex].id != -1)
            {
                pool[packet.SiloIndex].bulletCount = packet.NewRocketsAmount;
            }
        }
    }
}