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
            return new XDocument(new XElement(Dav + "multistatus", Responses.Select(r => r.ToXElement())));
        }
    }
}
