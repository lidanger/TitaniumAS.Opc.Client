using System;

namespace TitaniumAS.Opc.Client.Common
{
    /// <summary>
    /// Represents an OPC server URL parser.
    /// </summary>
    public static class UrlParser
    {
        public delegate void WithProgId(string host, string progIdOrClsid);
        public delegate void WithCLSID(string host, Guid clsid);

        /// <summary>
        /// Parses an OPC DA server URL.
        /// </summary>
        /// <param name="opcServerUrl">The OPC DA server URL.</param>
        /// <param name="withProgId">An action (host, progId) called when the URL contains programmatic identifier.</param>
        /// <param name="withCLSID">An action (host, clsid) called when the URL contains class identifier.</param>
        public static void Parse(Uri opcServerUrl, WithProgId withProgId, WithCLSID withCLSID)
        {
            string segment = opcServerUrl.Segments[1];
            string progIdOrClsid = segment.TrimEnd('/'); //workaround for split ProgId & ItemId in URL path
            try
            {
                Guid clsid = new Guid(progIdOrClsid);

                withCLSID(opcServerUrl.Host, clsid);
            }
            catch
            {
                withProgId(opcServerUrl.Host, progIdOrClsid);
            }
        }

        public delegate T WithProgIdFunc<T>(string host, string progIdOrClsid);
        public delegate T WithCLSID<T>(string host, Guid clsid);

        /// <summary>
        /// Parses an OPC DA server URL.
        /// </summary>
        /// <typeparam name="T">The type of returned value.</typeparam>
        /// <param name="opcServerUrl">The OPC DA server URL.</param>
        /// <param name="withProgId">A function (host, progId) called when the URL contains programmatic identifier.</param>
        /// <param name="withCLSID">A function (host, clsid) called when the URL contains class identifier.</param>
        /// <returns>
        /// The returned value of one called function.
        /// </returns>
        public static T Parse<T>(Uri opcServerUrl, WithProgIdFunc<T> withProgId, WithCLSID<T> withCLSID)
        {
            string sergment = opcServerUrl.Segments[1];
            string progIdOrClsid = sergment.TrimEnd('/'); //workaround for split ProgId & ItemId in URL path
            try
            {
                Guid clsid = new Guid(progIdOrClsid);

                return withCLSID(opcServerUrl.Host, clsid);
            }
            catch
            {
                return withProgId(opcServerUrl.Host, progIdOrClsid);
            }
        }
    }
}