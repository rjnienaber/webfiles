using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using WebFiles.Mvc.Requests;
using System.Xml.Linq;

namespace WebFiles.Mvc.Tests.Input
{
    [TestFixture]
    [Category("requests")]
    public class PropfindRequestTests
    {
        [Test]
        public void should_return_dav_properties_from_xml()
        {
            var request = @"<propfind xmlns=""DAV:"">
    <prop>
        <getcontentlength xmlns=""DAV:"" />
        <getlastmodified xmlns=""DAV:"" />
        <displayname xmlns=""DAV:"" />
    </prop>
</propfind>";

            var propFind = new PropfindRequest(null, null, XDocument.Parse(request));

            Assert.That(propFind.DavProperties.Count, Is.EqualTo(3));
            Assert.That(propFind.DavProperties[0], Is.EqualTo("getcontentlength"));
            Assert.That(propFind.DavProperties[1], Is.EqualTo("getlastmodified"));
            Assert.That(propFind.DavProperties[2], Is.EqualTo("displayname"));
            Assert.That(propFind.HasResourceType, Is.False);
        }

        [Test]
        public void should_return_request_for_resource_type_as_separate_property()
        {
            var request = @"<propfind xmlns=""DAV:"">
    <prop>
        <resourcetype xmlns=""DAV:"" />
    </prop>
</propfind>";

            var propFind = new PropfindRequest(null, null, XDocument.Parse(request));

            Assert.That(propFind.DavProperties.Count, Is.EqualTo(1));
            Assert.That(propFind.HasResourceType, Is.True);
        }

        [Test]
        public void should_return_non_dav_properties_from_xml()
        {
            var request = @"<propfind xmlns=""DAV:"">
    <prop>
        <foo xmlns=""http://example.com/neon/litmus/"" />
        <bar xmlns=""http://example.com/neon/litmus/"" />
    </prop>
</propfind>";

            var propFind = new PropfindRequest(null, null, XDocument.Parse(request));

            Assert.That(propFind.NonDavProperties.Count, Is.EqualTo(2));
            Assert.That(propFind.NonDavProperties[0].Name.LocalName, Is.EqualTo("foo"));
            Assert.That(propFind.NonDavProperties[0].Name.Namespace.NamespaceName, Is.EqualTo("http://example.com/neon/litmus/"));
            Assert.That(propFind.NonDavProperties[1].Name.LocalName, Is.EqualTo("bar"));
            Assert.That(propFind.NonDavProperties[1].Name.Namespace.NamespaceName, Is.EqualTo("http://example.com/neon/litmus/"));
        }

        [Test]
        public void should_handle_depth_header()
        {
            var request = @"<propfind xmlns=""DAV:""><prop /></propfind>";

            var propFind = new PropfindRequest(null, null, XDocument.Parse(request));
            Assert.That(propFind.Depth, Is.EqualTo(0));
        }

    }
}
