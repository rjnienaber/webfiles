using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebFiles.Mvc;
using System.IO;
using WebFiles.Mvc.Providers;
using WebFiles.Mvc.ActionResults;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Xml.Linq;
using WebFiles.Mvc.Requests;

namespace WebFiles.Mvc
{
    public class WebFilesController : Controller
    {
        public const string ActionName = "All";
        protected readonly IStorageProvider storageProvider;
        protected readonly Encoding requestEncoding;

        public WebFilesController() : this(null, null) { }
        public WebFilesController(IStorageProvider storageProvider) : this(Encoding.UTF8, storageProvider) { }
        public WebFilesController(Encoding requestEncoding, IStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
            this.requestEncoding = requestEncoding; 
        }

        [AcceptVerbs("PROPFIND")]
        [ActionName(ActionName)]
        public ActionResult Propfind(string pathInfo)
        {
            pathInfo = EnsureStartSlash(pathInfo);
            var depth = Request.Headers["Depth"];
            try
            {
                if (Request.ContentLength > 0)
                {
                    using (var streamReader = new StreamReader(Request.InputStream, requestEncoding))
                        return Propfind(new PropfindRequest(pathInfo, depth, XDocument.Load(streamReader)));
                }
                else
                    return Propfind(new PropfindRequest(pathInfo, depth));
            }
            catch (XmlException e)
            {
                throw new HttpException(400, "malformed xml request body", e);
            }
        }

        public virtual MultiStatusResult Propfind(PropfindRequest request)
        {
            if (!storageProvider.CheckExists(request.PathInfo))
                throw new HttpException(404, "path doesn't exist");

            var result = storageProvider.Process(request);
            foreach (var response in result.Responses)
                if (response.Href == "/")
                    response.Href = Request.Url.LocalPath;
                else if (request.PathInfo == "/")
                    response.Href = Request.Url.LocalPath.TrimEnd('/') + EnsureStartSlash(response.Href);
                else
                    response.Href = Request.Url.LocalPath.Replace(request.PathInfo, EnsureStartSlash(response.Href));

            return result;
        }

        [AcceptVerbs("PROPPATCH")]
        [ActionName(ActionName)]
        public ActionResult Proppatch(string pathInfo)
        {
            return Proppatch(pathInfo, null);
        }


        public virtual ActionResult Proppatch(string pathInfo, string test)
        {
            return new MultiStatusResult();
        }


        [AcceptVerbs("COPY")]
        [ActionName(ActionName)]
        public ActionResult Copy(string pathInfo)
        {
            return Copy(EnsureStartSlash(pathInfo), GetDestination(pathInfo), GetOverwrite());
        }

        public virtual ActionResult Copy(string relativeSource, string relativeDestination, bool mustOverwrite)
        {
            var destinationExists = CopyMoveOperation(relativeSource, relativeDestination, mustOverwrite, true); 
            return new NoContentResult(destinationExists ? 204 : 201);
        }

        

        [AcceptVerbs("MOVE")]
        [ActionName(ActionName)]
        public virtual ActionResult Move(string pathInfo)
        {
            return Move(EnsureStartSlash(pathInfo), GetDestination(pathInfo), GetOverwrite());
        }

        public virtual ActionResult Move(string relativeSource, string relativeDestination, bool mustOverwrite)
        {
            var destinationExists = CopyMoveOperation(relativeSource, relativeDestination, mustOverwrite, false); 
            return new NoContentResult(destinationExists ? 204 : 201);
        }

        bool CopyMoveOperation(string relativeSource, string relativeDestination, bool mustOverwrite, bool performCopy)
        {
            CheckAncestorPaths(relativeDestination);
            var destinationExists = storageProvider.CheckExists(relativeDestination);

            if (!mustOverwrite && destinationExists)
                throw new HttpException(412, "path already exists");

            if (destinationExists)
                storageProvider.Delete(relativeDestination);

            if (performCopy)
                storageProvider.Copy(relativeSource, relativeDestination);
            else
                storageProvider.Move(relativeSource, relativeDestination);

            return destinationExists;
        }

        [HttpGet]
        [ActionName(ActionName)]
        public virtual ActionResult Get(string pathInfo)
        {
            var stream = storageProvider.Read(EnsureStartSlash(pathInfo));
            return File(stream, "application/octet-stream");
        }

        [HttpPut]
        [ActionName(ActionName)]
        public virtual NoContentResult Put(string pathInfo)
        {
            storageProvider.Save(pathInfo, Request.InputStream);
            return new NoContentResult(201);
        }

        [HttpDelete]
        [ActionName(ActionName)]
        public virtual NoContentResult Delete(string pathInfo)
        {
            var relativePath = EnsureStartSlash(pathInfo);
            if (!storageProvider.CheckExists(relativePath))
                throw new HttpException(404, "path doesn't exist");

            storageProvider.Delete(relativePath);
            return new NoContentResult(204);
        }

        [AcceptVerbs("MKCOL")]
        [ActionName(ActionName)]
        public virtual ActionResult MakeCollection(string pathInfo)
        {
            if (Request.ContentLength != 0)
                throw new HttpException(415, "request body not understood");

            if (!storageProvider.CheckExists("/"))
                throw new HttpException(409, "parent path doesn't exist");

            CheckAncestorPaths(pathInfo);

            var destination = EnsureStartSlash(pathInfo);
            if (storageProvider.CheckExists(destination))
                throw new HttpException(405, "path already exists");

            storageProvider.CreateCollection(destination);
            return new NoContentResult(201);
        }
        
        [AcceptVerbs("OPTIONS")]
        [ActionName(ActionName)]
        public virtual NoContentResult Options(string pathInfo)
        {
            var result = new NoContentResult(200);
            result.Headers.Add("Allow", "OPTIONS, DELETE, MKCOL, PUT, GET, PROPFIND, COPY, MOVE");
            result.Headers.Add("DAV", "1, 2");
            return result;
        }

        private void CheckAncestorPaths(string pathInfo)
        {
            var parts = pathInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            //ensure ancestors exist
            string fullPath = null;
            if (parts.Length > 0)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    fullPath = string.Concat(fullPath, "/", part);
                    if (!storageProvider.CheckExists(fullPath))
                    {
                        var errorMessage = string.Format("ancestor path doesn't exist - /{0}", string.Join("/", parts, 0, i + 1));
                        throw new HttpException(409, errorMessage);
                    }
                }
            }
        }

        string EnsureStartSlash(string relativePath)
        {
            return "/" + (relativePath ?? "").TrimStart('/');
        }

        string GetDestination(string pathInfo)
        {
            var unencodedUri = HttpUtility.UrlDecode(Request.Url.AbsoluteUri);
            var relativeDestinationUrl = Request.Headers["Destination"].Replace(unencodedUri.Replace(pathInfo, ""), "");
            return EnsureStartSlash(relativeDestinationUrl);
        }

        bool GetOverwrite()
        {
            return !(Request.Headers.AllKeys.Contains("Overwrite") && Request.Headers["Overwrite"] == "F");
        }
    }
}
