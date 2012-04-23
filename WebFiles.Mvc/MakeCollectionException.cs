using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebFiles.Mvc
{
    public class MakeCollectionException : Exception
    {
        public MakeCollectionException(Exception inner) : base("Couldn't create collection", inner) { }
    }
}
