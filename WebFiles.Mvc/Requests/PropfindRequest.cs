using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WebFiles.Mvc.Requests
{
    public class PropfindRequest
    {
        public List<string> DavProperties { get; set; }
        public List<XElement> NonDavProperties { get; set; }

        public PropfindRequest()
        {
            DavProperties = new List<string>();
            NonDavProperties = new List<XElement>();
        }

        public PropfindRequest(XDocument document)
        {
            var properties = document.Descendants().First().Descendants().First().Descendants();
            var resourceTypeProperty = properties.FirstOrDefault(d => d.GetDefaultNamespace() == Util.DavNamespace && d.Name.LocalName == "resourcetype");

            DavProperties = properties.Where(d => d.GetDefaultNamespace() == Util.DavNamespace && d != resourceTypeProperty).Select(e => e.Name.LocalName).ToList();
            NonDavProperties = properties.Where(d => d.GetDefaultNamespace() != Util.DavNamespace).ToList();
            HasResourceTypeProperty = resourceTypeProperty != null;
        }

        public bool HasResourceTypeProperty { get; set; }
    }
}
