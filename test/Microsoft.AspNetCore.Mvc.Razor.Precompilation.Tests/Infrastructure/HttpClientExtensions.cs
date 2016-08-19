﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public static class HttpClientExtensions
    {
        public static async Task<string> GetStringWithRetryAsync(
            this HttpClient httpClient, 
            string url,
            ILogger logger)
        {
            var response = await RetryHelper.RetryRequest(() => httpClient.GetAsync(url), logger, retryCount: 5);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Assert.False(true,
                    $"Failed to GET content from {url}. Status code {response.StatusCode}." + 
                    Environment.NewLine +
                    content);
            }

            return content;
        }
    }
}
