using System.Web.Http;
using System.Web.Mvc;

namespace Microsoft.Azure.Blast.Web.Areas.Kablammo.Controllers
{
    /// <summary>
    /// The controller that will handle requests for the results page.
    /// </summary>
    public class ResultsController : Controller
    {
        private const string ErrorViewName = "Error";

        public ResultsController()
            : this(GlobalConfiguration.Configuration)
        {
        }

        public ResultsController(HttpConfiguration config)
        {
            Configuration = config;
        }

        public HttpConfiguration Configuration { get; private set; }

        public ActionResult Index()
        {
            ViewBag.DocumentationProvider = Configuration.Services.GetDocumentationProvider();
            return View();
        }
    }
}