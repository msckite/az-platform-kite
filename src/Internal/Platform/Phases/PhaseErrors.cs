using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace MSCKite.Azure.Platform.Internal.Platform.Phases
{
    // Writes a phase's non-terminating errors and counts them per cmdlet, so Set-Platform can stop before phases that depend on a failed one
    internal static class PhaseErrors
    {
        private static readonly ConditionalWeakTable<Cmdlet, StrongBox<int>> Counts = new ConditionalWeakTable<Cmdlet, StrongBox<int>>();

        internal static void WritePhaseError(this Cmdlet cmdlet, ErrorRecord errorRecord)
        {
            Counts.GetOrCreateValue(cmdlet).Value++;
            cmdlet.WriteError(errorRecord);
        }

        internal static int Count(Cmdlet cmdlet)
        {
            return Counts.TryGetValue(cmdlet, out var count) ? count.Value : 0;
        }
    }
}
