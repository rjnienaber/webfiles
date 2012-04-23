using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebFiles.Mvc;
using WebFiles.Mvc.Providers;

namespace WebFiles.Web.Tests.Controllers
{
    public class WebDavController : WebFilesController 
    {
        public WebDavController()
        {
            this.config = new Configuration { RootPath = "D:\\stuff" };
            this.storageProvider = new FileSystemProvider();
        }
    }
}
