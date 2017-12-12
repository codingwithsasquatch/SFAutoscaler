using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventGrid.Helper;
using EventGrid.Helper.Events;
using System.Fabric.Query;

namespace EventGridTest
{
    class Program
    {
        private const string TopicEndpoint = "[place endpoint here]";
        private const string TopicKey = "[place key here]";

        static void Main(string[] args)
        {
            SendLoadInfo().Wait();
            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();
        }

        private static async Task SendLoadInfo()
        {
            var errors = new List<GridEvent<LoadInfo>>
            {
                new GridEvent<LoadInfo>()
                {
                    Data =
                        new LoadInfo()
                        {
                            ClusterLoadInfo = new Dictionary<string, LoadMetricInformation>()
                        },
                    Subject = "clusterevent/loadinfo",
                    EventType = "loadinfo",
                    EventTime = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString()
                }
            };

            await Utils.SendEvent(TopicEndpoint, TopicKey, errors);
        }
    }
}
