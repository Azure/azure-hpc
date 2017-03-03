using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Areas.Kablammo
{
    public class KablammoAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Kablammo";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Kablammo_default",
                "Results/{action}/{id}",
                new { controller = "Results", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}