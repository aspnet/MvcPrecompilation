// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class SimpleAppWithAssemblyRenameTest : IClassFixture<SimpleAppWithAssemblyRenameTest.SimpleAppWithAssemblyRenameTestFixture>
    {
        public SimpleAppWithAssemblyRenameTest(SimpleAppWithAssemblyRenameTestFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        public static TheoryData SupportedFlavorsTheoryData => RuntimeFlavors.SupportedFlavorsTheoryData;

        [Theory]
        [MemberData(nameof(SupportedFlavorsTheoryData))]
        public async Task Precompilation_WorksForSimpleApps(RuntimeFlavor flavor)
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
				// ASSERT WORKS WITHOUT RENAME
				//Assert.Equal("AspNetCore._Views_Home_Index_cshtml, SimpleAppWithAssemblyRename.PrecompiledViews, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", response);
				
				Assert.Equal("AspNetCore._Views_Home_Index_cshtml, NewAssemblyName.PrecompiledViews, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", response);
			}
        }

        public class SimpleAppWithAssemblyRenameTestFixture : ApplicationTestFixture
        {
            public SimpleAppWithAssemblyRenameTestFixture()
                : base("SimpleAppWithAssemblyRename")
            {
            }
			
        }
    }
}
