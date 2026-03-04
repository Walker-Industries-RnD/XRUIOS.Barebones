using XRUIOS.Barebones;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRUIOS.Barebones.Functions
{
    public class FacadeClass : XRUIOSFunction
    {
        public override string FunctionName => "Facade";
        public static readonly FacadeClass Instance = new();
        private FacadeClass() { }

    }
}
