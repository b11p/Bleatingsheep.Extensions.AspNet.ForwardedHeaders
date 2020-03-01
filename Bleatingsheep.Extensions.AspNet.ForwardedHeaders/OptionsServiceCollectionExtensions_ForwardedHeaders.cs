using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Bleatingsheep.Extensions.AspNet.ForwardedHeaders
{
    public static class OptionsServiceCollectionExtensions_ForwardedHeaders
    {
        public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services, string hostName, int forwardLimit = 1)
        {
            return services.ConfigureForwardedHeaders(forwardLimit, hostName);
        }

        public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services, int forwardLimit, params string[] hostNames)
        {
            return services.Configure<ForwardedHeadersOptions>(options =>
            {
                foreach (var host in hostNames)
                {
                    options.AllowedHosts.Add(host);
                }

                // get global IPv6 prefixes. (default /64)
                var knownPrefixes = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetUnicastAddresses()
                    .Select(i => i.Address)
                    .Where(a => a?.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                        && !IPAddress.IsLoopback(a)
                        && !a.IsIPv6Teredo
                        && ((a.GetAddressBytes()[0] ^ 0b00100000) & 0b11100000) == 0) // is global unicast address.
                    .Select(localIPv6 =>
                    {
                        // get /64 prefixes
                        byte[] ipBytes = new byte[16];
                        Array.Copy(localIPv6.GetAddressBytes(), ipBytes, 8);
                        return new IPAddress(ipBytes);
                    }).Distinct();

                // add prefixes to known networks.
                foreach (IPAddress ip in knownPrefixes)
                {
                    options.KnownNetworks.Add(new IPNetwork(ip, 64));
                }

                options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("127.16.0.0"), 12)); // docker internal
                options.ForwardLimit = forwardLimit;
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
            });
        }
    }
}
