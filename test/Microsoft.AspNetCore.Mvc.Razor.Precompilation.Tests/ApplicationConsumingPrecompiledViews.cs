﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public class ApplicationConsumingPrecompiledViews 
        : IClassFixture<ApplicationConsumingPrecompiledViews.ApplicationConsumingPrecompiledViewsFixture>
    {
        public ApplicationConsumingPrecompiledViews(ApplicationConsumingPrecompiledViewsFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Theory]
        [InlineData(RuntimeFlavor.Clr)]
        [InlineData(RuntimeFlavor.CoreClr)]
        public async Task ConsumingClassLibrariesWithPrecompiledViewsWork(RuntimeFlavor flavor)
        {
            // Arrange
            using (var deployer = Fixture.CreateDeployment(flavor))
            {
                var deploymentResult = deployer.Deploy();
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(deploymentResult.ApplicationBaseUri)
                };

                // Act
                var response = await httpClient.GetStringAsync("Manage/Home");

                // Assert
                TestEmbeddedResource.AssertContent("ApplicationConsumingPrecompiledViews.Manage.Home.Index.txt", response);
            }
        }

        public class ApplicationConsumingPrecompiledViewsFixture : ApplicationTestFixture
        {
            private string _packOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            public ApplicationConsumingPrecompiledViewsFixture()
                : base("ApplicationUsingPrecompiledViewClassLibrary")
            {
                ClassLibraryPath = Path.GetFullPath(Path.Combine(ApplicationPath, "..", "ClassLibraryWithPrecompiledViews"));
            }

            private string ClassLibraryPath { get; }

            protected override void Restore()
            {
                CreateClassLibraryPackage();
                RestoreProject(ApplicationPath, new[] { _packOutputDirectory });
            }

            private void CreateClassLibraryPackage()
            {
                RestoreProject(ClassLibraryPath);
                ExecuteForClassLibrary(Command.CreateDotNet("build", new[] { ClassLibraryPath, "-c", "Release" }));
                ExecuteForClassLibrary(Command.CreateDotNet(
                    "razor-precompile", 
                    GetPrecompileArguments("net451")));

                ExecuteForClassLibrary(Command.CreateDotNet(
                    "razor-precompile",
                    GetPrecompileArguments("netcoreapp1.0")));

                var timestamp = "z" + DateTime.UtcNow.Ticks.ToString().PadLeft(18, '0');
                var packCommand = Command
                    .CreateDotNet("pack", new[] { "--no-build", "-c", "Release", "-o", _packOutputDirectory })
                    .EnvironmentVariable("DOTNET_BUILD_VERSION", timestamp);

                ExecuteForClassLibrary(packCommand);
            }

            private void ExecuteForClassLibrary(ICommand command)
            {
                Console.WriteLine($"Running {command.CommandName} {command.CommandArgs}");
                command
                    .WorkingDirectory(ClassLibraryPath)
                    .EnvironmentVariable(NuGetPackagesEnvironmentKey, TempRestoreDirectory)
                    .EnvironmentVariable(DotnetSkipFirstTimeExperience, "true")
                    .ForwardStdErr(Console.Error)
                    .ForwardStdOut(Console.Out)
                    .Execute()
                    .EnsureSuccessful();
            }

            private string[] GetPrecompileArguments(string targrtFramework)
            {
                return new[]
                {
                    ClassLibraryPath,
                    "-c",
                    "Release",
                    "-f",
                    $"{targrtFramework}",
                    "-o",
                    $"obj/precompiled/{targrtFramework}",
                };
            }

            public override void Dispose()
            {
                TryDeleteDirectory(_packOutputDirectory);
                base.Dispose();
            }
        }
    }
}
