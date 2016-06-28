using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureServicePrincipalWeb.Models
{
    public class ServicePrincipalModel
    {
        public string DisplayName { get; set; }
        public string AppIdUri { get; set; }
        public string Password { get; set; }
        public string TenantId { get; set; }
        public string AppId { get; set; }
        public string UserMessage { get; set; }
        public bool ShowSpDetails { get; set; }
        public bool SubmitSpEnabled { get; set; }
        public bool SubmitConsentEnabled { get; set; }
        public string ConsentAzureAdTenantDomainOrId { get; set; }
    }
}