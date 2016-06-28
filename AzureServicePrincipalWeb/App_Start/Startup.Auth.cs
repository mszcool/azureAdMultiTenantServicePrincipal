using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Owin;
using System.Web;
using AzureServicePrincipalWeb.BusinessLogic;
using System;
using System.Diagnostics;

namespace AzureServicePrincipalWeb
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = AuthenticationConfig.ConfiguratinItems.ClientId,
                    Authority = AuthenticationConfig.ConfiguratinItems.Authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                        // If the app needs access to the entire organization, then add the logic
                        // of validating the Issuer here.
                        // IssuerValidator
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            //context.ProtocolMessage.Prompt = "admin_consent";
                            //context.ProtocolMessage.GrantType = "authorization_code";

                            context.ProtocolMessage.RedirectUri = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
                            //context.ProtocolMessage.Resource = AuthenticationContextHelper.ConfiguratinItems.ManagementAppUri;

                            return Task.FromResult(0);
                        },
                        SecurityTokenValidated = (context) =>
                        {
                            // If your authentication logic is based on users then add your logic here
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            // Pass in the context back to the app
                            context.OwinContext.Response.Redirect("/Home/Error");
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = async (context) =>
                        {
                            // Save the current authorization code
                            AuthenticationConfig.SessionItems.AuthCode = context.Code;
                            AuthenticationConfig.SessionItems.AuthCodeLastTokenRequestUrl = new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path));
                            AuthenticationConfig.SessionItems.UserObjectId = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                            try
                            {
                                await AuthenticationLogic.GetTokensForNeededServices();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("----");
                                Debug.WriteLine(ex.ToString());
                                Debug.WriteLine("----");
                                if (ex.InnerException != null) Debug.WriteLine(ex.InnerException.ToString());
                                Debug.WriteLine("----");
                            }
                        }
                    }
                });
        }
    }
}
