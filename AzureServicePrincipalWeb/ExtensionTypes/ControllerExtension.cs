using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AzureServicePrincipalWeb.ExtensionTypes
{
    public static class ControllerExtension
    {
        public static async Task<ActionResult> SafeExecuteView(this Controller controller, Func<Task<ActionResult>> body, Func<ActionResult> createErrorResult)
        {
            try
            {
                return await body();
            }
            catch (Exception ex)
            {
                controller.ViewBag.ErrorTitle = $"Error while Executing Action {controller.Request.Path}";
                controller.ViewBag.ErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    controller.ViewBag.ErrorMessageDetails = ex.InnerException.Message;
                }
                return await Task.FromResult(createErrorResult());
            }
        }
    }
}