using XRUIOS.Barebones;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRUIOS.Barebones.Functions
{
    public class FacadeClass 
    {
         
        public static readonly FacadeClass Instance = new();
        private FacadeClass() { }

    }
}
