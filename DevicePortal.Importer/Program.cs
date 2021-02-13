using System;
using System.Linq;

namespace DevicePortal.Importer
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new DWHPMContext();
            var test = context.FnwiPortals.First();
        }
    }
}
