using GitHub.Copilot.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace Github_Co_Pilot_Local.Contracts
{
    public interface ICoPilotService
    {
        Task<CopilotSession> GetCopilotSessionAsync();
    }
}
