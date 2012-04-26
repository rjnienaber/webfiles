using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WebFiles.Mvc.ActionResults
{
    public class Response
    {
        public string Href { get; set; }
        public bool IsCollection { get; set; }

        public PropertyStatus Found { get; set; }
        public PropertyStatus NotFound { get; set; }

        public XNamespace Dav = MultiStatusResult.Dav;

        public Response()
        {
            Found = new PropertyStatus();
            NotFound = new PropertyStatus();
        }

        public XElement ToXElement()
        {
            var responseElement = new XElement(Dav + "response",
                                      new XElement(Dav + "href", Href));

            if (Found.Properties.Count > 0)
                responseElement.Add(Found.ToXElement());

            if (NotFound.Properties.Count > 0)
                responseElement.Add(NotFound.ToXElement());

            return responseElement;
        }
    }
}
