namespace CluckmechCoop
{
    // A lease belongs to one uninterrupted registration/space of a unit.
    // Returning to the same space never revives an older action.
    internal sealed class UnitActionLifetime
    {
        private ulong revision;
        internal ulong Capture() => revision;
        internal bool Current(ulong lease) => lease == revision;
        internal void Invalidate() { unchecked { revision++; } }
    }
}
