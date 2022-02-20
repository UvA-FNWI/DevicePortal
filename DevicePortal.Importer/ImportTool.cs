using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevicePortal.Importer
{
    internal class ImportTool
    {
        private readonly PortalContext _context;

        public ImportTool(IConfiguration config)
        {
            string connectionString = config.GetConnectionString("DefaultConnection");
            var options = new DbContextOptionsBuilder<PortalContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            _context = new PortalContext(options);
        }

        internal void Run()
        {
            var data = @"<get this from somewhere>";

            (string Mac, string Desc)[] macs = data.Split('\n').Select(l => l.Split(':')).Select(l => (l[0], l[1])).ToArray();

            foreach (var mac in macs)
            {
                var devices = _context.Devices.Where(d => d.Macadres == mac.Mac);
                if (devices.Count() != 1)
                    Console.WriteLine($"Error! Device count for {mac.Mac}:{mac.Desc} is {devices.Count()}");
                //Console.WriteLine($"{device.DeviceId} ({device.Category.ToString()}):{mac.Desc}");
                foreach (var device in devices)
                {
                    device.Category = DeviceCategory.PackageControl;
                    _context.Update(device);
                }
            }
            _context.SaveChanges();
        }
    }
}
