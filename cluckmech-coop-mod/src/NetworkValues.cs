namespace CluckmechCoop
{
    internal static class NetworkValues
    {
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
