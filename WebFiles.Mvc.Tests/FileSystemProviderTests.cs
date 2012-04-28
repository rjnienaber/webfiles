﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using WebFiles.Mvc.Providers;
using WebFiles.Mvc.Requests;
using System.Xml.Linq;

namespace WebFiles.Mvc.Tests
{
    [TestFixture]
    public class FileSystemProviderTests
    {
        FileSystemProvider fileSystem = new FileSystemProvider();

        List<string> paths;

        [SetUp]
        public void Setup()
        {
            paths = new List<string>();
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        string CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return AddPath(path);
        }

        string AddPath(string path)
        {
            paths.Add(path);
            return path;
        }


        [Test]
        public void Normalize_urls()
        {
            Assert.That(fileSystem.JoinPath("d:\\stuff", "litmus/"), Is.EqualTo("d:\\stuff\\litmus"));
            Assert.That(fileSystem.JoinPath("d:\\stuff", "litmus/another"), Is.EqualTo("d:\\stuff\\litmus\\another"));
        }

        [Test]
        public void Delete_of_a_file_off_disk()
        {
            var file = AddPath(Path.GetTempFileName());
            fileSystem.Delete(file);
            Assert.That(File.Exists(file), Is.False);
        }

        [Test]
        public void Delete_a_directory_recurisively_off_disk()
        {
            var newTempPath = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var newTempPath2 = CreateDirectory(Path.Combine(newTempPath, Path.GetRandomFileName()));

            fileSystem.Delete(newTempPath);
            Assert.That(Directory.Exists(newTempPath2), Is.False);
            Assert.That(Directory.Exists(newTempPath), Is.False);
       }

        [Test]
        public void Delete_a_non_existent_file_should_do_nothing()
        {
            fileSystem.Delete(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        }

        [Test]
        public void Save_resource_creates_file_on_disk()
        {
            var ms = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes("save this resource");
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;

            var fileName = AddPath(Path.GetTempFileName());
            fileSystem.Save(fileName, ms);

            var text = File.ReadAllText(fileName);
            Assert.That(text, Is.EqualTo("save this resource"));
       }

        [Test]
        public void Copy_resource_to_a_new_destination()
        {
            var tempFileSource = AddPath(Path.GetTempFileName());
            File.WriteAllText(tempFileSource, "copy resource to new location");

            var tempFileDestination = AddPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Assert.That(File.Exists(tempFileDestination), Is.False);
            fileSystem.Copy(tempFileSource, tempFileDestination);
            Assert.That(File.Exists(tempFileDestination), Is.True);

            Assert.That(File.ReadAllText(tempFileDestination), Is.EqualTo("copy resource to new location"));
       }

        [Test]
        public void Copy_resource_to_a_new_destination_should_overwrite_by_default()
        {
            string tempFileSource = AddPath(Path.GetTempFileName());
            File.WriteAllText(tempFileSource, "copy resource to new location");

            var tempFileDestination = AddPath(Path.GetTempFileName());
            Assert.That(File.Exists(tempFileDestination), Is.True);

            fileSystem.Copy(tempFileSource, tempFileDestination);
            Assert.That(File.Exists(tempFileDestination), Is.True);

            Assert.That(File.ReadAllText(tempFileDestination), Is.EqualTo("copy resource to new location"));
        }

        [Test]
        public void Determines_whether_a_path_is_a_directory_or_not()
        {
            var newTempPath = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(newTempPath);
            Assert.That(Directory.Exists(newTempPath), Is.True);
            Assert.That(fileSystem.IsACollection(newTempPath), Is.True);

            var tempFile = AddPath(Path.GetTempFileName());
            Assert.That(fileSystem.IsACollection(tempFile), Is.False);
        }

        [Test]
        public void Copy_should_create_the_directory_its_copying_to()
        {
            string startDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(startDir);
            string startDirTempFile = AddPath(Path.Combine(startDir, Path.GetRandomFileName()));
            File.WriteAllText(startDirTempFile, "start dir temp file");

            string newDir = AddPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            fileSystem.Copy(startDir, newDir);

            var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

            var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
            Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
        }

        [Test]
        public void Copy_recursively_including_directories()
        {
            var startDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var startDirTempFile = AddPath(Path.Combine(startDir, Path.GetRandomFileName()));
            File.WriteAllText(startDirTempFile, "start dir temp file");

            var subDir = CreateDirectory(Path.Combine(startDir, Path.GetRandomFileName()));
            Directory.CreateDirectory(subDir);
            var subDirTempFile = AddPath(Path.Combine(subDir, Path.GetRandomFileName()));
            File.WriteAllText(subDirTempFile, "sub dir temp file");

            var newDir = AddPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            
            fileSystem.Copy(startDir, newDir);

            var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

            var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
            Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
            var newSubDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(subDirTempFile));
            Assert.That(File.ReadAllText(newSubDirTempFile), Is.EqualTo("sub dir temp file"));
        }

        [Test]
        public void Move_should_move_a_file_to_new_location()
        {
            var startDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var startDirTempFile = AddPath(Path.Combine(startDir, Path.GetRandomFileName()));
            File.WriteAllText(startDirTempFile, "start dir temp file");

            var newDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var newDirTempFile = Path.Combine(newDir, Path.GetRandomFileName());
            fileSystem.Move(startDirTempFile, newDirTempFile); 

            var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

            Assert.That(File.Exists(startDirTempFile), Is.False);
            Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
        }

        [Test]
        public void Move_should_move_a_directory_to_a_new_location()
        {
            var startDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var startDirTempFile = Path.Combine(startDir, Path.GetRandomFileName());
            File.WriteAllText(startDirTempFile, "start dir temp file");

            var subDir = CreateDirectory(Path.Combine(startDir, Path.GetRandomFileName()));
            var subDirTempFile = Path.Combine(subDir, Path.GetRandomFileName());
            File.WriteAllText(subDirTempFile, "sub dir temp file");

            var newDir = CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var newSubDir = AddPath(Path.Combine(newDir, Path.GetRandomFileName()));

            fileSystem.Move(startDir, newSubDir);

            var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

            var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
            Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
            var newSubDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(subDirTempFile));
            Assert.That(File.ReadAllText(newSubDirTempFile), Is.EqualTo("sub dir temp file"));
        }

        [Test]
        public void should_return_is_collection_property()
        {
            var tempPath = Path.GetTempPath();
            var newDir = Path.GetRandomFileName();
            var tempDir = CreateDirectory(Path.Combine(tempPath, newDir));

            var request = new PropfindRequest { HasResourceType = true, PathInfo = newDir };
            var result = fileSystem.Process(tempPath, request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo(newDir));
            Assert.That(response.Found.IsCollection, Is.True);
            Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
        }

        [Test]
        public void should_return_empty_resourcetype_when_not_dir()
        {
            var tempPath = AddPath(Path.GetTempFileName());
            var fileName = Path.GetFileName(tempPath);

            var request = new PropfindRequest { HasResourceType = true, PathInfo = "/" + tempPath};
            var result = fileSystem.Process(Path.GetTempPath(), request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + tempPath));
            Assert.That(response.Found.IsCollection, Is.False);
            Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
        }

        [Test]
        public void should_return_content_length_of_file()
        {
            var tempPath = AddPath(Path.GetTempFileName());
            var fileName = Path.GetFileName(tempPath);
            File.WriteAllBytes(tempPath, new byte[] { 23, 45, 45 });

            var request = new PropfindRequest { HasGetContentLength = true, PathInfo = "/" + fileName };
            var result = fileSystem.Process(Path.GetTempPath(), request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + fileName));
            Assert.That(response.Found.ContentLength, Is.EqualTo(3));
            Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
        }

        [Test]
        public void should_not_return_content_length_of_directory()
        {
            var tempDir = Path.GetTempPath();
            var newDir = Path.GetRandomFileName();
            var tempPath = CreateDirectory(Path.Combine(tempDir, newDir));

            var request = new PropfindRequest { HasGetContentLength = true, PathInfo = "/" + newDir};
            var result = fileSystem.Process(tempDir, request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + newDir));
            Assert.That(response.NotFound.Status, Is.EqualTo("HTTP/1.1 404 Not Found"));
            Assert.That(response.NotFound.Properties.Count, Is.EqualTo(1));

            var displayNameProperty = response.NotFound.Properties[0];
            Assert.That(displayNameProperty.Name.LocalName, Is.EqualTo("getcontentlength"));
            Assert.That(displayNameProperty.Name.Namespace, Is.EqualTo(Util.DavNamespace));
        }

        [Test]
        public void should_return_last_modified_of_file()
        {
            var tempPath = AddPath(Path.GetTempFileName());
            var fileName = Path.GetFileName(tempPath);
            var lastModified = File.GetLastWriteTimeUtc(tempPath);

            var request = new PropfindRequest { HasGetLastModified = true, PathInfo = "/" + fileName };
            var result = fileSystem.Process(Path.GetTempPath(), request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + fileName));
            Assert.That(response.Found.LastModified, Is.EqualTo(lastModified));
            Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
        }

        [Test]
        public void should_return_last_modified_of_directory()
        {
            var tempDir = Path.GetTempPath();
            var fileName = Path.GetRandomFileName();
            var tempPath = CreateDirectory(Path.Combine(tempDir, fileName));
            var lastModified = Directory.GetLastWriteTimeUtc(tempPath);

            var request = new PropfindRequest { HasGetLastModified = true, PathInfo = "/" + fileName };
            var result = fileSystem.Process(tempDir, request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + fileName));
            Assert.That(response.Found.LastModified, Is.EqualTo(lastModified));
            Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
        }

        [Test]
        public void should_return_unhandled_dav_properties_as_not_found()
        {
            var tempPath = AddPath(Path.GetTempFileName());
            var fileName = Path.GetFileName(tempPath);

            var request = new PropfindRequest { PathInfo = "/" + fileName, DavProperties = new List<string> { "displayname" } };
            var result = fileSystem.Process(Path.GetTempPath(), request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + fileName));
            Assert.That(response.Found.Properties, Is.Empty);
            Assert.That(response.NotFound.Status, Is.EqualTo("HTTP/1.1 404 Not Found"));
            Assert.That(response.NotFound.Properties.Count, Is.EqualTo(1));

            var displayNameProperty = response.NotFound.Properties[0];
            Assert.That(displayNameProperty.Name.LocalName, Is.EqualTo("displayname"));
            Assert.That(displayNameProperty.Name.Namespace, Is.EqualTo(Util.DavNamespace));
        }

        [Test]
        public void should_return_unhandled_properties_as_not_found()
        {
            var tempPath = AddPath(Path.GetTempFileName());
            var fileName = Path.GetFileName(tempPath);

            XNamespace nameSpace = "http://example.com/neon/litmus/";
            var element = new XElement(nameSpace + "foo", null);
            var request = new PropfindRequest { PathInfo = "/" + fileName, NonDavProperties = new List<XElement> { element } };
            var result = fileSystem.Process(Path.GetTempPath(), request);

            Assert.That(result.Responses.Count, Is.EqualTo(1));
            var response = result.Responses[0];
            Assert.That(response.Href, Is.EqualTo("/" + fileName));
            Assert.That(response.Found.Properties, Is.Empty);
            Assert.That(response.NotFound.Status, Is.EqualTo("HTTP/1.1 404 Not Found"));
            Assert.That(response.NotFound.Properties.Count, Is.EqualTo(1));

            var displayNameProperty = response.NotFound.Properties[0];
            Assert.That(displayNameProperty.Name.LocalName, Is.EqualTo("foo"));
            Assert.That(displayNameProperty.Name.Namespace, Is.EqualTo(nameSpace));
        }
    }
}
