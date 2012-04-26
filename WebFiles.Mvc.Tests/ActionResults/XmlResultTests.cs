using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using WebFiles.Mvc.ActionResults;
using System.Xml.Linq;
using System.Web.Mvc;
using Moq;
using System.Web;
using System.Web.Routing;
using System.IO;

namespace WebFiles.Mvc.Tests.ActionResults
{
    [TestFixture]
    [Category("action_results")]
    public class XmlResultTests
    {
        Mock<HttpResponseBase> response = null;
        ControllerContext controllerContext;
        MockRepository factory = null;

        [SetUp]
        public void Setup()
        {
            factory = new MockRepository(MockBehavior.Default);
            var controller = new MockController();
            var context = factory.Create<HttpContextBase>();
            response = factory.Create<HttpResponseBase>();
            context.Setup(c => c.Response).Returns(response.Object);
            controllerContext = controller.ControllerContext = new ControllerContext(context.Object, new RouteData(), controller);
        }

        [TearDown]
        public void Teardown()
        {
            factory.VerifyAll();
        }

        [Test]
        public void should_set_content_type() 
        {
            response.SetupSet(r => r.ContentType = "application/xml");
            var writer = new StringWriter();
            response.Setup(r => r.Output).Returns(writer);

            var mockResult = new MockXmlResult();
            mockResult.ExecuteResult(controllerContext);

            Assert.That(writer.ToString(), Is.EqualTo(@"<?xml version=""1.0"" encoding=""utf-16""?><test />"));
        }
    }

    public class MockXmlResult : XmlResult {

        protected override XDocument Construct()
        {
           return new XDocument(new XElement("test"));
        }
    }

    public class MockController : Controller
    {

    }
}
