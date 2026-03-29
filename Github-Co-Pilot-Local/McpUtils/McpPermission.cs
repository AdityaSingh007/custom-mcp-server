using GitHub.Copilot.SDK;

namespace Github_Co_Pilot_Local.McpUtils
{
    public static class McpPermission
    {
        public static CancellationTokenSource spinnerCancellationToken { get; } = new CancellationTokenSource();
        public static Task<PermissionRequestResult> PromptPermission(PermissionRequest request, PermissionInvocation invocation)
        {
            spinnerCancellationToken.Cancel();
            Console.WriteLine($"\n[Permission Request: {request.Kind}]");
            Console.Write("Approve? (y/n): ");

            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
            PermissionRequestResultKind kind = input is "Y" or "YES" ? PermissionRequestResultKind.Approved : PermissionRequestResultKind.DeniedInteractivelyByUser;

            return Task.FromResult(new PermissionRequestResult { Kind = kind });
        }

        // Spinner helper: starts a background task that shows an animated spinner after the prefix.
        public static (CancellationTokenSource, Task) StartSpinner(string prefix = "Assistant: ")
        {
            Console.Write(prefix);
            var task = Task.Run(async () =>
            {
                var sequence = new[] { '|', '/', '-', '\\' };
                var i = 0;
                try
                {
                    while (!spinnerCancellationToken.IsCancellationRequested)
                    {
                        Console.Write(sequence[i++ % sequence.Length]);
                        await Task.Delay(100, spinnerCancellationToken.Token).ConfigureAwait(false);
                        Console.Write('\b');
                    }
                }
                catch (OperationCanceledException) { }
            });

            return (spinnerCancellationToken, task);
        }
    }
}
