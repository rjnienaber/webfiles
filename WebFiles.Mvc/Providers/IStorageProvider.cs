using System;
using System.IO;
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
    }
}
