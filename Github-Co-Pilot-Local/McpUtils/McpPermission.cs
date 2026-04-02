using GitHub.Copilot.SDK;

namespace Github_Co_Pilot_Local.McpUtils
{
    public static class McpPermission
    {
        public static Task<PermissionRequestResult> PromptPermission(PermissionRequest request, PermissionInvocation invocation)
        {
            Console.WriteLine($"\n[Permission Request: {request.Kind}]");
            Console.Write("Approve? (y/n): ");

            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
            PermissionRequestResultKind kind = input is "Y" or "YES" ? PermissionRequestResultKind.Approved : PermissionRequestResultKind.DeniedInteractivelyByUser;

            return Task.FromResult(new PermissionRequestResult { Kind = kind });
        }
    }
}
