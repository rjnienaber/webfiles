using System;
using System.IO;
using WebFiles.Mvc.Requests;
using WebFiles.Mvc.ActionResults;
namespace WebFiles.Mvc.Providers
{
    public interface IStorageProvider
    {
        bool CheckExists(string fullPath);
        string JoinPath(string basePath, string additionalPath);
        bool IsACollection(string fullPath);

        void CreateCollection(string fullPath);
        void Delete(string fullPath);
        void Save(string fullPath, Stream input);
        void Copy(string source, string destination);
        void Move(string source, string destination);

        MultiStatusResult Process(string rootPath, PropfindRequest request);
    }
}
