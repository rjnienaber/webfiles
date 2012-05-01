using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WebFiles.Mvc.Requests;
using WebFiles.Mvc.ActionResults;

namespace WebFiles.Mvc.Providers
{
    public class FileSystemProvider : IStorageProvider
    {
        public virtual string JoinPath(string basePath, string additionalPath)
        {
            return Path.Combine(basePath.Replace("/", "\\").TrimEnd('\\'), 
                                additionalPath.Replace("/", "\\").Trim('\\'));
        }

        public virtual bool CheckExists(string fullPath)
        {
            return Directory.Exists(fullPath) || File.Exists(fullPath);  
        }

        public virtual void CreateCollection(string fullPath)
        {
            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception e)
            {
                throw new MakeCollectionException(e);
            }
        }

        public virtual void Delete(string fullPath)
        {
            if (!CheckExists(fullPath)) return;

            if (IsACollection(fullPath))
                Directory.Delete(fullPath, true);
            else
                File.Delete(fullPath);
        }

        public virtual void Save(string fullPath, Stream input)
        {
            using (var file = File.OpenWrite(fullPath))
            {
                byte[] buffer = new byte[16 * 1024];
                int len;
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                    file.Write(buffer, 0, len);
            }
        }

        public void Copy(string source, string destination)
        {
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

        public bool IsACollection(string fullPath)
        {
            return Directory.Exists(fullPath);
        }

        public void Move(string source, string destination)
        {
            if (!IsACollection(source))
            {
                File.Move(source, destination);
                return;
            }

            Directory.Move(source, destination);
        }


        public MultiStatusResult Process(string rootPath, PropfindRequest request)
        {
            var fullPath = JoinPath(rootPath, request.PathInfo);
            var multiStatus = new MultiStatusResult();
            
            var response = new Response { Href = request.PathInfo};
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

            return multiStatus;
        }
    }
}
