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

        public CoPilotService(SessionConfig sessionConfig)
        {
            this._sessionConfig = sessionConfig;
        }

        public async Task<CopilotSession> GetCopilotSessionAsync()
        {
            var client = new CopilotClient();
            var session = await client.CreateSessionAsync(_sessionConfig);
            return session;
        }
    }
}
