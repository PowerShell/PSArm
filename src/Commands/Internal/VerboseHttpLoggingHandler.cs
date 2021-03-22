
// Copyright (c) Microsoft Corporation.

using System.Management.Automation.Host;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PSArm.Commands.Internal
{
    internal class VerboseHttpLoggingHandler : DelegatingHandler
    {
        PSHostUserInterface _psUI;

        public VerboseHttpLoggingHandler(PSHostUserInterface psUI, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _psUI = psUI;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _psUI.WriteVerboseLine($"Sending {request.Method} request to '{request.RequestUri}'");
            _psUI.WriteVerboseLine("Body:");
            _psUI.WriteVerboseLine(await request.Content.ReadAsStringAsync().ConfigureAwait(false));

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _psUI.WriteVerboseLine($"Got response: {response}");
            if (response.Content is not null)
            {
                _psUI.WriteVerboseLine("Body:");
                _psUI.WriteVerboseLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }

            return response;
        }
    }
}
