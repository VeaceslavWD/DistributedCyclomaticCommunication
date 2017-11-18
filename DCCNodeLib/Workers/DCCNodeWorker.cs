﻿namespace DCCNodeLib.Workers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Agents;
    using DCCCommon;
    using DCCCommon.Agents;
    using DCCCommon.Comparers;
    using DCCCommon.Conventions;
    using DCCCommon.Entities;
    using DCCCommon.Messages;
    using DSL;
    using EasySharp.NHelpers.CustomExMethods;
    using Interfaces;

    public class DCCNodeWorker : IDCCNodeWorker
    {
        private IPAddress _localIpAddress;
        public int CurrentNodeId { get; set; }

        public string DataSourcePath { get; set; }

        //public IPAddress LocalIpAddress { get; set; }
        public IPEndPoint MulticastIPEndPoint { get; set; }

        public int TcpServingPort { get; set; }
        //public IEnumerable<IPEndPoint> AdjacentNodesEndPoints { get; set; }

        public void Start()
        {
            Console.Out.WriteLine($"Node with id [ {CurrentNodeId} ] is activated.");

            new Thread(StartListeningToMulticastPort).Start();
            new Thread(StartListeningToTcpServingPort).Start();


            //Task tcpListenerTask = Task.Run(StartListeningToTcpServingPortAsync);
            //Task.WaitAll(multicastListenerTask, tcpListenerTask);
        }

        public void Init(int nodeId)
        {
            CurrentNodeId = nodeId;

            string nodeDataSourcePath = StartupConfigManager.Default
                .GetNodeDataSourcePath(nodeId);

            if (string.IsNullOrWhiteSpace(nodeDataSourcePath))
            {
                Console.Out.WriteLine("Data Source path hadn't been found in the configuration file.");
                Environment.Exit(1);
            }

            //IPAddress localIpAddress = StartupConfigManager.Default
            //    .GetNodeLocalIpAddress(nodeId);

            //if (localIpAddress == null)
            //{
            //    Console.Out.WriteLine("Local IP Address is not found in the configuration file.");
            //    Environment.Exit(1);
            //}

            _localIpAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();


            IPEndPoint multicastIpEndPoint = StartupConfigManager.Default
                .GetNodeMulticastIPEndPoint(nodeId);

            if (multicastIpEndPoint == null)
            {
                Console.Out.WriteLine("Multicast IP Address and port are not found in the configuration file.");
                Environment.Exit(1);
            }

            int tcpServingPort = StartupConfigManager.Default
                .GetNodeTcpServingPort(nodeId);

            if (tcpServingPort == -1)
            {
                Console.Out.WriteLine("TCP serving port is not found in the configuration file.");
                Environment.Exit(1);
            }

            IEnumerable<(int, IPEndPoint)> adjacentNodesEndPointsWithIDs = StartupConfigManager.Default
                .GetAdjacentNodesEndPointsWithIDs(nodeId);

            DataSourcePath = nodeDataSourcePath;
            //LocalIpAddress = localIpAddress;
            MulticastIPEndPoint = multicastIpEndPoint;
            TcpServingPort = tcpServingPort;
            AdjacentNodesEndPointsWithIDs = adjacentNodesEndPointsWithIDs;
        }

        public IEnumerable<(int, IPEndPoint)> AdjacentNodesEndPointsWithIDs { get; set; }

        private void StartListeningToMulticastPort()
        {
            // Multicast Socket Initialization
            Socket mCastSocket = MulticastSocketInit();


            // To be put below the while loop
            //mCastSocket.Close(300);

            Console.Out.WriteLine($"Start listening to {MulticastIPEndPoint}");

            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[Common.MulticastBufferSize];

            while (true)
            {
                Console.Out.WriteLine("Waiting for multicast packets...");
                Console.Out.WriteLine("Enter ^C to terminate");

                int bytesRead = mCastSocket.ReceiveFrom(buffer, ref remoteEndPoint);

                ProcessMulticastMessage(buffer, bytesRead);
            }
        }

        private Socket MulticastSocketInit()
        {
            Socket mCastSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);

            var localEndPoint = new IPEndPoint(_localIpAddress, MulticastIPEndPoint.Port);

            mCastSocket.ExclusiveAddressUse = false;
            mCastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            mCastSocket.Bind(localEndPoint);

            MulticastOption multicastOption =
                new MulticastOption(MulticastIPEndPoint.Address, _localIpAddress);

            mCastSocket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                multicastOption);

            Console.WriteLine("Current multicast group is: " + multicastOption.Group);
            Console.WriteLine("Current multicast local address is: " + multicastOption.LocalAddress);

            return mCastSocket;
        }

        private void ProcessMulticastMessage(byte[] messageBuffer, int bytesRead)
        {
            string xmlMessage = messageBuffer.Take(bytesRead).ToArray().ToUtf8String();

            var requestMessage = xmlMessage.DeserializeTo<MulticastDiscoveryRequestMessage>();

            Console.Out.WriteLine("The captured Discovery Request Message is:");
            Console.Out.WriteLine(requestMessage);

            var clientIpAddress = IPAddress.Parse(requestMessage.IPAddress);
            int clientListeningPort = requestMessage.ListeningPort;

            // GENERATE RESPONSE

            var responseAgent = new DiscoveryResponseAgent();

            var responseMessage = new DiscoveryResponseMessage
            {
                //IPAddress = LocalIpAddress.ToString(), // $c$ to be changed
                IPAddress = _localIpAddress.ToString(),
                ListeningPort = TcpServingPort,
                NodeConnectionNum = AdjacentNodesEndPointsWithIDs.Count()
            };

            Console.Out.WriteLine(
                $"Client said that he wants to get DISCOVERY Response at {clientIpAddress}:{clientListeningPort}");

            responseAgent.SendDiscoveryResponse(responseMessage, clientIpAddress, clientListeningPort);
        }

        private void StartListeningToTcpServingPort()
        {
            var portListener = new TcpPortListener();
            portListener.StartListening(TcpServingPort, HandleRequest);
        }

        private void HandleRequest(Socket workerSocket)
        {
            #region Get Request Data Message

            var interceptor = new RequestInterceptor();
            RequestDataMessage requestDataMessage = interceptor.GetRequest(workerSocket);

            #endregion

            #region Trash

            //while (true)
            //{
            //    if (tcpListener.Inactive)
            //    {
            //        break;
            //    }
            //    byte[] buffer = new byte[BufferSize];
            //    int receivedBytes = networkStream.Read(buffer, 0, buffer.Length);
            //    if (receivedBytes == 0)
            //    {
            //        Console.Out.WriteLine($@" [TCP]   >> SERVER WORKER says: ""No bytes received. Connection closed.""");
            //        break;
            //    }
            //    receivedBinaryData.AddLast(buffer.Take(receivedBytes));
            //}

            #endregion

            #region Business Logic

            var dslInterpreter = new DSLInterpreter(requestDataMessage);

            var employees = Enumerable.Empty<Employee>().ToList();

            var dataAgentRequestTasks = new LinkedList<Task<string>>();

            if (AdjacentNodesEndPointsWithIDs.Any() && requestDataMessage.Propagation > 0)
            {
                // Message Retransmission
                RequestDataMessage replicatedMessage = requestDataMessage.Replicate();

                replicatedMessage.Propagation = 0;
                replicatedMessage.DataFormat = Common.Xml;

                var dataAgent = new DataAgent();

                foreach (var idEpPair in AdjacentNodesEndPointsWithIDs)
                {
                    Task<string> requestDataTask = Task.Run<string>(() =>
                    {
                        string xmlData = dataAgent.MakeRequest(
                            replicatedMessage,
                            idEpPair.Item2,
                            idEpPair.Item1.ToString());

                        return xmlData;
                    });

                    dataAgentRequestTasks.AddLast(requestDataTask);
                }
            }

            #region Trash

            //while (dataAgentRequestTasks.Count > 0)
            //{
            //    // Identify the first task that completes.
            //    Task<string> firstCompletedTask = Task.WhenAny(dataAgentRequestTasks).Result;

            //    // Remove the selected task from the list so that you don't 
            //    // process it more than once.
            //    dataAgentRequestTasks.Remove(firstCompletedTask);

            //    // Await the completed task.
            //    string xmlData = firstCompletedTask.Result;

            //    var employeesContainer = xmlData.DeserializeTo<EmployeesRoot>();

            //    employees.AddRange(employeesContainer.EmployeeArray);
            //}

            #endregion

            Task.WaitAll(dataAgentRequestTasks.Cast<Task>().ToArray()); // WARNING: $C$ IMPORTANT CHANGES

            foreach (Task<string> task in dataAgentRequestTasks)
            {
                string xmlData = task.Result;

                EmployeesRoot root = xmlData.DeserializeTo<EmployeesRoot>();

                employees.AddRange(root.EmployeeArray);
            }

            IEnumerable<Employee> dataFromCurrentNode = dslInterpreter
                .GetDataFromDataSource(DataSourcePath);

            employees.AddRange(dataFromCurrentNode);

            if (requestDataMessage.Propagation > 0)
            {
                employees = employees.Distinct(EmployeeIdComparer.Default).ToList();

                employees = dslInterpreter.ProcessData(employees).ToList();
            }

            var dslConverter = new DSLConverter(requestDataMessage);

            string serializedData = dslConverter.TransformDataToRequiredFormat(employees);

            #endregion

            #region Send Back Response Data

            var respondent = new DataRespondent();
            respondent.SendResponse(workerSocket, serializedData);

            #endregion

            workerSocket.Close();
        }
    }
}