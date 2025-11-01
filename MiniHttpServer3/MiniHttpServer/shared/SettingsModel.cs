using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.shared
{
    public class SettingsModel
    {
        public string PublicDirectoryPath { get; set; }
        public string Domain {  get; set; }
        public int Port { get; set; }
    }
}
