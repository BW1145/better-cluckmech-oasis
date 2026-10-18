namespace CluckmechCoop
{
    // Input timestamps use the Input System clock; gameplay time never measures a held button.
    internal sealed class OtherworldActorInputState
    {
        private const double HoldThresholdSeconds = .25;

        internal bool Armed { get; private set; }
        internal bool Qualified { get; private set; }
        private double startedAt;

        internal bool Start(double now, bool allowed, bool active)
        {
            if (Armed || !allowed || !active || double.IsNaN(now) || double.IsInfinity(now))
                return false;

            Armed = true;
            Qualified = false;
            startedAt = now;
            return true;
        }

        internal bool Qualify(double now, bool allowed)
        {
            if (!Armed || Qualified || !allowed || double.IsNaN(now) || double.IsInfinity(now) ||
                now - startedAt < HoldThresholdSeconds)
                return false;

            Qualified = true;
            return true;
        }

        internal bool Reset()
        {
            bool armed = Armed;
            Armed = Qualified = false;
            startedAt = 0;
            return armed;
        }
    }
}
