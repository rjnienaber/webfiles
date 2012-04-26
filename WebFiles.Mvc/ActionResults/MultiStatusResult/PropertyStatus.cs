using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WebFiles.Mvc.ActionResults
{
    public class PropertyStatus
    {
        public string Status { get; set; }
        public List<XElement> Properties { get; private set; }
        public XNamespace Dav = MultiStatusResult.Dav;

        public PropertyStatus()
        {
            Properties = new List<XElement>();
        }

        public XElement AddProperty(string name, string value)
        {
            return AddProperty(Dav, name, value);
        }

        public XElement AddProperty(XNamespace nameSpace, string name, string value)
        {
            var property = new XElement(nameSpace + name, value);
            Properties.Add(property);
            return property;
        }

        public XElement AddCollectionProperty()
        {
            var property = new XElement(Dav + "resourcetype",
                               new XElement(Dav + "collection"));
            Properties.Add(property);
            return property;
        }

        public XElement ToXElement()
        {
            return new XElement(Dav + "propstat",
                        new XElement(Dav + "prop", Properties),
                        new XElement(Dav + "status", Status)
                    );
        }
    }
}
