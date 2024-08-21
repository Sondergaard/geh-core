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

using System.Net;
using ExampleHost.FunctionApp.Tests.Fixtures;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ExampleHost.FunctionApp.Tests.Integration;

/// <summary>
/// Authorization tests using a nested token (a token which contains both an
/// external and an internal token). Focus is on verifying the use of the Authorize
/// attribute with Roles.
///
/// Similar tests exists for Web App in the 'AuthorizationTests' class
/// located in the 'ExampleHost.WebApi.Tests' project.
/// </summary>
[Collection(nameof(ExampleHostsCollectionFixture))]
public class AuthorizationTests : IAsyncLifetime
{
    private const string PermissionOrganizationView = "organizations:view";
    private const string PermissionGridAreasManage = "grid-areas:manage";

    public AuthorizationTests(ExampleHostsFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);

        Fixture.App01HostManager.ClearHostLog();
    }

    private ExampleHostsFixture Fixture { get; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);

        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, PermissionOrganizationView)]
    [InlineData(HttpStatusCode.Forbidden, "")]
    [InlineData(HttpStatusCode.Forbidden, PermissionGridAreasManage)]
    public async Task CallingGetOrganizationReadPermission_WithRole_IsExpectedStatusCode(
        HttpStatusCode expectedStatusCode,
        params string[] roles)
    {
        // Arrange
        var requestIdentification = Guid.NewGuid().ToString();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/authorization/org/{requestIdentification}");
        request.Headers.Authorization = await Fixture.OpenIdJwtManager.JwtProvider.CreateInternalTokenAuthenticationHeaderAsync(roles: roles);
        using var actualResponse = await Fixture.App01HostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, PermissionOrganizationView)]
    [InlineData(HttpStatusCode.OK, PermissionGridAreasManage)]
    [InlineData(HttpStatusCode.OK, PermissionGridAreasManage, PermissionOrganizationView)]
    [InlineData(HttpStatusCode.Forbidden, "")]
    public async Task CallingGetOrganizationOrGridAreasPermission_WithRole_IsExpectedStatusCode(
        HttpStatusCode expectedStatusCode,
        params string[] roles)
    {
        // Arrange
        var requestIdentification = Guid.NewGuid().ToString();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/authorization/org_or_grid/{requestIdentification}");
        request.Headers.Authorization = await Fixture.OpenIdJwtManager.JwtProvider.CreateInternalTokenAuthenticationHeaderAsync(roles: roles);
        using var actualResponse = await Fixture.App01HostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, PermissionGridAreasManage, PermissionOrganizationView)]
    [InlineData(HttpStatusCode.Forbidden, "")]
    [InlineData(HttpStatusCode.Forbidden, PermissionOrganizationView)]
    [InlineData(HttpStatusCode.Forbidden, PermissionGridAreasManage)]
    public async Task CallingGetOrganizationAndGridAreasPermission_WithRole_IsExpectedStatusCode(
        HttpStatusCode expectedStatusCode,
        params string[] roles)
    {
        // Arrange
        var requestIdentification = Guid.NewGuid().ToString();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/authorization/org_and_grid/{requestIdentification}");
        request.Headers.Authorization = await Fixture.OpenIdJwtManager.JwtProvider.CreateInternalTokenAuthenticationHeaderAsync(roles: roles);
        using var actualResponse = await Fixture.App01HostManager.HttpClient.SendAsync(request);

        // Assert
        actualResponse.StatusCode.Should().Be(expectedStatusCode);
    }
}