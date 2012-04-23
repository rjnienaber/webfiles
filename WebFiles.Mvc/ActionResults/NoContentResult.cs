using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;

namespace WebFiles.Mvc.ActionResults
{
    public class NoContentResult : ContentResult
    {
        public readonly int HttpStatusCode;
        public NoContentResult(int httpStatusCode)
        {
            this.HttpStatusCode = httpStatusCode;
            Content = "";
            ContentType = "text/html";
            Headers = new NameValueCollection();
        }

        public NameValueCollection Headers { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;
            foreach (var key in Headers.AllKeys)
                response.AddHeader(key, Headers[key]);

            response.StatusCode = HttpStatusCode;
            base.ExecuteResult(context);
        }
    }
}
