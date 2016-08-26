﻿using AzureServicePrincipalWeb.BusinessLogic;
using AzureServicePrincipalWeb.Models;
using AzureServicePrincipalWeb.ExtensionTypes;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AzureServicePrincipalWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            return await this.SafeExecuteView(() =>
            {
                // Create the basic model for the view
                var newSp = new ServicePrincipalModel
                {
                    UserMessage = "Please submit your request!",
                    AppId = Guid.Empty.ToString(),
                    ShowSpDetails = true,
                    SubmitConsentEnabled = false,
                    SubmitSpEnabled = true
                };

                // If not all tokens are available, update the user action and update the user message
                if (AuthenticationConfig.SessionItems.GraphAuthToken == null || AuthenticationConfig.SessionItems.ManagementAuthToken == null)
                {
                    newSp.ShowSpDetails = false;
                    newSp.SubmitConsentEnabled = true;
                    newSp.SubmitSpEnabled = false;
                    newSp.UserMessage = "Please initate a consent with the button below as it looks we were unable to get access to management APIs right away! This might be possible because you need admin consent, consent expired or you used Microsoft Accounts (MSA) for signing in!";
                }
                else
                {
                    newSp.TenantId = AuthenticationConfig.SessionItems.GraphTargetTenant;

                }

                // Admin-Consent is enabled, only, if a target tenant is given
                newSp.SubmitAdminConsentEnabled = !string.IsNullOrEmpty(AuthenticationConfig.SessionItems.GraphTargetTenant);

                // Show the view witht he model
                return Task.FromResult<ActionResult>(View(newSp));
            }, 
            () => { return View("Error"); });
        }


        [HttpPost]
        public async Task<ActionResult> SubmitServicePrincipal(ServicePrincipalModel newPrincipal)
        {
            return await this.SafeExecuteView(async () =>
            {
                // Add the default values to the model
                newPrincipal.TenantId = AuthenticationConfig.SessionItems.GraphTargetTenant;

                // Validate required attributes for Service Principal Submission
                if (string.IsNullOrEmpty(newPrincipal.Password)) ModelState.AddModelError("Password", "Missing password!");
                if (string.IsNullOrEmpty(newPrincipal.DisplayName)) ModelState.AddModelError("DisplayName", "Missing display name for the principal!");
                if (string.IsNullOrEmpty(newPrincipal.AppIdUri)) ModelState.AddModelError("AppIdUri", "Missing AppId Uri for the app created for the principal!");

                var tenantIdGuid = default(Guid);
                if (!Guid.TryParse(newPrincipal.TenantId, out tenantIdGuid)) ModelState.AddModelError("TenantId", "Invalid TenantId - TenantId must be GUID!");

                // Depending on model state, create the principal or skip creation
                if (ModelState.IsValid)
                {
                    var appForSp = await ServicePrincipalLogic.CreateAppAndServicePrincipal
                                                (
                                                    newPrincipal.DisplayName,
                                                    newPrincipal.AppIdUri,
                                                    newPrincipal.Password,
                                                    newPrincipal.TenantId
                                                );

                    newPrincipal.DisplayName = appForSp.App.DisplayName;
                    newPrincipal.AppIdUri = appForSp.App.IdentifierUris.First();
                    newPrincipal.AppId = appForSp.App.AppId;

                    var messageBuilder = new StringBuilder();
                    messageBuilder.Append($"Request executed successfully at {DateTime.Now.ToString("yyyy-MM-dd hh:mm")}:{Environment.NewLine}");
                    messageBuilder.AppendFormat("- {0}{1}", (appForSp.IsNewApp ? "Created new App!" : "Re-used existing App!"), Environment.NewLine);
                    messageBuilder.AppendFormat("- {0}{1}", (appForSp.IsNewPrincipal ? "Created new Service Principal on App!" : "Re-used existing Service Principal on App and added new password!"), Environment.NewLine);

                    newPrincipal.UserMessage = messageBuilder.ToString();
                }
                else
                {
                    newPrincipal.UserMessage = "Please fix validation errors in your data entry!";
                }

                newPrincipal.SubmitSpEnabled = true;
                newPrincipal.ShowSpDetails = true;
                newPrincipal.SubmitConsentEnabled = false;
                newPrincipal.SubmitAdminConsentEnabled = true;
                return View("Index", newPrincipal);
            },
            () => { return View("Error"); });
        }

        public async Task<ActionResult> ConsentReset()
        {
            AuthenticationConfig.SessionItems.AuthCode = null;
            AuthenticationConfig.SessionItems.GraphAuthToken = null;
            AuthenticationConfig.SessionItems.GraphTargetTenant = null;
            AuthenticationConfig.SessionItems.ManagementAuthToken = null;
            return await Task.FromResult(RedirectToAction("Index"));
        }

        [HttpPost]
        public async Task<ActionResult> InitiateConsent(ServicePrincipalModel principalModel)
        {
            return await this.SafeExecuteView(async () =>
            {
                // Validate the basic input data
                if (string.IsNullOrEmpty(principalModel.ConsentAzureAdTenantDomainOrId)) ModelState.AddModelError("ConsentAzureAdTenantDomainOrId", "Please enter a Tenant ID (GUID) or a Tenant Domain (e.g. xyz.onmicrosoft.com) for initiaiting the consent!");

                if (ModelState.IsValid)
                {
                    // Start the consent flow for the target tenant
                    var redirectUrl = string.Format("{0}{1}",
                                        Request.Url.GetLeftPart(UriPartial.Authority),
                                        Url.Action("CatchConsentResult"));
                    var authorizationUrl = await AuthenticationLogic.ConstructConsentUrlAsync
                                                    (
                                                        principalModel.ConsentAzureAdTenantDomainOrId,
                                                        AuthenticationConfig.ConfiguratinItems.ManagementAppUri,
                                                        redirectUrl
                                                    );

                    return Redirect(authorizationUrl);
                }
                else
                {
                    principalModel.SubmitConsentEnabled = true;
                    principalModel.SubmitSpEnabled = false;
                    principalModel.SubmitAdminConsentEnabled = false;
                    principalModel.UserMessage = "Please fix errors and try initiating the consent again!";
                    return View("Index", principalModel);
                }

            },
            () => { return View("Error"); });
        }

        [HttpPost]
        public async Task<ActionResult> InitiateAdminConsent(ServicePrincipalModel principalModel)
        {
            return await this.SafeExecuteView(async () =>
            {
                // Validate the basic input data
                if (string.IsNullOrEmpty(AuthenticationConfig.SessionItems.GraphTargetTenant))
                {
                    throw new Exception("Cannot initate Admin Consent without a default tenant known!");
                }
                else
                {
                    // Start the consent flow for the target tenant
                    var redirectUrl = string.Format("{0}{1}",
                                        Request.Url.GetLeftPart(UriPartial.Authority),
                                        Url.Action("CatchConsentResult"));
                    var authorizationUrl = await AuthenticationLogic.ConstructConsentUrlAsync
                                                    (
                                                        AuthenticationConfig.SessionItems.GraphTargetTenant,
                                                        AuthenticationConfig.ConfiguratinItems.ManagementAppUri,
                                                        redirectUrl,
                                                        true
                                                    );

                    return Redirect(authorizationUrl);
                }
            },
            () => { return View("Error"); });
        }

        [HttpGet]
        public async Task<ActionResult> CatchConsentResult(string code, string error, string error_description)
        {
            return await this.SafeExecuteView(async () =>
            {
                if (code != null)
                {
                    // Capture the code and the last redirect URL for this session
                    AuthenticationConfig.SessionItems.AuthCode = code;
                    AuthenticationConfig.SessionItems.AuthCodeLastTokenRequestUrl =
                                    new Uri(string.Format("{0}{1}", Request.Url.GetLeftPart(UriPartial.Authority),
                                                                    Url.Action("CatchConsentResult")));

                    // Try get the tokens
                    await AuthenticationLogic.GetTokensForNeededServices();

                    // Go back to the index action
                    return RedirectToAction("Index");
                }
                else
                {
                    if (error != null) ViewBag.ErrorTitle = error;
                    else ViewBag.ErrorTitle = "Unknown Error";

                    if (error_description != null) ViewBag.ErrorMessage = error_description;
                    else ViewBag.ErrorMessage = "Please verify the app configuration in your AAD or if you have access to an Azure Subscription!";

                    return View("Error");
                }
            },
            () => { return View("Error"); });
        }

        [HttpGet]
        public ActionResult About()
        {
            ViewBag.Message = "Little helper web application that creates a Service Principal for apps in your directory.";

            return View();
        }

        [HttpGet]
        public ActionResult Contact()
        {
            ViewBag.Message = "http://blog.mszcool.com";

            return View();
        }
    }
}