﻿namespace DCCCommon.Conventions
{
    using System.Xml.Linq;

    public static class Common
    {
        #region Client

        public const string Client = "client";

        #endregion

        #region Discovery

        public const string Discovery = "discovery";
        public const string ResponseTcpPort = "response-tcpPort";
        public const string MulticastIpAddress = "multicast-ipAddress";
        public const string MulticastPort = "multicast-port";

        #endregion

        #region Proxy

        public const string Proxy = "proxy";

        #endregion

        #region Common

        public const string LocalIpAddress = "local-ipAddress";
        public const string RemotePort = "remote-tcpPort";
        public const string RemoteIpAddress = "remote-ipAddress";

        #endregion

        #region Node

        public const string Id = "id";
        public const string Node = "node";
        public const string TcpServingPort = "tcpserving-port";
        public const string AdjacentNodes = "adjacent-nodes";
        public const string RemoteNode = "remote-node";

        #endregion
    }
}