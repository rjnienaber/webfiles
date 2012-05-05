using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WebFiles.Mvc.Requests;
using WebFiles.Mvc.ActionResults;
using System.Web;

namespace WebFiles.Mvc.Providers
{
    public class FileSystemProvider : IStorageProvider
    {
        internal string basePath;
        internal FileSystemProvider() { }
        public FileSystemProvider(string basePath)
        {
            if (basePath == null)
                throw new ArgumentNullException("basePath");

            this.basePath = basePath.Replace("/", "\\").TrimEnd('\\');
        }

        internal string GetFullPath(string additionalPath)
        {
            return Path.Combine(basePath, additionalPath.Replace("/", "\\").Trim('\\'));
        }

        string JoinRelativePath(string basePath, string additionalPath)
        {
            if (basePath == "/")
                return string.Concat(basePath, additionalPath);
            return string.Concat("/", basePath.Trim('/'), "/", additionalPath.Trim('/'));
        }

        public virtual bool CheckExists(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            return Directory.Exists(fullPath) || File.Exists(fullPath);  
        }

        public virtual void CreateCollection(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception e)
            {
                throw new MakeCollectionException(e);
            }
        }

        public virtual void Delete(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            if (!CheckExists(fullPath)) return;

            if (IsACollection(fullPath))
                Directory.Delete(fullPath, true);
            else
                File.Delete(fullPath);
        }

        public virtual void Save(string relativePath, Stream input)
        {
            var fullPath = GetFullPath(relativePath);
            using (var file = File.OpenWrite(fullPath))
            {
                byte[] buffer = new byte[16 * 1024];
                int len;
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                    file.Write(buffer, 0, len);
            }
        }

        public void Copy(string relativeSourcePath, string relativeDestinationPath)
        {
            var source = GetFullPath(relativeSourcePath);
            var destination = GetFullPath(relativeDestinationPath);

            if (!IsACollection(source) && !IsACollection(destination))
            {
                File.Copy(source, destination, true);
                return;
            }

            var destinationDir = Path.Combine(destination, Path.GetFileName(source));
            Directory.CreateDirectory(destinationDir);

            foreach (var subDir in Directory.GetDirectories(source)) {
                var destinationSubDir = Path.Combine(destination, Path.GetFileName(subDir));
                Directory.CreateDirectory(destinationSubDir);
                Copy(subDir, destinationSubDir);
            }                                                          

            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        internal bool IsACollection(string fullPath)
        {
            return Directory.Exists(fullPath);
        }

        public void Move(string relativeSourcePath, string relativeDestinationPath)
        {
            var source = GetFullPath(relativeSourcePath);
            var destination = GetFullPath(relativeDestinationPath);

            if (!IsACollection(source))
            {
                File.Move(source, destination);
                return;
            }

            Directory.Move(source, destination);
        }


        public MultiStatusResult Process(PropfindRequest request)
        {
            var multiStatus = new MultiStatusResult();

            var relativePath = request.PathInfo.StartsWith("/") ? request.PathInfo : "/" + request.PathInfo;

            Process(request, relativePath, request.Depth, multiStatus);

            return multiStatus;
        }

        void Process(PropfindRequest request, string relativePath, int currentDepth, MultiStatusResult multiStatus)
        {
            var fullPath = GetFullPath(relativePath);

            var response = new Response { Href = relativePath };
            multiStatus.Responses.Add(response);
            var isACollection = IsACollection(fullPath);
            
            if (request.HasGetContentLength)
            {
                if (isACollection)
                    response.NotFound.AddProperty("getcontentlength", null);
                else
                    response.Found.AddContentLength(new FileInfo(fullPath).Length);
            }

            if (request.HasGetLastModified) {
                if (isACollection)
                    response.Found.AddLastModified(Directory.GetLastWriteTimeUtc(fullPath));
                else
                    response.Found.AddLastModified(File.GetLastWriteTimeUtc(fullPath));
            }

            if (request.HasResourceType)
                response.Found.AddResourceType(isACollection);

            if (response.Found.Properties.Any())
                response.Found.Status = "HTTP/1.1 200 OK";

            var supportedProperties = new[] { "getcontentlength", "getlastmodified", "resourcetype" };
            foreach (var prop in request.DavProperties.Where(p => !supportedProperties.Contains(p)))
                response.NotFound.AddProperty(prop, null);

            response.NotFound.Properties.AddRange(request.NonDavProperties);

            if (response.NotFound.Properties.Any())
                response.NotFound.Status = "HTTP/1.1 404 Not Found";

            if (currentDepth == 0)
                return;

            if (isACollection)
            {
                //process files
                var files = Directory.GetFiles(fullPath);
                foreach (var file in files)
                    Process(request, JoinRelativePath(relativePath, Path.GetFileName(file)), 0, multiStatus);

                //process directories
                var dirs = Directory.GetDirectories(fullPath);
                foreach (var dir in dirs)
                    Process(request, JoinRelativePath(relativePath, Path.GetFileName(dir)) + "/", currentDepth - 1, multiStatus);
            }
        }

        public Stream Read(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            if (File.Exists(fullPath))
                return File.OpenRead(fullPath);

            throw new Exception("File path not found");
        }
    }
}
