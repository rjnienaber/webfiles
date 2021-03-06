﻿using System;
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

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"" />"));
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

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:getlastmodified p5:dt=""dateTime.rfc1123"" xmlns:p5=""urn:uuid:c2f41010-65b3-11d1-a29f-00aa00c14882"">Mon, 23 Apr 2012 23:09:47 GMT</d:getlastmodified>
      </d:prop>
      <d:status>HTTP/1.1 200 OK</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
        }

        [Test]
        public void adding_last_modified_property_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 OK";
            var dateTime = new DateTime(2012, 4, 23, 23, 09, 47);
            response.Found.AddLastModified(dateTime);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:getlastmodified>Mon, 23 Apr 2012 23:09:47 GMT</d:getlastmodified>
      </d:prop>
      <d:status>HTTP/1.1 200 OK</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
            Assert.That(response.Found.LastModified, Is.EqualTo(dateTime));
        }


        [Test, Ignore]
        public void adding_not_found_property_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.NotFound.Status = "HTTP/1.1 404 Not Found";
            response.NotFound.AddProperty("getlastmodified", null);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:getlastmodified />
      </d:prop>
      <d:status>HTTP/1.1 404 Not Found</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
        }

        [Test]
        public void mark_response_as_a_collection_should_be_returned_in_xml()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 Found";
            response.Found.AddResourceType(true);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:resourcetype>
          <d:collection />
        </d:resourcetype>
      </d:prop>
      <d:status>HTTP/1.1 200 Found</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
            Assert.That(response.Found.IsCollection, Is.True);
        }

        [Test]
        public void mark_response_as_empty_resource_type()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 Found";
            response.Found.AddResourceType(false);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:resourcetype />
      </d:prop>
      <d:status>HTTP/1.1 200 Found</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
            Assert.That(response.Found.IsCollection, Is.False);
        }

        [Test]
        public void set_content_length_property()
        {
            var response = new Response { Href = "/public/litmus" };
            response.Found.Status = "HTTP/1.1 200 Found";
            response.Found.AddContentLength(4096);
            var multiStatus = new MultiStatusResult();
            multiStatus.Responses.Add(response);
            var result = multiStatus.ToString();

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <d:getcontentlength>4096</d:getcontentlength>
      </d:prop>
      <d:status>HTTP/1.1 200 Found</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
            Assert.That(response.Found.ContentLength, Is.EqualTo(4096));
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

            Assert.That(result, Is.EqualTo(@"<d:multistatus xmlns:d=""DAV:"">
  <d:response>
    <d:href>/public/litmus</d:href>
    <d:propstat>
      <d:prop>
        <foo xmlns=""http://example.com/neon/litmus/"" />
      </d:prop>
      <d:status>HTTP/1.1 200 Found</d:status>
    </d:propstat>
  </d:response>
</d:multistatus>"));
        }
    }
}
