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
        protected IStorageProvider storageProvider = null;
        protected Configuration config = null;

        public WebFilesController() : this(null, null) { }
        public WebFilesController(Configuration config, IStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
            this.config = config;
        }

        [AcceptVerbs("PROPFIND")]
        [ActionName(ActionName)]
        public ActionResult Propfind(string pathInfo)
        {
            try
            {
                using (var streamReader = new StreamReader(Request.InputStream, config.RequestEncoding))
                    return Propfind(pathInfo, new PropfindRequest(XDocument.Load(streamReader)));
            }
            catch (XmlException e)
            {
                throw new HttpException(400, "malformed xml request body", e);
            }
        }

        public virtual ActionResult Propfind(string pathInfo, PropfindRequest request)
        {
            var fullPath = storageProvider.JoinPath(config.RootPath, pathInfo);
            if (!storageProvider.CheckExists(fullPath))
                throw new HttpException(404, "path doesn't exist");

            return storageProvider.Process(config.RootPath, pathInfo, request);
        }

        [AcceptVerbs("COPY")]
        [ActionName(ActionName)]
        public virtual ActionResult Copy(string pathInfo)
        {
            var relativeDestinationUrl = Request.Headers["Destination"].Replace(Request.Url.AbsoluteUri.Replace(pathInfo, ""), "");
            var destination = CheckAncestorPaths(relativeDestinationUrl);

            var destinationExists = storageProvider.CheckExists(destination);

            if (Request.Headers.AllKeys.Contains("Overwrite") &&
                Request.Headers["Overwrite"] == "F" &&
                destinationExists)
                throw new HttpException(412, "path already exists");

            if (destinationExists)
                storageProvider.Delete(destination);

            var source = storageProvider.JoinPath(config.RootPath, pathInfo);

            storageProvider.Copy(source, destination);
            return new NoContentResult(destinationExists ? 204 : 201);
        }

        [AcceptVerbs("MOVE")]
        [ActionName(ActionName)]
        public virtual NoContentResult Move(string pathInfo)
        {
            var relativeDestinationUrl = Request.Headers["Destination"].Replace(Request.Url.AbsoluteUri.Replace(pathInfo, ""), "");
            var destination = CheckAncestorPaths(relativeDestinationUrl);

            var destinationExists = storageProvider.CheckExists(destination);

            if (Request.Headers.AllKeys.Contains("Overwrite") &&
                Request.Headers["Overwrite"] == "F" &&
                destinationExists)
                throw new HttpException(412, "path already exists");

            if (destinationExists)
                storageProvider.Delete(destination);

            var source = storageProvider.JoinPath(config.RootPath, pathInfo);

            storageProvider.Move(source, destination);
            return new NoContentResult(destinationExists ? 204 : 201);
        }

        [HttpGet]
        [ActionName(ActionName)]
        public virtual ActionResult Get(string pathInfo)
        {
            var fullPath = storageProvider.JoinPath(config.RootPath, pathInfo);
            return File(fullPath, "application/octet-stream");
        }

        [HttpPut]
        [ActionName(ActionName)]
        public virtual NoContentResult Put(string pathInfo)
        {
            var fullPath = storageProvider.JoinPath(config.RootPath, pathInfo);
            storageProvider.Save(fullPath, Request.InputStream);
            return new NoContentResult(201);
        }

        [HttpDelete]
        [ActionName(ActionName)]
        public virtual NoContentResult Delete(string pathInfo)
        {
            var fullPath = storageProvider.JoinPath(config.RootPath, pathInfo);
            if (!storageProvider.CheckExists(fullPath))
                throw new HttpException(404, "path doesn't exist");

            storageProvider.Delete(fullPath);
            return new NoContentResult(204);
        }

        [AcceptVerbs("MKCOL")]
        [ActionName(ActionName)]
        public virtual ActionResult MakeCollection(string pathInfo)
        {
            if (Request.ContentLength != 0)
                throw new HttpException(415, "request body not understood");

            if (!storageProvider.CheckExists(config.RootPath))
                throw new HttpException(409, "parent path doesn't exist");

            var fullPath = CheckAncestorPaths(pathInfo);

            if (storageProvider.CheckExists(fullPath))
                throw new HttpException(405, "path already exists");

            storageProvider.CreateCollection(fullPath);
            return new NoContentResult(201);
        }

        private string CheckAncestorPaths(string pathInfo)
        {
            var parts = pathInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            //ensure ancestors exist
            var fullPath = config.RootPath;
            if (parts.Length > 0)
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    fullPath = storageProvider.JoinPath(fullPath, part);
                    if (!storageProvider.CheckExists(fullPath))
                    {
                        var errorMessage = string.Format("ancestor path doesn't exist - /{0}", string.Join("/", parts, 0, i + 1));
                        throw new HttpException(409, errorMessage);
                    }
                }
            }

            return storageProvider.JoinPath(fullPath, parts.Last());
        }
        
        [AcceptVerbs("OPTIONS")]
        [ActionName(ActionName)]
        public virtual NoContentResult Options(string pathInfo)
        {
            var result = new NoContentResult(200);
            result.Headers.Add("Allow", "OPTIONS, DELETE, MKCOL, PUT, GET, PROPFIND, PROPPATCH, COPY, MOVE");
            result.Headers.Add("DAV", "1, 2");
            return result;
        }
    }
}
