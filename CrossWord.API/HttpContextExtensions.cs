using System.Net;

namespace CrossWord.API
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Get remote ip address, optionally allowing for x-forwarded-for header check
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="allowForwarded">Whether to allow x-forwarded-for header check</param>
        /// <returns>IPAddress</returns>
        public static IPAddress GetRemoteIPAddress(this HttpContext context, bool allowForwarded = true)
        {
            if (allowForwarded)
            {
                // CF-Connecting-IP is used by CloudFlare
                // X-Forwarded-For is used by proxies to forward the real ip
                string header = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (IPAddress.TryParse(header, out IPAddress ip))
                {
                    return ip;
                }
            }
            return context.Connection.RemoteIpAddress;
        }
    }
}