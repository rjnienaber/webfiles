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
        public bool IsCollection { get; private set; }
        public long ContentLength { get; private set; }
        public DateTime LastModified { get; private set; }

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

        public XElement AddResourceType(bool isCollection)
        {
            IsCollection = isCollection;
            var property = new XElement(Dav + "resourcetype", 
                               isCollection ? new XElement(Dav + "collection") : null);
            Properties.Add(property);
            return property;
        }

        public XElement AddContentLength(long contentLength)
        {
            ContentLength = contentLength;
            return AddProperty("getcontentlength", contentLength.ToString());
        }

        public XElement AddLastModified(DateTime lastModified)
        {
            LastModified = lastModified;
            var property = AddProperty("getlastmodified", lastModified.ToString("r"));
            //XNamespace dateNs = "urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882";
            //property.SetAttributeValue(dateNs + "dt", "dateTime.rfc1123");
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
