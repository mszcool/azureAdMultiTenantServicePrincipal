using AzureServicePrincipalWeb.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace AzureServicePrincipalWeb.BusinessLogic
{
    public static class AzureRmLogic
    {
        public static async Task<List<AzureSubscriptionModel>> GetUserSubscriptions()
        {
            var results = new List<AzureSubscriptionModel>();
            var mgmtAuthToken = AuthenticationConfig.SessionItems.ManagementAuthToken;
            var mgmtBaseUrl = AuthenticationConfig.ConfiguratinItems.ManagementAppUri;
            var mgmtApiVersion = AuthenticationConfig.ConfiguratinItems.ManagementApiVersion;

            // Put everything together for the request
            var requestUrl = $"{mgmtBaseUrl}subscriptions?api-version={mgmtApiVersion}";
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mgmtAuthToken);

            // Execute the request against the ARM APIs
            var response = await httpClient.SendAsync(request);
            if(!response.IsSuccessStatusCode)
            {
                var errorStatusCode = response.StatusCode;
                var errorBodyDetails = string.Empty;
                try
                {
                    errorBodyDetails = await response.Content.ReadAsStringAsync();
                } catch { }
                throw new Exception($"Failed executing request against ARM APIs to determine list of subscriptions: {errorStatusCode}\r\n{errorBodyDetails}");
            }

            // Request executed successfully, build up list of subscriptions
            var contentString = await response.Content.ReadAsStringAsync();
            var contentValue = (System.Web.Helpers.Json.Decode(contentString)).value;
            foreach(var subs in contentValue)
            {
                results.Add(new AzureSubscriptionModel
                {
                    Id = subs.subscriptionId,
                    DisplayName = subs.displayName
                });
            }

            // No subscriptions found
            return results;
        }
    }
}