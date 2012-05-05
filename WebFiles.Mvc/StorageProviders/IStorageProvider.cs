using System;
using System.IO;
using WebFiles.Mvc.Requests;
using WebFiles.Mvc.ActionResults;
namespace WebFiles.Mvc.Providers
{
    public interface IStorageProvider
    {
        bool CheckExists(string relativePath);
        void CreateCollection(string relativePath);
        void Save(string relativePath, Stream input);
        Stream Read(string relativePath);
        void Copy(string sourceRelativePath, string destinationRelativePath);
        void Move(string sourceRelativePath, string destinationRelativePath);
        void Delete(string relativePath);

        MultiStatusResult Process(PropfindRequest request);
    }
}
