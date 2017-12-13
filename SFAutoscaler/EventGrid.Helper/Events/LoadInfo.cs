using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventGrid.Helper.Events
{
    public class LoadInfo
    {
        public IList<LoadMetricInformation> ClusterLoadInfo { get; set; }
    }
}
