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

using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.Core.Messaging.Communication;

public static class Registration
{
    /// <summary>
    /// Method for registering the communication library.
    /// It is the responsibility of the caller to register the dependencies of the
    /// <see cref="IIntegrationEventProvider"/> implementation.
    /// </summary>
    /// <typeparam name="TIntegrationEventProvider">The type of the service to use for outbound events.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="settingsFactory"></param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddCommunication<TIntegrationEventProvider>(
        this IServiceCollection services,
        Func<IServiceProvider, CommunicationSettings> settingsFactory)
        where TIntegrationEventProvider : class, IIntegrationEventProvider
    {
        services.AddScoped<IIntegrationEventProvider, TIntegrationEventProvider>();
        services.AddSingleton<IServiceBusSenderProvider, ServiceBusSenderProvider>(sp =>
        {
            var settings = settingsFactory(sp);
            return new ServiceBusSenderProvider(settings);
        });

        services.AddScoped<IOutboxSender, OutboxSender>();
        services.AddScoped<IServiceBusMessageFactory, ServiceBusMessageFactory>();

        RegisterHostedServices(services);

        return services;
    }

    private static void RegisterHostedServices(IServiceCollection services)
    {
        services.AddHostedService<OutboxSenderTrigger>();

        services
            .AddHealthChecks()
            .AddRepeatingTriggerHealthCheck<OutboxSenderTrigger>(TimeSpan.FromMinutes(1));
    }
}