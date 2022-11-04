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

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.WebApp.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Energinet.DataHub.Core.App.WebApp.Authentication;

public static class AuthenticationExtensions
{
    public static void UseUserMiddleware<TUser>(this IApplicationBuilder builder)
        where TUser : class
    {
        builder.UseMiddleware<UserMiddleware<TUser>>();
    }

    public static void AddUserAuthentication<TUser, TUserProvider>(this IServiceCollection services)
        where TUser : class
        where TUserProvider : class, IUserProvider<TUser>
    {
        services.AddScoped<UserContext<TUser>>();
        services.AddScoped<IUserContext<TUser>>(s => s.GetRequiredService<UserContext<TUser>>());
        services.AddScoped<IUserProvider<TUser>, TUserProvider>();
        services.AddScoped<UserMiddleware<TUser>>();
    }

    public static void AddJwtBearerAuthentication(
        this IServiceCollection services,
        string metadataAddress,
        string frontendAppId)
    {
        ArgumentNullException.ThrowIfNull(metadataAddress);
        ArgumentNullException.ThrowIfNull(frontendAppId);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new OpenIdConnectConfigurationRetriever());
                var tokenParams = options.TokenValidationParameters;
                tokenParams.ValidateAudience = true;
                tokenParams.ValidateIssuer = true;
                tokenParams.ValidateIssuerSigningKey = true;
                tokenParams.ValidateLifetime = true;
                tokenParams.RequireSignedTokens = true;
                tokenParams.ClockSkew = TimeSpan.Zero;
                tokenParams.AudienceValidator = (audiences, token, _) =>
                {
                    if (token is not JwtSecurityToken jwtToken)
                    {
                        // Only JWT is supported, deny all other access.
                        return false;
                    }

                    var aud = audiences.ToList();
                    if (aud.Count != 1)
                    {
                        // Multiple audiences, should never happen for our access token.
                        return false;
                    }

                    if (aud.Contains(frontendAppId))
                    {
                        // Access token created for frontend app.
                        return true;
                    }

                    // If audience is not the frontend app id, but an external actor id,
                    // then the token MUST have an 'azp' claim.
                    var authorizedParty = jwtToken
                        .Claims
                        .Single(x => x.Type == JwtRegisteredClaimNames.Azp);

                    return authorizedParty.Value == frontendAppId;
                };
            });
    }
}