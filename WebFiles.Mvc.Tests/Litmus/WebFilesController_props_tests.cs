using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Moq;
using System.Web;
using System.Diagnostics;
using System.Web.Mvc;
using System.IO;
using WebFiles.Mvc.Providers;
using WebFiles.Mvc.ActionResults;
using System.Collections.Specialized;
using System.Web.Routing;
using WebFiles.Mvc.Requests;
using System.Xml.Linq;

namespace WebFiles.Mvc.Tests.Litmus
{
    [TestFixture]
    [Category("props")]
    public class WebFilesController_props_tests
    {
        Configuration config = null;
        Mock<IStorageProvider> provider = null;
        Mock<HttpContextBase> context = null;
        Mock<HttpRequestBase> request = null;
        WebFilesController controller = null;
        MockRepository factory = null;

        [SetUp]
        public void Setup()
        {
            config = new Configuration { RootPath = "D:\\stuff" };
            factory = new MockRepository(MockBehavior.Default);
            provider = factory.Create<IStorageProvider>();
            controller = new WebFilesController(config, provider.Object);
            context = factory.Create<HttpContextBase>();
            request = factory.Create<HttpRequestBase>();
            controller.ControllerContext = new ControllerContext(context.Object, new RouteData(), controller);
        }

        [TearDown]
        public void Teardown()
        {
            factory.VerifyAll();
        }

        [Test]
        public void PROPFIND_should_fail_with_non_well_formed_xml()
        {
            var ms = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><propfind xmlns=\"DAV:\"><prop></propfind>");
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            request.Setup(r => r.InputStream).Returns(ms);
            context.Setup(c => c.Request).Returns(request.Object);

            var exception = Assert.Throws<HttpException>(() => controller.Propfind("litmus/"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(400));
            Assert.That(exception.Message, Is.EqualTo("malformed xml request body"));
        }

        [Test]
        public void PROPFIND_should_return_status_for_directory()
        {
            var request = new PropfindRequest();
            request.DavProperties.AddRange(new[] { "getcontentlength" });
            XNamespace ns = "urn:someNamespace";
            request.NonDavProperties.Add(new XElement(ns + "test"));

            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            var multiStatusResult = new MultiStatusResult();
            provider.Setup(p => p.Process(request)).Returns(multiStatusResult);

            var result = controller.Propfind("litmus/", request) as MultiStatusResult;

            Assert.That(result, Is.Not.Null);

            Assert.That(result, Is.SameAs(multiStatusResult));
            Assert.That(result.Responses.Count, Is.EqualTo(1));
            Assert.That(result.Responses[0].Href, Is.EqualTo("litmus/"));
        }
    }
}