﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureServicePrincipalWeb.Models
{
    public class AzureSubscriptionModel
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string TenantId { get; set; }
    }
}