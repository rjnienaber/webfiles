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
            return Path.Combine(basePath.Replace("/", "\\"), additionalPath.Replace("/", "\\")).TrimEnd('\\');
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


        public MultiStatusResult Process(string rootPath, string pathInfo, PropfindRequest request)
        {
            var fullPath = JoinPath(rootPath, pathInfo);
            var multiStatus = new MultiStatusResult();
            if (request.HasResourceTypeProperty)
            {
                var response = new Response { Href = pathInfo };
                response.Found.Status = "HTTP/1.1 200 Found";
                if (IsACollection(fullPath))
                    response.Found.AddCollectionProperty();
                multiStatus.Responses.Add(response);
            }

            return multiStatus;
        }
    }
}
