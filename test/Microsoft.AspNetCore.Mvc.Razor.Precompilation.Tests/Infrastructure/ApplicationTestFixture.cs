﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        public const string NuGetPackagesEnvironmentKey = "NUGET_PACKAGES";
        public const string DotnetSkipFirstTimeExperience = "DOTNET_SKIP_FIRST_TIME_EXPERIENCE";
        private readonly string _oldRestoreDirectory;
        private bool _isRestored;

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
            _oldRestoreDirectory = Environment.GetEnvironmentVariable(NuGetPackagesEnvironmentKey);
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public string TempRestoreDirectory { get; } = CreateTempRestoreDirectory();

        public IApplicationDeployer CreateDeployment(RuntimeFlavor flavor)
        {
            if (!_isRestored)
            {
                Restore();
                _isRestored = true;
            }

            var tempRestoreDirectoryEnvironment = new KeyValuePair<string, string>(
                NuGetPackagesEnvironmentKey,
                TempRestoreDirectory);

            var skipFirstTimeCacheCreation = new KeyValuePair<string, string>(
                DotnetSkipFirstTimeExperience,
                "true");


            var deploymentParameters = new DeploymentParameters(
                ApplicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
                PublishApplicationBeforeDeployment = true,
                TargetFramework = flavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                PreservePublishedApplicationForDebugging = true,
                Configuration = "Release",
                EnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment,
                    skipFirstTimeCacheCreation,
                },
                PublishEnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment,
                    skipFirstTimeCacheCreation,
                },
            };

            var logger = new LoggerFactory()
                .AddConsole()
                .CreateLogger($"{ApplicationName}:{flavor}");

            return ApplicationDeployerFactory.Create(deploymentParameters, logger);
        }

        protected virtual void Restore()
        {
            RestoreProject(ApplicationPath);
        }

        public virtual void Dispose()
        {
            TryDeleteDirectory(TempRestoreDirectory);
        }

        protected static void TryDeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Ignore delete failures.
            }
        }

        protected void RestoreProject(string applicationDirectory, string[] additionalFeeds = null)
        {
            var packagesDirectory = GetNuGetPackagesDirectory();
            var args = new List<string>
            {
                Path.Combine(applicationDirectory, "project.json"),
                "--packages",
                TempRestoreDirectory,
            };

            if (additionalFeeds != null)
            {
                foreach (var feed in additionalFeeds)
                {
                    args.Add("-f");
                    args.Add(feed);
                }
            }

            Command
                .CreateDotNet("restore", args)
                .EnvironmentVariable(DotnetSkipFirstTimeExperience, "true")
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(Console.Out)
                .Execute()
                .EnsureSuccessful();
        }

        private static string CreateTempRestoreDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return Directory.CreateDirectory(path).FullName;
        }

        protected static string GetNuGetPackagesDirectory()
        {
            var nugetFeed = Environment.GetEnvironmentVariable(NuGetPackagesEnvironmentKey);
            if (!string.IsNullOrEmpty(nugetFeed))
            {
                return nugetFeed;
            }

            string basePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            else
            {
                basePath = Environment.GetEnvironmentVariable("HOME");
            }

            return Path.Combine(basePath, ".nuget", "packages");
        }
    }
}
