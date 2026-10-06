using System.Threading;

namespace mosair.Services
{
    // The cancel button's token for the work running on this logical thread (Mos, stock fit, export). It flows
    // into Task.Run and Parallel.For, so the engine's long loops can stop at their checkpoints without every
    // method taking a token parameter. Outside cancellable work it is CancellationToken.None and Check() does
    // nothing.
    public static class WorkCancellation
    {
        private static readonly AsyncLocal<CancellationToken> CurrentToken = new();

        public static CancellationToken Token
        {
            get => CurrentToken.Value;
            set => CurrentToken.Value = value;
        }

        // Throws OperationCanceledException when the user pressed cancel.
        public static void Check() => CurrentToken.Value.ThrowIfCancellationRequested();
    }
}
