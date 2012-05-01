using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WebFiles.Mvc.ActionResults
{
    public class MultiStatusResult : XmlResult
    {
        public static readonly XNamespace Dav = Util.DavNamespace; 
        public List<Response> Responses { get; private set; }

        public MultiStatusResult()
        {
            Responses = new List<Response>();
        }

        protected override XDocument Construct()
        {
            var dav = new XAttribute(XNamespace.Xmlns + "d", Dav);
            return new XDocument(new XElement(Dav + "multistatus", dav, Responses.Select(r => r.ToXElement())));
        }
    }
}
