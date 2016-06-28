using Microsoft.Azure.ActiveDirectory.GraphClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace AzureServicePrincipalWeb.BusinessLogic
{
    public class ServicePrincipalResponse
    {
        public IApplication App { get; set; }
        public IServicePrincipal Principal { get; set; }
        public bool IsNewApp { get; set; }
        public bool IsNewPrincipal { get; set; }
    }

    public static class ServicePrincipalLogic
    {
        private static readonly string GRAPH_BASEURL = "https://graph.windows.net";

        public async static Task<ServicePrincipalResponse> CreateAppAndServicePrincipal(string displayName, string appIdUri, string password, string tenantId)
        {
            //
            // First create the Azure AD Graph API client proxy
            //
            var token = AuthenticationConfig.SessionItems.GraphAuthToken;
            var baseUri = new Uri(GRAPH_BASEURL);
            var adClient = new ActiveDirectoryClient
                                (
                                    new Uri(baseUri, tenantId),
                                    async () =>
                                    {
                                        if (token == null)
                                            throw new Exception("Authorization required before calling into Graph!");

                                        return await Task.FromResult<string>(token);
                                    }
                                );

            // 
            // First create the app backing up the service principal
            //
            try
            {
                var appResponse = await CreateAppObjectIfNotExists(displayName, appIdUri, adClient);

                //
                // Now we can create the service principal to be created in the AD
                //
                var spResponse = await CreateServicePrincipalIfNotExists(
                                        appResponse.App,
                                        password,
                                        adClient
                                  );

                // 
                // Finally return the AppId
                //
                return await Task.FromResult
                                (
                                    new ServicePrincipalResponse
                                    {
                                        App = appResponse.App,
                                        IsNewApp = appResponse.IsNewApp,
                                        Principal = spResponse.Principal,
                                        IsNewPrincipal = spResponse.IsNewPrincipal
                                    }
                                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                if (ex.InnerException != null) Debug.WriteLine(ex.InnerException.ToString());
                throw;
            }
        }

        private static async Task<ServicePrincipalResponse> CreateAppObjectIfNotExists(string displayName, string appIdUri, ActiveDirectoryClient adClient)
        {
            var isNewApp = false;
            var appCreated = default(IApplication);

            // First check if the App exists, already
            var appFilter = adClient.Applications.Where(app => app.IdentifierUris.Any(iduri => iduri == appIdUri));
            var foundApp = await appFilter.ExecuteAsync();
            if (foundApp.CurrentPage.Count == 0)
            {
                var newApp = new Application
                {
                    //AppId = Guid.NewGuid().ToString(),
                    DisplayName = displayName,
                    IdentifierUris = new List<string> { appIdUri }
                };
                await adClient.Applications.AddApplicationAsync(newApp);

                appCreated = newApp;

                isNewApp = true;
            }
            else
            {
                appCreated = foundApp.CurrentPage.FirstOrDefault();
            }

            return new ServicePrincipalResponse { App = appCreated, IsNewApp = isNewApp };
        }

        private static async Task<ServicePrincipalResponse> CreateServicePrincipalIfNotExists(IApplication app, string password, ActiveDirectoryClient adClient)
        {
            var isNewSp = false;
            var spCreated = default(IServicePrincipal);

            // First check if the service principal exists, already
            var appIdToFilter = app.AppId;
            var spFilter = adClient.ServicePrincipals.Where(sp => sp.AppId == appIdToFilter);
            var foundSp = await spFilter.ExecuteAsync();
            if (foundSp.CurrentPage.Count == 0)
            {
                spCreated = new ServicePrincipal
                {
                    AccountEnabled = true,
                    AppId = app.AppId,
                    DisplayName = app.DisplayName,
                    ServicePrincipalNames = new List<string> { app.AppId, app.IdentifierUris.First() },
                    PasswordCredentials = new List<PasswordCredential>
                                            {
                                                new PasswordCredential
                                                {
                                                    StartDate = DateTime.UtcNow,
                                                    EndDate = DateTime.UtcNow.AddYears(1),
                                                    Value = password,
                                                    KeyId = Guid.NewGuid()
                                                }
                                            }
                };

                // Submit the creation request and return the newly created Service Principal Object
                await adClient.ServicePrincipals.AddServicePrincipalAsync(spCreated);

                isNewSp = true;
            }
            else
            {
                spCreated = foundSp.CurrentPage.First();
                spCreated.PasswordCredentials.Add(
                                        new PasswordCredential
                                        {
                                            StartDate = DateTime.UtcNow,
                                            EndDate = DateTime.UtcNow.AddYears(1),
                                            Value = password,
                                            KeyId = Guid.NewGuid()
                                        }
                                    );
                await spCreated.UpdateAsync();
            }
            return new ServicePrincipalResponse { IsNewPrincipal = isNewSp, Principal = spCreated };
        }
    }
}