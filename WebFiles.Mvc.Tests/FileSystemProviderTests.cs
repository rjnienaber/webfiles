using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using WebFiles.Mvc.Providers;
using WebFiles.Mvc.Requests;

namespace WebFiles.Mvc.Tests
{
    [TestFixture]
    public class FileSystemProviderTests
    {
        FileSystemProvider fileSystem = new FileSystemProvider();
        [Test]
        public void Normalize_urls()
        {
            Assert.That(fileSystem.JoinPath("d:\\stuff", "litmus/"), Is.EqualTo("d:\\stuff\\litmus"));
            Assert.That(fileSystem.JoinPath("d:\\stuff", "litmus/another"), Is.EqualTo("d:\\stuff\\litmus\\another"));
        }

        [Test]
        public void Delete_of_a_file_off_disk()
        {
            var file = Path.GetTempFileName();
            Assert.That(File.Exists(file), Is.True);

            try
            {
                fileSystem.Delete(file);
                Assert.That(File.Exists(file), Is.False);
            }
            finally
            {
                EnsureFileRemoved(file);
            }
        }

        [Test]
        public void Delete_a_directory_recurisively_off_disk()
        {
            var newTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(newTempPath);
            Assert.That(Directory.Exists(newTempPath), Is.True);
            try
            {
                var newTempPath2 = Path.Combine(newTempPath, Path.GetRandomFileName());
                Directory.CreateDirectory(newTempPath2);
                Assert.That(Directory.Exists(newTempPath2), Is.True);

                fileSystem.Delete(newTempPath);
                Assert.That(Directory.Exists(newTempPath2), Is.False);
                Assert.That(Directory.Exists(newTempPath), Is.False);
            }
            finally
            {
                EnsureDirectoryRemoved(newTempPath);
            }
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

            var fileName = Path.GetTempFileName();
            fileSystem.Save(fileName, ms);
            try
            {
                var text = File.ReadAllText(fileName);
                Assert.That(text, Is.EqualTo("save this resource"));
            }
            finally
            {
                EnsureFileRemoved(fileName);
            }
        }

        [Test]
        public void Copy_resource_to_a_new_destination()
        {
            string tempFileSource = null;
            string tempFileDestination = null;
            try
            {
                tempFileSource = Path.GetTempFileName();
                File.WriteAllText(tempFileSource, "copy resource to new location");

                tempFileDestination = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Assert.That(File.Exists(tempFileDestination), Is.False);
                fileSystem.Copy(tempFileSource, tempFileDestination);
                Assert.That(File.Exists(tempFileDestination), Is.True);

                Assert.That(File.ReadAllText(tempFileDestination), Is.EqualTo("copy resource to new location"));
            }
            finally
            {
                EnsureFileRemoved(tempFileSource);
                EnsureFileRemoved(tempFileDestination);
            }
        }

        [Test]
        public void Copy_resource_to_a_new_destination_should_overwrite_by_default()
        {
            string tempFileSource = null;
            string tempFileDestination = null;
            try
            {
                tempFileSource = Path.GetTempFileName();
                File.WriteAllText(tempFileSource, "copy resource to new location");

                tempFileDestination = Path.GetTempFileName();
                Assert.That(File.Exists(tempFileDestination), Is.True);
                fileSystem.Copy(tempFileSource, tempFileDestination);
                Assert.That(File.Exists(tempFileDestination), Is.True);

                Assert.That(File.ReadAllText(tempFileDestination), Is.EqualTo("copy resource to new location"));
            }
            finally
            {
                EnsureFileRemoved(tempFileSource);
                EnsureFileRemoved(tempFileDestination);
            }
        }

        [Test]
        public void Determines_whether_a_path_is_a_directory_or_not()
        {
            string tempFile = null;
            var newTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(newTempPath);
            Assert.That(Directory.Exists(newTempPath), Is.True);
            try
            {
                Assert.That(fileSystem.IsACollection(newTempPath), Is.True);

                tempFile = Path.GetTempFileName();
                Assert.That(fileSystem.IsACollection(tempFile), Is.False);

            }
            finally
            {
                EnsureDirectoryRemoved(newTempPath);
                EnsureFileRemoved(tempFile);
            }
        }

        [Test]
        public void Copy_should_create_the_directory_its_copying_to()
        {
            string startDir = null;
            string startDirTempFile = null;
            string newDir = null;
            try
            {
                startDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(startDir);
                startDirTempFile = Path.Combine(startDir, Path.GetRandomFileName());
                File.WriteAllText(startDirTempFile, "start dir temp file");

                newDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                fileSystem.Copy(startDir, newDir);

                var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

                var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
                Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
            }
            finally
            {
                EnsureDirectoryRemoved(startDir);
                EnsureDirectoryRemoved(newDir);
            }
        }

        [Test]
        public void Copy_recursively_including_directories()
        {
            string startDir = null;
            string startDirTempFile = null;
            string subDir = null;
            string subDirTempFile = null;
            string newDir = null;
            try
            {
                startDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(startDir);
                startDirTempFile = Path.Combine(startDir, Path.GetRandomFileName());
                File.WriteAllText(startDirTempFile, "start dir temp file");

                subDir = Path.Combine(startDir, Path.GetRandomFileName());
                Directory.CreateDirectory(subDir);
                subDirTempFile = Path.Combine(subDir, Path.GetRandomFileName());
                File.WriteAllText(subDirTempFile, "sub dir temp file");

                newDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                fileSystem.Copy(startDir, newDir);

                var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

                var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
                Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
                var newSubDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(subDirTempFile));
                Assert.That(File.ReadAllText(newSubDirTempFile), Is.EqualTo("sub dir temp file"));
            }
            finally
            {
                EnsureDirectoryRemoved(startDir);
                EnsureDirectoryRemoved(newDir);
            }
        }

        [Test]
        public void Move_should_move_a_file_to_new_location()
        {
            string startDir = null;
            string startDirTempFile = null;
            string newDir = null;
            try
            {
                startDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(startDir);
                startDirTempFile = Path.Combine(startDir, Path.GetRandomFileName());
                File.WriteAllText(startDirTempFile, "start dir temp file");

                newDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(newDir);
                var newDirTempFile = Path.Combine(newDir, Path.GetRandomFileName());
                fileSystem.Move(startDirTempFile, newDirTempFile); 

                var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

                Assert.That(File.Exists(startDirTempFile), Is.False);
                Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
            }
            finally
            {
                EnsureDirectoryRemoved(startDir);
                EnsureDirectoryRemoved(newDir);
            }
        }

        [Test]
        public void Move_should_move_a_directory_to_a_new_location()
        {
            string startDir = null;
            string newDir = null;
            try
            {
                startDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(startDir);
                var startDirTempFile = Path.Combine(startDir, Path.GetRandomFileName());
                File.WriteAllText(startDirTempFile, "start dir temp file");

                var subDir = Path.Combine(startDir, Path.GetRandomFileName());
                Directory.CreateDirectory(subDir);
                var subDirTempFile = Path.Combine(subDir, Path.GetRandomFileName());
                File.WriteAllText(subDirTempFile, "sub dir temp file");

                newDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(newDir);
                var newSubDir = Path.Combine(newDir, Path.GetRandomFileName());

                fileSystem.Move(startDir, newSubDir);

                var files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories);

                var newDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(startDirTempFile));
                Assert.That(File.ReadAllText(newDirTempFile), Is.EqualTo("start dir temp file"));
                var newSubDirTempFile = files.First(f => Path.GetFileName(f) == Path.GetFileName(subDirTempFile));
                Assert.That(File.ReadAllText(newSubDirTempFile), Is.EqualTo("sub dir temp file"));
            }
            finally
            {
                EnsureDirectoryRemoved(startDir);
                EnsureDirectoryRemoved(newDir);
            }
        }


        [Test]
        public void should_return_is_collection_property()
        {
            string tempDir = null;
            try
            {
                var tempPath = Path.GetTempPath();
                var newDir = Path.GetRandomFileName();
                tempDir = Path.Combine(tempPath, newDir);
                Directory.CreateDirectory(tempDir);

                var request = new PropfindRequest { HasResourceTypeProperty = true };
                var result = fileSystem.Process(tempPath, newDir, request);

                Assert.That(result.Responses.Count, Is.EqualTo(1));
                var response = result.Responses[0];
                Assert.That(response.Href, Is.EqualTo(newDir));
                Assert.That(response.Found.IsCollection, Is.True);
                Assert.That(response.Found.Status, Is.EqualTo("HTTP/1.1 200 Found"));
            }
            finally
            {
                EnsureDirectoryRemoved(tempDir);
            }
        }

        void EnsureFileRemoved(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        void EnsureDirectoryRemoved(string filePath)
        {
            if (Directory.Exists(filePath))
                Directory.Delete(filePath, true);
        }
    }
}
