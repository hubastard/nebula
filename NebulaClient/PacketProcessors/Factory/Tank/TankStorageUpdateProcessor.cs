﻿using NebulaModel.Attributes;
using NebulaModel.Networking;
using NebulaModel.Packets.Factory.Tank;
using NebulaModel.Packets.Processors;

namespace NebulaClient.PacketProcessors.Factory.Tank
{
    [RegisterPacketProcessor]
    class TankStorageUpdateProcessor : IPacketProcessor<TankStorageUpdatePacket>
    {
        public void ProcessPacket(TankStorageUpdatePacket packet, NebulaConnection conn)
        {
            TankComponent[] pool = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.factoryStorage?.tankPool;
            if (pool != null && packet.TankIndex != -1 && packet.TankIndex < pool.Length && pool[packet.TankIndex].id != -1)
            {
                pool[packet.TankIndex].fluidId = packet.FluidId;
                pool[packet.TankIndex].fluidCount = packet.FluidCount;
            }
        }
    }
}