// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    /// <summary>
    /// Deployer for WebListener and Kestrel.
    /// </summary>
    public class ApplicationDeployer : IDisposable
    {
        public Process HostProcess { get; private set; }

        public DeploymentParameters DeploymentParameters { get; }

        public string ApplicationName { get; }

        public string ApplicationPath { get; }

        public ILogger Logger { get; }

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILogger logger, string applicationPath, string applicationName)
        {
            DeploymentParameters = deploymentParameters;
            Logger = logger;
            ApplicationPath = applicationPath;
            ApplicationName = applicationName;
        }

        public DeploymentResult Deploy()
        {
            DotnetPublish(ApplicationPath, ApplicationName);

            var uri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);
            // Launch the host process.
            var hostExitToken = StartSelfHost(uri);

            return new DeploymentResult
            {
                ContentRoot = DeploymentParameters.PublishedApplicationRootPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = hostExitToken
            };
        }

        protected CancellationToken StartSelfHost(Uri uri)
        {
            string executableName;
            string executableArgs = string.Empty;
            string workingDirectory = string.Empty;
            workingDirectory = DeploymentParameters.PublishedApplicationRootPath;
            var executableExtension =
                DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? ".exe" :
                DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll" : "";
            var executable = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, new DirectoryInfo(DeploymentParameters.ApplicationPath).Name + executableExtension);
            
            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
            {
                executableName = "dotnet";
                executableArgs = executable;
            }
            else
            {
                executableName = executable;
            }

            executableArgs += $" --server.urls {uri} --server \"Microsoft.AspNetCore.Server.Kestrel\"";

            Logger.LogInformation($"Executing {executableName} {executableArgs}");

            var startInfo = new ProcessStartInfo
            {
                FileName = executableName,
                Arguments = executableArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                // Trying a work around for https://github.com/aspnet/Hosting/issues/140.
                RedirectStandardInput = true,
                WorkingDirectory = workingDirectory
            };

            AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

            var hostExitTokenSource = new CancellationTokenSource();
            HostProcess = new Process() { StartInfo = startInfo };
            HostProcess.Exited += (sender, e) =>
            {
                TriggerHostShutdown(hostExitTokenSource);
            };
            HostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
            HostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
            HostProcess.EnableRaisingEvents = true;
            HostProcess.Start();
            HostProcess.BeginErrorReadLine();
            HostProcess.BeginOutputReadLine();

            if (HostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, HostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, HostProcess.Id);
            return hostExitTokenSource.Token;
        }

        public void Dispose()
        {
            HostProcess?.Kill();
        }

        protected void DotnetPublish(string applicationPath, string applicationName)
         {
            using (Logger.BeginScope("dotnet-publish"))
            {
                if (string.IsNullOrEmpty(DeploymentParameters.TargetFramework))
                {
                    throw new Exception($"A target framework must be specified in the deployment parameters for applications that require publishing before deployment");
                }

                var applicationFullPath = Path.Combine(applicationPath, $"{Path.GetFileName(applicationName)}.csproj");
                var parameters = $"publish {applicationFullPath}"
                    + $" --output \"{DeploymentParameters.PublishedApplicationRootPath}\""
                    + $" --framework {DeploymentParameters.TargetFramework}"
                    + $" --configuration {DeploymentParameters.Configuration}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = parameters,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = applicationPath,
                };

                AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.PublishEnvironmentVariables);

                using (var hostProcess = new Process { StartInfo = startInfo})
                {
                    hostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
                    hostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
                    hostProcess.EnableRaisingEvents = true;

                    hostProcess.Start();
                    hostProcess.BeginErrorReadLine();
                    hostProcess.BeginOutputReadLine();

                    Logger.LogInformation($"Executing command dotnet {parameters}");

                    hostProcess.WaitForExit();

                    if (hostProcess.ExitCode != 0)
                    {
                        var message = $"dotnet publish exited with exit code : {hostProcess.ExitCode}";
                        Logger.LogError(message);
                        throw new Exception(message);
                    }

                    Logger.LogInformation($"dotnet publish finished with exit code : {hostProcess.ExitCode}");
                }
            }
        }

        void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo, List<KeyValuePair<string, string>> environmentVariables)
        {
            var environment = startInfo.Environment;
            SetEnvironmentVariable(environment, "ASPNETCORE_ENVIRONMENT", DeploymentParameters.EnvironmentName);

            foreach (var environmentVariable in environmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

        void SetEnvironmentVariable(IDictionary<string, string> environment, string name, string value)
        {
            if (value == null)
            {
                Logger.LogInformation("Removing environment variable {name}", name);
                environment.Remove(name);
            }
            else
            {
                Logger.LogInformation("SET {name}={value}", name, value);
                environment[name] = value;
            }
        }

        protected void TriggerHostShutdown(CancellationTokenSource hostShutdownSource)
        {
            Logger.LogInformation("Host process shutting down.");
            try
            {
                hostShutdownSource.Cancel();
            }
            catch (Exception)
            {
                // Suppress errors.
            }
        }
    }
}