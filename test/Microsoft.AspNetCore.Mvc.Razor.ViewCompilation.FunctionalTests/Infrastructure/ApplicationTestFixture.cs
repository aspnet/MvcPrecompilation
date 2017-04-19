// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public abstract class ApplicationTestFixture : IDisposable
    {
        public const string NuGetPackagesEnvironmentKey = "NUGET_PACKAGES";
        public const string DotnetSkipFirstTimeExperience = "DOTNET_SKIP_FIRST_TIME_EXPERIENCE";
        public const string DotnetCLITelemetryOptOut = "DOTNET_CLI_TELEMETRY_OPTOUT";

        private readonly string _oldRestoreDirectory;
        private bool _isRestored;

        private string _workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        protected ApplicationTestFixture(string applicationName)
        {
            ApplicationName = applicationName;
            _oldRestoreDirectory = Environment.GetEnvironmentVariable(NuGetPackagesEnvironmentKey);
            TempRestoreDirectory = CreateTempRestoreDirectory();
        }

        public string ApplicationName { get; }

        public string ApplicationPath => ApplicationPaths.GetTestAppDirectory(ApplicationName);

        public string TempRestoreDirectory { get; }

        public HttpClient HttpClient { get; } = new HttpClient();

        public ILogger Logger { get; private set; }

        public ApplicationDeployer CreateDeployment(RuntimeFlavor flavor)
        {
            PrepareForDeployment(flavor);
            var deploymentParameters = GetDeploymentParameters(flavor);
            return new ApplicationDeployer(deploymentParameters, Logger, ApplicationPath, ApplicationName);
        }

        public virtual void PrepareForDeployment(RuntimeFlavor flavor)
        {
            Logger = CreateLogger(flavor);

            if (!_isRestored)
            {
                Restore();
                _isRestored = true;
            }
        }

        public virtual DeploymentParameters GetDeploymentParameters(RuntimeFlavor flavor)
        {
            var tempRestoreDirectoryEnvironment = new KeyValuePair<string, string>(
                NuGetPackagesEnvironmentKey,
                TempRestoreDirectory);

            var skipFirstTimeCacheCreation = new KeyValuePair<string, string>(
                DotnetSkipFirstTimeExperience,
                "true");

            var telemetryOptOut = new KeyValuePair<string, string>(
                DotnetCLITelemetryOptOut,
                "1");

            var publishPath = Path.Combine(_workingDirectory, Path.GetRandomFileName());
            var deploymentParameters = new DeploymentParameters(
                ApplicationPath,
                ServerType.Kestrel,
                flavor,
                RuntimeArchitecture.x64)
            {
                PublishedApplicationRootPath = Path.Combine(_workingDirectory, Path.GetRandomFileName()),
                PublishApplicationBeforeDeployment = false,
                TargetFramework = flavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.1",
                Configuration = "Release",
                EnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment,
                    skipFirstTimeCacheCreation,
                    telemetryOptOut,
                },
                PublishEnvironmentVariables =
                {
                    tempRestoreDirectoryEnvironment,
                    skipFirstTimeCacheCreation,
                    telemetryOptOut,
                },
            };

            return deploymentParameters;
        }

        protected virtual ILogger CreateLogger(RuntimeFlavor flavor)
        {
            return new LoggerFactory()
                .AddConsole()
                .CreateLogger($"{ApplicationName}:{flavor}");
        }

        protected virtual void Restore()
        {
            RestoreProject(ApplicationPath);
        }

        public virtual void Dispose()
        {
            TryDeleteDirectory(_workingDirectory);
            HttpClient.Dispose();
        }

        protected static void TryDeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                // Ignore delete failures.
            }
        }

        protected void RestoreProject(string applicationDirectory, string[] additionalFeeds = null)
        {            
            var args = new List<string>
            {
                "--packages",
                TempRestoreDirectory,
            };

            if (additionalFeeds != null)
            {
                foreach (var feed in additionalFeeds)
                {
                    args.Add("-s");
                    args.Add(feed);
                }
            }

            Command
                .CreateDotNet("restore", args)
                .EnvironmentVariable(DotnetSkipFirstTimeExperience, "true")
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(Console.Out)
                .WorkingDirectory(applicationDirectory)
                .Execute()
                .EnsureSuccessful();
        }

        private string CreateTempRestoreDirectory()
        {
            var path = Path.Combine(_workingDirectory, Path.GetRandomFileName());
            return Directory.CreateDirectory(path).FullName;
        }

        private static string GetNuGetPackagesDirectory()
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
