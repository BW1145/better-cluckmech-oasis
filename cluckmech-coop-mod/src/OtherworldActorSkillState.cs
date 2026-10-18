using System;

namespace CluckmechCoop
{
    // Authoritative skill rules have no Unity, input or presentation dependencies.
    internal sealed class OtherworldActorSkillState
    {
        internal int Charges { get; private set; }
        internal bool Held { get; private set; }
        private bool dashStarted;
        private float rechargeAt;

        internal void Initialize(int maximum)
        {
            Charges = maximum;
            rechargeAt = 0;
            Release();
        }

        internal bool Spend(float now, float cooldown, bool hold)
        {
            if (Charges <= 0)
                return false;

            Charges--;
            if (rechargeAt <= now)
                rechargeAt = now + cooldown;

            if (hold)
            {
                Held = true;
                dashStarted = false;
            }
            return true;
        }

        internal bool Dash()
        {
            if (!Held || dashStarted)
                return false;

            dashStarted = true;
            Held = false;
            return true;
        }

        internal bool Release()
        {
            bool held = Held;
            Held = false;
            dashStarted = false;
            return held;
        }

        internal bool Recharge(float now, float cooldown, int maximum)
        {
            if (Charges >= maximum || now < rechargeAt)
                return false;

            Charges++;
            rechargeAt = Charges < maximum ? now + cooldown : 0;
            return true;
        }

        internal bool Reduce(float now, float seconds)
        {
            if (rechargeAt <= now || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0)
                return false;

            rechargeAt = Math.Max(now, rechargeAt - seconds);
            return true;
        }

        internal bool Receive(int charges, float remaining, int maximum, float now)
        {
            if (charges < 0 || charges > maximum || remaining < 0 || float.IsNaN(remaining) ||
                float.IsInfinity(remaining) || (double)now + remaining > float.MaxValue)
                return false;

            Charges = charges;
            rechargeAt = now + remaining;
            return true;
        }

        internal float RechargeRemaining(float now) => Math.Max(0, rechargeAt - now);
        internal float Remaining(float now) => Charges > 0 ? 0 : RechargeRemaining(now);
    }
}
