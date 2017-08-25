﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ViewCompilationOptionsTest : IClassFixture<ViewCompilationOptionsTest.TestFixture>
    {
        public ViewCompilationOptionsTest(TestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public void Precompilation_PreventsRefAssembliesFromBeingPublished(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                // Act
                var deploymentResult = deployer.Deploy();

                // Assert
                Assert.False(Directory.Exists(Path.Combine(deploymentResult.ContentRoot, "refs")));
            }
        }

        [Theory(Skip = "https://github.com/aspnet/MvcPrecompilation/issues/195")]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public void PublishingWithOption_AllowsPublishingRefAssemblies(RuntimeFlavor flavor)
        {
            // Arrange
            Fixture.PrepareForDeployment(flavor);
            var deploymentParameters = Fixture.GetDeploymentParameters(flavor);
            deploymentParameters.PublishEnvironmentVariables.Add(
                new KeyValuePair<string, string>("MvcRazorExcludeRefAssembliesFromPublish", "false"));

            using (var deployer = new ApplicationDeployer(deploymentParameters, Fixture.Logger, Fixture.ApplicationPath, Fixture.ApplicationName))
            {
                // Act
                var deploymentResult = deployer.Deploy();

                // Assert
                var refsDirectory = Path.Combine(deploymentResult.ContentRoot, "refs");
                Assert.True(Directory.Exists(refsDirectory), $"{refsDirectory} does not exist. {deploymentParameters.PublishedApplicationRootPath}");
            }
        }

        public class TestFixture : ApplicationTestFixture
        {
            public TestFixture()
                : base("SimpleApp")
            {
            }
        }
    }
}
