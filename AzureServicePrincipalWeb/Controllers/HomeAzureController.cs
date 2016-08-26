using AzureServicePrincipalWeb.BusinessLogic;
using AzureServicePrincipalWeb.ExtensionTypes;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AzureServicePrincipalWeb.Controllers
{
    public class HomeAzureController : Controller
    {
        // GET: AzureMgmt
        public async Task<ActionResult> Index()
        {
            return await this.SafeExecuteView(async () =>
            {
                var subscriptions = await AzureRmLogic.GetUserSubscriptions();
                return View(subscriptions);
            },
            () => { return View("Error"); });
        }
    }
}