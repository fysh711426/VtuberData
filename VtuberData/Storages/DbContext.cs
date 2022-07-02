using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VtuberData.Models;

namespace VtuberData.Storages
{
    public class DbContext
    {
        public Storage<string, Vtuber> Vtubers { get; set; } = null!;
        public Storage<string, Data> Datas { get; set; } = null!;
    }
}
