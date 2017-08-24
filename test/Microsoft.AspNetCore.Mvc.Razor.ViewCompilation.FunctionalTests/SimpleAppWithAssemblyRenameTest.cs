// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppWithAssemblyRenameTest : IClassFixture<SimpleAppWithAssemblyRenameTest.TestFixture>
    {
        public SimpleAppWithAssemblyRenameTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForSimpleAppsWithRename(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();

                // Act
                var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                    deploymentResult.ApplicationBaseUri,
                    Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("SimpleAppWithAssemblyRenameTest.Home.Index.txt", response);
            }
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleApp")
            {
                ExecutableName = "DifferentAssembly";
            }

            public override DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor)
            {
                var deploymentParameters = base.GetDeploymentParameters(flavor);
                deploymentParameters.PublishEnvironmentVariables.Add(new KeyValuePair<string, string>("RenameAssemblyName", "true"));

                return deploymentParameters;
            }
        }
    }
}
