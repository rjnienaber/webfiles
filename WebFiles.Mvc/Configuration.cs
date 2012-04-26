using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebFiles.Mvc
{
    public class Configuration
    {
        public string RootPath { get; set; }
        public Encoding RequestEncoding { get; set; }

        public Configuration()
        {
            RequestEncoding = Encoding.UTF8;
        }
    }
}
