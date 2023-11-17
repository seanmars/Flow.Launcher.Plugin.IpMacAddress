using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.IpMacAddress
{
    public class IpMacAddress : IAsyncPlugin, IResultUpdated
    {
        private const string IconInside = "inside.png";
        private const string IconOutside = "outside.png";

        private PluginInitContext _context;
        public event ResultUpdatedEventHandler ResultsUpdated;

        private static Func<ActionContext, bool> Action(string text)
        {
            return e =>
            {
                CopyToClipboard(text);

                return true;
            };
        }

        private static void CopyToClipboard(string text)
        {
            Clipboard.SetDataObject(text);
        }

        /// <summary>
        /// Initialize plugin
        /// </summary>
        /// <param name="context"></param>
        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Query for ip and mac address
        /// </summary>
        /// <param name="query"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            var results = new List<Result>();

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                {
                    continue;
                }

                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (nic.GetPhysicalAddress().GetAddressBytes().Length == 0)
                {
                    continue;
                }

                var macAdr = string.Join("-",
                        nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("x2")))
                    .ToUpper();

                var ipList = nic.GetIPProperties().UnicastAddresses
                    .OrderBy(adr => adr.Address.AddressFamily);

                foreach (var ip in ipList)
                {
                    if (ip.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork &&
                        ip.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        continue;
                    }

                    var ipAdr = ip.Address.ToString();

                    results.Add(new Result
                    {
                        Title = ipAdr,
                        SubTitle = $"MAC: {macAdr}",
                        IcoPath = IconInside,
                        Action = Action(ipAdr)
                    });
                }
            }

            ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
            {
                Query = query,
                Results = results
            });

            using HttpClient client = new();
            var externalIp = await client.GetStringAsync("https://api.ipify.org", token);
            results.Add(new Result
            {
                Title = externalIp,
                SubTitle = "External IP Address",
                IcoPath = IconOutside,
                Action = Action(externalIp)
            });

            return results;
        }
    }
}