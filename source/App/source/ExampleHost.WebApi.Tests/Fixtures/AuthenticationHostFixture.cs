﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration.B2C;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace ExampleHost.WebApi.Tests.Fixtures
{
    public sealed class AuthenticationHostFixture : IAsyncLifetime
    {
        /// <summary>
        /// We can use any of the allowed environments.
        /// </summary>
        private const string B2CEnvironment = "u001";

        /// <summary>
        /// We don't require a certain client app, we can use any for which we can
        /// get a valid access token.
        /// </summary>
        private const string SystemOperator = "endk-tso";

        public AuthenticationHostFixture()
        {
            var web04BaseUrl = "http://localhost:5003";

            IntegrationTestConfiguration = new IntegrationTestConfiguration();

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", IntegrationTestConfiguration.ApplicationInsightsInstrumentationKey);

            AuthorizationConfiguration = new B2CAuthorizationConfiguration(
                usedForSystemTests: false,
                environment: B2CEnvironment,
                new List<string> { SystemOperator });

            BackendAppAuthenticationClient = new B2CAppAuthenticationClient(
                AuthorizationConfiguration.TenantId,
                AuthorizationConfiguration.BackendApp,
                AuthorizationConfiguration.ClientApps[SystemOperator]);

            var metadataArg = $"--metadata={Metadata}";
            var frontendAppIdArg = $"--appid={FrontendClientId}";

            // We cannot use TestServer as this would not work with Application Insights.
            Web04Host = WebHost.CreateDefaultBuilder(new[] { metadataArg, frontendAppIdArg })
                .UseStartup<WebApi04.Startup>()
                .UseUrls(web04BaseUrl)
                .Build();

            Web04HttpClient = new HttpClient
            {
                BaseAddress = new Uri(web04BaseUrl),
            };
        }

        public string Metadata => AuthorizationConfiguration.BackendOpenIdConfigurationUrl;

        public string FrontendClientId => BackendAppAuthenticationClient.ClientAppSettings.CredentialSettings.ClientId;

        public HttpClient Web04HttpClient { get; }

        public B2CAppAuthenticationClient BackendAppAuthenticationClient { get; }

        public B2CAuthorizationConfiguration AuthorizationConfiguration { get; }

        private IWebHost Web04Host { get; }

        private IntegrationTestConfiguration IntegrationTestConfiguration { get; }

        public async Task InitializeAsync()
        {
            await Web04Host.StartAsync();
        }

        public async Task DisposeAsync()
        {
            Web04HttpClient.Dispose();
            await Web04Host.StopAsync();
        }
    }
}