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

namespace WebFiles.Mvc.Tests
{
    [TestFixture]
    public class WebFilesControllerTests
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
        public void MKCOL_if_resource_exists_command_should_fail()
        {
            provider.Setup(p => p.CheckExists("D:\\stuff")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);

            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.ContentLength).Returns(0);

            var exception = Assert.Throws<HttpException>(() => controller.MakeCollection("litmus/"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(405));
            Assert.That(exception.Message, Is.EqualTo("path already exists"));
        }

        [Test]
        public void MKCOL_ensure_parent_path_exists()
        {
            provider.Setup(p => p.CheckExists("D:\\stuff")).Returns(false);
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.ContentLength).Returns(0);

            var exception = Assert.Throws<HttpException>(() => controller.MakeCollection("litmus/"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(409));
            Assert.That(exception.Message, Is.EqualTo("parent path doesn't exist"));
        }

        [Test]
        public void MKCOL_ensure_ancestors_exist()
        {
            provider.Setup(p => p.CheckExists("D:\\stuff")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "another")).Returns("D:\\stuff\\litmus\\another");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\another")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus\\another", "dir")).Returns("D:\\stuff\\litmus\\another\\dir");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\another\\dir")).Returns(false);

            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.ContentLength).Returns(0);

            var exception = Assert.Throws<HttpException>(() => controller.MakeCollection("litmus/another/dir/new"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(409));
            Assert.That(exception.Message, Is.EqualTo("ancestor path doesn't exist - /litmus/another/dir"));
        }

        [Test]
        public void MKCOL_create_collection_and_return_201_status()
        {
            provider.Setup(p => p.CheckExists("D:\\stuff")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(false);
            provider.Setup(p => p.CreateCollection("D:\\stuff\\litmus"));

            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.ContentLength).Returns(0);

            var result = controller.MakeCollection("litmus/") as NoContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(201));
        }

        [Test]
        public void MKCOL_should_throw_error_when_request_body_is_populated()
        {
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.ContentLength).Returns(8);
            var exception = Assert.Throws<HttpException>(() => controller.MakeCollection("litmus/"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(415));
            Assert.That(exception.Message, Is.EqualTo("request body not understood"));
        }

        [Test]
        public void DELETE_a_deletion_should_remove_resource()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.Delete("D:\\stuff\\litmus"));

            var result = controller.Delete("litmus/") as NoContentResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(204));
        }

        [Test]
        public void DELETE_a_deletion_on_non_existent_resource_should_fail()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(false);

            var exception = Assert.Throws<HttpException>(() => controller.Delete("litmus/"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(404));
            Assert.That(exception.Message, Is.EqualTo("path doesn't exist"));
        }

        [Test]
        public void OPTIONS_should_return_all_supported_http_methods()
        {
            var result = controller.Options("/") as NoContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(200));
            Assert.That(result.Headers["Allow"], Is.EqualTo("OPTIONS, DELETE, MKCOL, PUT"));
            Assert.That(result.Headers["Dav"], Is.EqualTo("1, 2"));
        }

        [Test]
        public void PUT_should_save_request_stream_to_provider()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "/litmus/newFile.txt")).Returns("D:\\stuff\\litmus\\newFile.txt");
            var ms = new MemoryStream();
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.InputStream).Returns(ms);
            provider.Setup(p => p.Save("D:\\stuff\\litmus\\newFile.txt", ms));

            var result = controller.Put("/litmus/newFile.txt") as NoContentResult;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(201));
        }

        [Test]
        public void GET_should_retrieve_an_existing_resource()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/newFile.txt")).Returns("D:\\stuff\\litmus\\newFile.txt");
            var result = controller.Get("litmus/newFile.txt") as FilePathResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.FileName, Is.EqualTo("D:\\stuff\\litmus\\newFile.txt"));
            Assert.That(result.ContentType, Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void COPY_should_duplicate_a_resource()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/newFile.txt")).Returns("D:\\stuff\\litmus\\newFile.txt");
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "newFile2.txt")).Returns("D:\\stuff\\litmus\\newFile2.txt");
            provider.Setup(p => p.Copy("D:\\stuff\\litmus\\newFile.txt", "D:\\stuff\\litmus\\newFile2.txt"));

            var headers = new NameValueCollection { {"Destination", "http://localhost/webdav/files/litmus/newFile2.txt" }};
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.Headers).Returns(headers);
            request.Setup(r => r.Url).Returns(new Uri("http://localhost/webdav/files/litmus/newFile.txt"));
            var result = controller.Copy("litmus/newFile.txt") as NoContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(201));
        }

        [Test]
        public void COPY_should_not_duplicate_a_resource_if_overwrite_header_is_present_and_false()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "newFile2.txt")).Returns("D:\\stuff\\litmus\\newFile2.txt");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\newFile2.txt")).Returns(true);

            context.Setup(c => c.Request).Returns(request.Object);
            
            var headers = new NameValueCollection { { "Destination", "http://localhost/webdav/files/litmus/newFile2.txt" }, { "Overwrite", "F"} };
            request.Setup(r => r.Headers).Returns(headers);
            request.Setup(r => r.Url).Returns(new Uri("http://localhost/webdav/files/litmus/newFile.txt"));
            var exception = Assert.Throws<HttpException>(() => controller.Copy("litmus/newFile.txt"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(412));
            Assert.That(exception.Message, Is.EqualTo("path already exists"));
        }

        [Test]
        public void COPY_should_overwrite_directories_by_default()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/newFile.txt")).Returns("D:\\stuff\\litmus\\newFile.txt");
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "adir")).Returns("D:\\stuff\\litmus\\adir");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\adir")).Returns(true);
            provider.Setup(p => p.Copy("D:\\stuff\\litmus\\newFile.txt", "D:\\stuff\\litmus\\adir"));

            var headers = new NameValueCollection { {"Destination", "http://localhost/webdav/files/litmus/adir" }};
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.Headers).Returns(headers);
            request.Setup(r => r.Url).Returns(new Uri("http://localhost/webdav/files/litmus/newFile.txt"));
            var result = controller.Copy("litmus/newFile.txt") as NoContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(204));
        }

        [Test]
        public void COPY_should_fail_if_one_of_ancestors_for_destination_is_missing()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "another")).Returns("D:\\stuff\\litmus\\another");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\another")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus\\another", "dir")).Returns("D:\\stuff\\litmus\\another\\dir");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus\\another\\dir")).Returns(false);

            var headers = new NameValueCollection { {"Destination", "http://localhost/webdav/files/litmus/another/dir/newFile.txt" }};
            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.Headers).Returns(headers);
            request.Setup(r => r.Url).Returns(new Uri("http://localhost/webdav/files/litmus/newFile.txt"));

            var exception = Assert.Throws<HttpException>(() => controller.Copy("litmus/newFile.txt"));

            Assert.That(exception.GetHttpCode(), Is.EqualTo(409));
            Assert.That(exception.Message, Is.EqualTo("ancestor path doesn't exist - /litmus/another/dir"));
        }

        [Test]
        public void MOVE_should_move_files_to_new_location()
        {
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus/newFile.txt")).Returns("D:\\stuff\\litmus\\newFile.txt");

            var headers = new NameValueCollection { {"Destination", "http://localhost/webdav/files/litmus/newFile2.txt" }};
            provider.Setup(p => p.JoinPath("D:\\stuff", "litmus")).Returns("D:\\stuff\\litmus");
            provider.Setup(p => p.CheckExists("D:\\stuff\\litmus")).Returns(true);
            provider.Setup(p => p.JoinPath("D:\\stuff\\litmus", "newFile2.txt")).Returns("D:\\stuff\\litmus\\newFile2.txt");

            provider.Setup(p => p.Move("D:\\stuff\\litmus\\newFile.txt", "D:\\stuff\\litmus\\newFile2.txt"));

            context.Setup(c => c.Request).Returns(request.Object);
            request.Setup(r => r.Headers).Returns(headers);
            request.Setup(r => r.Url).Returns(new Uri("http://localhost/webdav/files/litmus/newFile.txt"));
            var result = controller.Move("litmus/newFile.txt") as NoContentResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("text/html"));
            Assert.That(result.Content, Is.EqualTo(""));
            Assert.That(result.HttpStatusCode, Is.EqualTo(201));

        }
    }
}