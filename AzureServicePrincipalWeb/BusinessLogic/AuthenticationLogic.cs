using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AzureServicePrincipalWeb.BusinessLogic
{
    public static class AuthenticationLogic
    {
        public static async Task<AuthenticationResult> RedeemTokenAsync(string targetResourceUri, Uri originalReplyUrl)
        {

            var code = AuthenticationConfig.SessionItems.AuthCode;
            var userObjectId = AuthenticationConfig.SessionItems.UserObjectId;

            var authContext = new AuthenticationContext
                                    (
                                        AuthenticationConfig.ConfiguratinItems.Authority,
                                        new AdalSimpleTokenCache(userObjectId)
                                    );
            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                                    code,
                                    originalReplyUrl,
                                    new ClientCredential(AuthenticationConfig.ConfiguratinItems.ClientId, AuthenticationConfig.ConfiguratinItems.AppKey),
                                    targetResourceUri
                                );
            return authResult;

        }

        public static async Task GetTokensForNeededServices()
        {
            // Try get the token for the service management APIs
            var token = await AuthenticationLogic.RedeemTokenAsync
                        (
                            AuthenticationConfig.ConfiguratinItems.ManagementAppUri,
                            AuthenticationConfig.SessionItems.AuthCodeLastTokenRequestUrl
                        );
            AuthenticationConfig.SessionItems.ManagementAuthToken = token.AccessToken;

            // Then try getting the token for Graph API calls
            var graphToken = await AuthenticationLogic.RedeemTokenAsync
                                    (
                                        AuthenticationConfig.ConfiguratinItems.GraphAppUri,
                                        AuthenticationConfig.SessionItems.AuthCodeLastTokenRequestUrl
                                    );
            AuthenticationConfig.SessionItems.GraphAuthToken = graphToken.AccessToken;
            AuthenticationConfig.SessionItems.GraphTargetTenant = graphToken.TenantId;
        }

        public static async Task<string> ConstructConsentUrlAsync(string tenantDomainOrId, string targetResourceUri, string redirectUrl, bool isAdmin = false)
        {
            var authorizationUrl = string.Empty;
            if (!string.IsNullOrEmpty(targetResourceUri))
            {
                authorizationUrl = string.Format(
                    "https://login.windows.net/{0}/oauth2/authorize?" +
                        "api-version=1.0&" +
                        "response_type=code&client_id={1}&" +
                        "resource={2}&" +
                        "redirect_uri ={3}",
                    tenantDomainOrId,
                    AuthenticationConfig.ConfiguratinItems.ClientId,
                    Uri.EscapeUriString(targetResourceUri),
                    redirectUrl
                );
            }
            else
            {
                authorizationUrl = string.Format(
                    "https://login.windows.net/{0}/oauth2/authorize?" +
                        "api-version=1.0&" +
                        "response_type=code&client_id={1}&" +
                        "redirect_uri ={2}",
                    tenantDomainOrId,
                    AuthenticationConfig.ConfiguratinItems.ClientId,
                    redirectUrl
                );

            }

            if(isAdmin)
            {
                authorizationUrl += "&prompt=admin_consent";
            }

            return await Task.FromResult<string>(authorizationUrl);
        }
    }
}