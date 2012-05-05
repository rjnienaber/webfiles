using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebFiles.Mvc;
using WebFiles.Mvc.Providers;
using NLog;
using System.Diagnostics;

namespace WebFiles.Web.Tests.Controllers
{
    public class WebDavController : WebFilesController 
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public WebDavController()
        {
            this.config = new Configuration { RootPath = "D:\\stuff" };
            this.storageProvider = new FileSystemProvider();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext.Items["Time_Action"] = Stopwatch.StartNew();
            base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            var stopwatch = HttpContext.Items["Time_Action"] as Stopwatch;
            stopwatch.Stop();
            var pathInfo = filterContext.RouteData.Values["pathInfo"];
            logger.Debug("Method: {0}, PathInfo: {1}, Time: {2}(ms)", Request.HttpMethod, pathInfo, stopwatch.ElapsedMilliseconds);
        }

    }
}
