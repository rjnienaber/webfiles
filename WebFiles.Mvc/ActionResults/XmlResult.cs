using System.Web.Mvc;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.IO;

namespace WebFiles.Mvc.ActionResults
{
    public class XmlResult : ActionResult
    {
        protected virtual XDocument Construct()
        {
            return new XDocument();
        }


        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = "application/xml";
            var writer = new XmlTextWriter(response.Output);
            Construct().WriteTo(writer);
        }

        public override string ToString()
        {
            return Construct().ToString();
        }
    }
}