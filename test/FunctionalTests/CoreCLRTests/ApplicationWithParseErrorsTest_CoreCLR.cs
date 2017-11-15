﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace FunctionalTests
{
    public class ApplicationWithParseErrorsTest_CoreCLR
        : IClassFixture<CoreCLRApplicationTestFixture<ApplicationWithParseErrors.Startup>>
    {
        public ApplicationWithParseErrorsTest_CoreCLR(CoreCLRApplicationTestFixture<ApplicationWithParseErrors.Startup> fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task PublishingPrintsParseErrors()
        {
            // Arrange
            var applicationPath = ApplicationPaths.GetTestAppDirectory("ApplicationWithParseErrors");
            var indexPath = Path.Combine(applicationPath, "Views", "Home", "Index.cshtml");
            var viewImportsPath = Path.Combine(applicationPath, "Views", "Home", "About.cshtml");
            var expectedErrors = new[]
            {
                indexPath + " (0): The code block is missing a closing \"}\" character.  Make sure you have a matching \"}\" character for all the \"{\" characters within this block, and that none of the \"}\" characters are being interpreted as markup.",
                viewImportsPath + " (1): A space or line break was encountered after the \"@\" character.  Only valid identifiers, keywords, comments, \"(\" and \"{\" are valid at the start of a code block and they must occur immediately following \"@\" with no space in between.",

            };
            var testSink = new TestSink();
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);

            // Act
            await Assert.ThrowsAsync<Exception>(() => Fixture.CreateDeploymentAsync(loggerFactory));

            // Assert
            var logs = testSink.Writes.Select(w => w.State.ToString().Trim()).ToList();
            foreach (var expectedError in expectedErrors)
            {
                Assert.Contains(logs, log => log.Contains(expectedError));
            }
        }
    }
}
