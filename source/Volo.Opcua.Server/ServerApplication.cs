using LibUA.Core;
using LibUA.Server;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Volo.Opcua.Server
{
    public partial class ServerApplication : Application
    {
        private readonly ApplicationDescription _appDescription;
        private readonly NodeObject _itemsRoot;
        private readonly NodeVariable[] _trendNodes;
        private readonly AppSettings _settings;
        private readonly SecurityProvider _securityProvider;

        public override X509Certificate2 ApplicationCertificate
        {
            get { return _securityProvider.Cert; }
        }

        public override RSACryptoServiceProvider ApplicationPrivateKey
        {
            get { return _securityProvider.Key; }
        }

        public ServerApplication(AppSettings settings, SecurityProvider securityProvider)
        {
            _settings = settings;
            _securityProvider = securityProvider;

            _securityProvider.LoadCertificateAndPrivateKey();

            _appDescription = new ApplicationDescription(_settings.ApplicationUri,
                                                           _settings.ProductUri,
                                                           new LocalizedText("en-US", _settings.ApplicationName),
                                                           ApplicationType.Server,
                                                           null,
                                                           null,
                                                           null);

            _itemsRoot = new NodeObject(new NodeId(2, 0), new QualifiedName("Items"), new LocalizedText("Items"),
            new LocalizedText("Items"), 0, 0, 0);

            // Objects organizes Items
            AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(2, 0), false));
            _itemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(UAConst.ObjectsFolder), true));
            AddressSpaceTable.TryAdd(_itemsRoot.Id, _itemsRoot);

            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerRead)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerWrite)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerMethodCall)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerBrowse)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerRegisterNodes)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerTranslateBrowsePathsToNodeIds)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxNodesPerNodeManagement)] as NodeVariable).Value = 100;
            (AddressSpaceTable[new NodeId(UAConst.OperationLimitsType_MaxMonitoredItemsPerCall)] as NodeVariable).Value = 100;

            _trendNodes = new NodeVariable[1000];
            var nodeTypeFloat = new NodeId(0, 10);
            for (int i = 0; i < _trendNodes.Length; i++)
            {
                var id = string.Format("Trend {0}", (1 + i).ToString("D6"));
                _trendNodes[i] = new NodeVariable(new NodeId(2, (uint)(1 + i)), new QualifiedName(id),
                    new LocalizedText(id), new LocalizedText(id), 0, 0,
                    AccessLevel.CurrentRead | AccessLevel.HistoryRead,
                    AccessLevel.CurrentRead | AccessLevel.HistoryRead, 0, true, nodeTypeFloat);

                _itemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), _trendNodes[i].Id, false));
                _trendNodes[i].References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), _itemsRoot.Id, true));
                AddressSpaceTable.TryAdd(_trendNodes[i].Id, _trendNodes[i]);
            }
        }

        public override IList<EndpointDescription> GetEndpointDescriptions(string endpointUrlHint)
        {
            var certStr = ApplicationCertificate.Export(X509ContentType.Cert);

            var epNoSecurity = new EndpointDescription(
                endpointUrlHint, _appDescription, null,
                MessageSecurityMode.None, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                }, Types.TransportProfileBinary, 0);

            var epSignBasic128Rsa15 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15]),
                }, Types.TransportProfileBinary, 0);

            var epSignBasic256 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256]),
                }, Types.TransportProfileBinary, 0);

            var epSignBasic256Sha256 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.Sign, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                }, Types.TransportProfileBinary, 0);

            var epSignEncryptBasic128Rsa15 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic128Rsa15]),
                }, Types.TransportProfileBinary, 0);

            var epSignEncryptBasic256 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256]),
                }, Types.TransportProfileBinary, 0);

            var epSignEncryptBasic256Sha256 = new EndpointDescription(
                endpointUrlHint, _appDescription, certStr,
                MessageSecurityMode.SignAndEncrypt, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                        new UserTokenPolicy("1", UserTokenType.UserName, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.Basic256Sha256]),
                }, Types.TransportProfileBinary, 0);

            return new EndpointDescription[]
            {
                    epNoSecurity,
                    epSignBasic256Sha256, epSignEncryptBasic256Sha256,
                    epSignBasic128Rsa15, epSignEncryptBasic128Rsa15,
                    epSignBasic256, epSignEncryptBasic256
            };
        }

        public override ApplicationDescription GetApplicationDescription(string endpointUrlHint)
        {
            return _appDescription;
        }

        protected override DataValue HandleReadRequestInternal(NodeId id)
        {
            Node node = null;
            if (id.NamespaceIndex == 2 &&
                AddressSpaceTable.TryGetValue(id, out node))
            {
                return new DataValue(3.14159265, StatusCode.Good, DateTime.Now);
            }

            return base.HandleReadRequestInternal(id);
        }

        protected int rowCount = 1;
        protected Random rnd = new Random();
        protected UInt64 nextEventId = 1;

        private EventNotification GenerateSampleAlarmEvent(DateTime eventTime)
        {
            return new EventNotification(new EventNotification.Field[]
            {
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("EventId")}
                        ),
                        Value = nextEventId
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("EventType")}
                        ),
                        Value = new NodeId(UAConst.ExclusiveLevelAlarmType)
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("SourceName")}
                        ),
                        Value = "Source name"
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("Time")}
                        ),
                        Value = eventTime,
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("Message")}
                        ),
                        Value = new LocalizedText("Event message")
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("Severity")}
                        ),
                        // Severity is 0 to 1000
                        Value = (UInt16) (rnd.Next() % 1000)
                    },
                    // ActiveState object is a name, Id gives the value specified by the name
                    // The names do not mean anything (just display text), but Id is important
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("ActiveState")}
                        ),
                        Value = new LocalizedText("Active")
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            // Represents ActiveState.Id
                            new[] {new QualifiedName("ActiveState"), new QualifiedName("Id")}
                        ),
                        // Inactive specifies false, Active specifies true
                        Value = true
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("ActiveState"), new QualifiedName("EffectiveDisplayName")}
                        ),
                        Value = new LocalizedText("Alarm active")
                    },
                    // Same rules for AckedState
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("AckedState")}
                        ),
                        Value = new LocalizedText("Acknowledged")
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            // Represents AckedState.Id
                            new[] {new QualifiedName("AckedState"), new QualifiedName("Id")}
                        ),
                        // Inactive specifies false, Active specifies true
                        Value = true,
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("Retain")}
                        ),
                        Value = true
                    },
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            new[] {new QualifiedName("ConditionName")}
                        ),
                        Value = "Sample alarm"
                    },
                    // Necessary field for alarms
                    new EventNotification.Field()
                    {
                        Operand = new SimpleAttributeOperand(
                            NodeId.Zero, new[] {new QualifiedName("ConditionType")},
                            NodeAttribute.NodeId, null
                        ),
                        Value = NodeId.Zero
                    },
            });
        }

        public void PlayRow()
        {
            Console.WriteLine("Play row {0}", rowCount);

            foreach (var node in _trendNodes)
            {
                var dv = new DataValue((float)(rowCount + 0.1 * rnd.NextDouble()), StatusCode.Good, DateTime.Now);
                // MonitorNotifyDataChange(node.Id, dv);
            }

            ++rowCount;

            var eventTime = DateTime.UtcNow;
            var ev = GenerateSampleAlarmEvent(eventTime);
            MonitorNotifyEvent(new NodeId(UAConst.Server), ev);

            nextEventId++;
        }
    }
}