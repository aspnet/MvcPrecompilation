﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public static class ApplicationPaths
    {
        private const string SolutionName = "RazorViewCompilation.sln";

        public static string SolutionDirectory { get; } = GetSolutionDirectory();

        public static string ArtifactPackagesDirectory => Path.Combine(SolutionDirectory, "artifacts", "build");

        public static string GetTestAppDirectory(string appName) =>
            Path.Combine(SolutionDirectory, "testapps", appName);

        private static string GetSolutionDirectory()
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, SolutionName));
                if (solutionFileInfo.Exists)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            } while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution directory could not be found for {applicationBasePath}.");
        }
    }
}
