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

        public WebDavController() : base(new FileSystemProvider("D:\\stuff"))
        {
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpContext.Items["Time_Action"] = Stopwatch.StartNew();
            base.OnActionExecuting(filterContext);
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);
            var pathInfo = filterContext.RouteData.Values["pathInfo"];
            LogStopwatch(pathInfo, true);
            logger.Error("Exception: {0}", filterContext.Exception.ToString());
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            var pathInfo = filterContext.RouteData.Values["pathInfo"];
            LogStopwatch(pathInfo, false);
        }

        void LogStopwatch(object pathInfo, bool error)
        {
            var stopwatch = HttpContext.Items["Time_Action"] as Stopwatch;
            stopwatch.Stop();
            var errorMessage = error ? "*ERROR*" : "";
            logger.Debug("Method: {0}, PathInfo: {1}, Time: {2}(ms) {3}", Request.HttpMethod, pathInfo, stopwatch.ElapsedMilliseconds, errorMessage);
        }

    }
}
