namespace CluckmechCoop
{
    internal sealed class SceneLifetime
    {
        internal readonly int Handle;
        internal bool Active { get; private set; } = true;
        internal bool CombatStarted { get; private set; }

        internal SceneLifetime(int handle) { Handle = handle; }
        internal bool BeginCombat()
        {
            if (!Active || CombatStarted) return false;
            CombatStarted = true;
            return true;
        }
        internal bool Close()
        {
            if (!Active) return false;
            Active = false;
            return true;
        }
    }

    internal sealed class NetworkSessionState
    {
        private readonly UnitActionLifetime callbacks = new UnitActionLifetime();
        internal bool Active { get; private set; }
        internal bool Peer { get; private set; }
        internal int Revision { get; private set; }
        internal int Acknowledged { get; private set; } = -1;
        internal bool Ready => Peer && Acknowledged == Revision;

        internal void Open() { Active = true; }
        internal ulong Capture() => callbacks.Capture();
        internal bool Current(ulong lease) => Active && callbacks.Current(lease);
        internal void CancelPending() => callbacks.Invalidate();
        internal void Published() { Revision++; }
        internal void PeerSeen() { Open(); Peer = true; }
        internal void Acknowledge() { Open(); Peer = true; Acknowledged = Revision; }
        internal void AcceptSettings(int revision)
        {
            Revision = revision;
            Acknowledge();
        }
        internal void Mismatch() { Peer = false; Acknowledged = -1; }
        internal void Close()
        {
            CancelPending();
            Active = Peer = false;
            Revision = 0;
            Acknowledged = -1;
        }
    }
}
