﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class PublishWithDebugTest_CoreCLR :
        LoggedTest, IClassFixture<PublishWithDebugTest_CoreCLR.TestFixture>
    {
        public PublishWithDebugTest_CoreCLR(
            TestFixture fixture,
            ITestOutputHelper output)
            : base(output)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PublishingInDebugWorks()
        {
            using (StartLog(out var loggerFactory))
            {
                // Arrange
                var deployment = await Fixture.CreateDeploymentAsync(loggerFactory);

                // Assert
                Assert.True(File.Exists(Path.Combine(deployment.ContentRoot, $"{Fixture.ApplicationName}.PrecompiledViews.dll")));
            }
        }

        public class TestFixture : CoreCLRApplicationTestFixture<SimpleApp.Startup>
        {
            public TestFixture()
            {
                PublishOnly = true;
            }

            protected override DeploymentParameters GetDeploymentParameters()
            {
                var deploymentParameters = base.GetDeploymentParameters();
                deploymentParameters.Configuration = "Debug";

                return deploymentParameters;
            }
        }
    }
}
