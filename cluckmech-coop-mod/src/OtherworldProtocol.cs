using Mirror;

namespace CluckmechCoop
{
    internal struct OtherworldMessage : NetworkMessage
    {
        internal uint Player, Sequence;
        internal int Kind, Value;
        internal float X, Y, EndX, EndY, Duration, Amount;
        internal string Token, Data;
    }

    // Published wire numbers: do not renumber or reuse retired values.
    internal static class OtherworldMessageKind
    {
        internal const int Cast = 0;
        internal const int BeginPhase = 1;
        internal const int ReleaseInput = 2;
        internal const int ReservedCommand = 3;
        internal const int Dash = 4;
        internal const int State = 10;
        internal const int Effect = 11;
        internal const int Beam = 12;
        internal const int Transfer = 14;
        internal const int AddEnergy = 15;
        internal const int EnsureRoom = 16;
        internal const int Progress = 20;
        internal const int Drop = 21;
        internal const int DamageStatistics = 22;
        internal const int AddGold = 23;
        internal const int SubmitProgress = 30;
        internal const int CollectDrop = 31;

        internal static bool IsCommand(int kind) => kind >= Cast && kind <= Dash;
    }

    internal static class OtherworldProtocol
    {
        // v0.6.4 layout, intentionally unversioned on wire. Peer capability negotiation
        // remains in the existing handshake (dash has its own Value protocol marker).
        // Compatible releases retain this order/types/numbers; incompatible changes
        // require a negotiated capability or a new message type, never extra fields.
        internal static void Register()
        {
            Writer<OtherworldMessage>.write = Write;
            Reader<OtherworldMessage>.read = Read;
        }

        private static void Write(NetworkWriter writer, OtherworldMessage message)
        {
            writer.WriteUInt(message.Player);
            writer.WriteUInt(message.Sequence);
            writer.WriteInt(message.Kind);
            writer.WriteInt(message.Value);
            writer.WriteFloat(message.X);
            writer.WriteFloat(message.Y);
            writer.WriteFloat(message.EndX);
            writer.WriteFloat(message.EndY);
            writer.WriteFloat(message.Duration);
            writer.WriteFloat(message.Amount);
            writer.WriteString(message.Token);
            writer.WriteString(message.Data);
        }

        private static OtherworldMessage Read(NetworkReader reader)
        {
            return new OtherworldMessage
            {
                Player = reader.ReadUInt(),
                Sequence = reader.ReadUInt(),
                Kind = reader.ReadInt(),
                Value = reader.ReadInt(),
                X = reader.ReadFloat(),
                Y = reader.ReadFloat(),
                EndX = reader.ReadFloat(),
                EndY = reader.ReadFloat(),
                Duration = reader.ReadFloat(),
                Amount = reader.ReadFloat(),
                Token = reader.ReadString(),
                Data = reader.ReadString()
            };
        }
    }
}
