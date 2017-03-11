﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.FunctionalTests
{
    public class PublishWithEmbedViewSourcesTest
        : IClassFixture<PublishWithEmbedViewSourcesTest.PublishWithEmbedViewSourcesTestFixture>
    {
        private const string ApplicationName = "PublishWithEmbedViewSources";

        public PublishWithEmbedViewSourcesTest(PublishWithEmbedViewSourcesTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task Precompilation_CanEmbedViewSourcesAsResources()
        {
            // Arrange
            var expectedViews = new[]
            {
                "/Areas/TestArea/Views/Home/Index.cshtml",
                "/Views/Home/About.cshtml",
                "/Views/Home/Index.cshtml",
            };
            var expectedText = "Hello Index!";
            var deploymentResult = Fixture.CreateDeployment();

            var assemblyPath = Path.Combine(
                deploymentResult.DeploymentParameters.PublishedApplicationRootPath,
                $"{ApplicationName}.PrecompiledViews.dll");

            // Act - 1
            var response1 = await Fixture.HttpClient.GetStringWithRetryAsync(
                $"{deploymentResult.ApplicationBaseUri}Home/Index",
                Fixture.Logger);

            // Assert - 1
            Assert.Equal(expectedText, response1.Trim());

            // Act - 2
            var response2 = await Fixture.HttpClient.GetStringWithRetryAsync(
                $"{deploymentResult.ApplicationBaseUri}Home/GetPrecompiledResourceNames",
                Fixture.Logger);

            // Assert - 2
            var actual = response2.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedViews, actual);
        }

        public class PublishWithEmbedViewSourcesTestFixture : ApplicationTestFixture
        {
            public PublishWithEmbedViewSourcesTestFixture()
                : base(PublishWithEmbedViewSourcesTest.ApplicationName)
            {
            }
        }
    }
}
