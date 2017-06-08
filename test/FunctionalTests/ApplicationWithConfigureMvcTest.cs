﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace FunctionalTests
{
    public class ApplicationWithConfigureMvcTest
        : IClassFixture<ApplicationWithConfigureMvcTest.ApplicationWithConfigureMvcFixture>
    {
        public ApplicationWithConfigureMvcTest(ApplicationWithConfigureMvcFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_RunsConfiguredCompilationCallbacks(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {

                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                deployment.DeploymentResult.ApplicationBaseUri,
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent("ApplicationWithConfigureMvc.Home.Index.txt", response);
            }
        }

        [ConditionalTheory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_UsesConfiguredParseOptions(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployment = await Fixture.CreateDeploymentAsync(flavor))
            {
                // Act
                var response = await deployment.HttpClient.GetStringWithRetryAsync(
                "Home/ViewWithPreprocessor",
                Fixture.Logger);

                // Assert
                TestEmbeddedResource.AssertContent(
                    "ApplicationWithConfigureMvc.Home.ViewWithPreprocessor.txt",
                    response);
            }
        }

        public class ApplicationWithConfigureMvcFixture : ApplicationTestFixture
        {
            public ApplicationWithConfigureMvcFixture()
                : base("ApplicationWithConfigureMvc")
            {
            }
        }
    }
}
