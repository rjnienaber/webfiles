using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WebFiles.Mvc.Requests
{
    public class PropfindRequest
    {
        public XDocument XmlRequest { get; private set; }
        public string PathInfo { get; set; }
        public List<string> DavProperties { get; set; }
        public List<XElement> NonDavProperties { get; set; }

        public bool HasResourceType { get; set; }
        public bool HasGetContentLength { get; set; }
        public bool HasGetLastModified { get; set; }

        XNamespace Dav = Util.DavNamespace;

        public PropfindRequest()
        {
            DavProperties = new List<string>();
            NonDavProperties = new List<XElement>();
        }

        public PropfindRequest(string pathInfo, XDocument document)
        {
            PathInfo = pathInfo;
            XmlRequest = document;
            var properties = XmlRequest.Descendants().First().Descendants().First().Descendants();
            
            DavProperties = properties.Where(d => d.Name.Namespace == Dav).Select(e => e.Name.LocalName).ToList();

            HasResourceType = DavProperties.Contains("resourcetype");
            HasGetContentLength = DavProperties.Contains("getcontentlength");
            HasGetLastModified = DavProperties.Contains("getlastmodified");

            NonDavProperties = properties.Where(d => d.GetDefaultNamespace() != Dav).ToList();
        }
    }
}
