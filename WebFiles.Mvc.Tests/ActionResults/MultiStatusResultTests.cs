using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using WebFiles.Mvc.ActionResults;
using System.Xml.Linq;

namespace WebFiles.Mvc.Tests.ActionResults
{
    [TestFixture]
    [Category("action_results")]
    public class MultiStatusResultTests
    {
        [Test]
        public void print_empty_status_result()
        {
            var multiStatus = new MultiStatusResult();
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<multistatus xmlns=""DAV:"" />"));
        }

        [Test]
        public void adding_found_property_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 OK";
            XNamespace dateNs = "urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882";
            response.Found.AddProperty("getlastmodified", "Mon, 23 Apr 2012 23:09:47 GMT")
                          .SetAttributeValue(dateNs + "dt", "dateTime.rfc1123"); 
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<multistatus xmlns=""DAV:"">
  <response>
    <href>/public/litmus</href>
    <propstat>
      <prop>
        <getlastmodified p5:dt=""dateTime.rfc1123"" xmlns:p5=""urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882"">Mon, 23 Apr 2012 23:09:47 GMT</getlastmodified>
      </prop>
      <status>HTTP/1.1 200 OK</status>
    </propstat>
  </response>
</multistatus>"));
        }

        [Test]
        public void adding_not_found_property_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.NotFound.Status = "HTTP/1.1 404 Not Found";
            response.NotFound.AddProperty("getlastmodified", null);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<multistatus xmlns=""DAV:"">
  <response>
    <href>/public/litmus</href>
    <propstat>
      <prop>
        <getlastmodified />
      </prop>
      <status>HTTP/1.1 404 Not Found</status>
    </propstat>
  </response>
</multistatus>"));
        }

        [Test]
        public void mark_response_as_a_collection_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 Found";
            response.Found.AddCollectionProperty();
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<multistatus xmlns=""DAV:"">
  <response>
    <href>/public/litmus</href>
    <propstat>
      <prop>
        <resourcetype>
          <collection />
        </resourcetype>
      </prop>
      <status>HTTP/1.1 200 Found</status>
    </propstat>
  </response>
</multistatus>"));
        }

        [Test]
        public void namespaced_properties_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 Found";
            response.Found.AddProperty("http://example.com/neon/litmus/", "foo", null);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<multistatus xmlns=""DAV:"">
  <response>
    <href>/public/litmus</href>
    <propstat>
      <prop>
        <foo xmlns=""http://example.com/neon/litmus/"" />
      </prop>
      <status>HTTP/1.1 200 Found</status>
    </propstat>
  </response>
</multistatus>"));
        }
    }
}
