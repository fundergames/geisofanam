namespace Geis.Puzzles
{
    /// <summary>
    /// Receives overlap counts from <see cref="SoulSwitchProximityRelay"/> for interact / prompt zones.
    /// </summary>
    public interface IPuzzleProximityRelayOwner
    {
        void OnProximityRelayEnter(bool interact, bool prompt);
        void OnProximityRelayExit(bool interact, bool prompt);
    }
}
