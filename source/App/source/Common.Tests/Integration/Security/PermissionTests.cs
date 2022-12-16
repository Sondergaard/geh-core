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
using System.Linq;
using Energinet.DataHub.Core.App.Common.Security;
using Xunit;

namespace Energinet.DataHub.Core.App.Common.Tests.Integration.Security;

public sealed class PermissionTests
{
    [Fact]
    public void Value_NoAlias_Allowed()
    {
        // Arrange
        var values = Enum.GetValues<Permission>().Distinct();
        var names = Enum.GetNames<Permission>();

        // Act + Assert
        Assert.Equal(values.Count(), names.Length);
    }
}