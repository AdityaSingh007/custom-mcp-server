using GitHub.Copilot.SDK;
using Github_Co_Pilot_Local.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Github_Co_Pilot_Local.CoPilot_Client
{
    public class CoPilotService : ICoPilotService
    {
        private readonly SessionConfig _sessionConfig;
        private readonly CopilotClient _copilotClient;

        public CoPilotService(SessionConfig sessionConfig , CopilotClient copilotClient)
        {
            this._sessionConfig = sessionConfig;
            this._copilotClient = copilotClient;
        }

        public async Task<CopilotSession> GetCopilotSessionAsync()
        {
            var session = await _copilotClient.CreateSessionAsync(_sessionConfig);
            return session;
        }
    }
}
