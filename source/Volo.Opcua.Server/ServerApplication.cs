using LibUA;
using LibUA.Core;
using LibUA.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Volo.Opcua.Server
{
    public class ServerApplication : Application
    {
        private readonly ApplicationDescription _appDescription;
        private readonly NodeObject _itemsRoot;
        private readonly NodeVariable[] _trendNodes;
        private X509Certificate2 _appCertifiacte = null;
        private RSACryptoServiceProvider _cryptPrivateKey = null;

        public override X509Certificate2 ApplicationCertificate
        {
            get { return _appCertifiacte; }
        }

        public override RSACryptoServiceProvider ApplicationPrivateKey
        {
            get { return _cryptPrivateKey; }
        }

        public ServerApplication(AppSettings settings)
        {
            LoadCertificateAndPrivateKey();

            _appDescription = new ApplicationDescription(settings.ApplicationUri,
                                                           settings.ProductUri,
                                                           new LocalizedText("en-US", settings.ApplicationName),
                                                           ApplicationType.Server,
                                                           null,
                                                           null,
                                                           null);

            _itemsRoot = new NodeObject(new NodeId(2, 0), new QualifiedName("Items"), new LocalizedText("Items"),
            new LocalizedText("Items"), 0, 0, 0);

            // Objects organizes Items
            AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References
                .Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(2, 0), false));
            _itemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes),
                new NodeId(UAConst.ObjectsFolder), true));
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

        public override object SessionCreate(SessionCreationInfo sessionInfo)
        {
            // Create and return a session object with sessionInfo

            return null;
        }

        public override bool SessionValidateClientApplication(object session,
            ApplicationDescription clientApplicationDescription, byte[] clientCertificate, string sessionName)
        {
            // Update your session object with the client's UA application description
            // Return true to allow the client, false to reject

            return true;
        }

        public override void SessionRelease(object session)
        {
        }

        public override bool SessionValidateClientUser(object session, object userIdentityToken)
        {
            if (userIdentityToken is UserIdentityAnonymousToken)
            {
                return true;
            }
            else if (userIdentityToken is UserIdentityUsernameToken)
            {
                var username = (userIdentityToken as UserIdentityUsernameToken).Username;
                var password =
                    (new UTF8Encoding()).GetString((userIdentityToken as UserIdentityUsernameToken).PasswordHash);

                return true;
            }

            throw new Exception("Unhandled user identity token type");
            return base.SessionValidateClientUser(session, userIdentityToken);
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

        private List<DataValue> testHistoryPoints = null;

        public override UInt32 HandleHistoryReadRequest(object session, object readDetails, HistoryReadValueId id,
            ContinuationPointHistory continuationPoint, List<DataValue> results, ref int? offsetContinueFit)
        {
            if (testHistoryPoints == null)
            {
                testHistoryPoints = new List<DataValue>();

                var dt = new DateTime(2015, 12, 1);
                for (int i = 0; i < 100000; i++)
                {
                    testHistoryPoints.Add(new DataValue(
                        Math.Sin(i * 0.3) + Math.Cos(i * 0.17) * 0.5 + Math.Sin(i * 0.087) * 0.25, StatusCode.Good,
                        dt));
                    dt = dt.AddHours(1);
                }
            }

            int startOffset = continuationPoint.IsValid ? continuationPoint.Offset : 0;
            if (readDetails is ReadRawModifiedDetails)
            {
                var rd = readDetails as ReadRawModifiedDetails;
                for (int i = 0; i < 100000; i++)
                {
                    var p = testHistoryPoints[i];
                    if (p.SourceTimestamp >= rd.StartTime &&
                        p.SourceTimestamp < rd.EndTime)
                    {
                        // Skip startOffset points
                        if (startOffset > 0)
                        {
                            startOffset--;
                            continue;
                        }

                        results.Add(p);
                    }
                }

                return (UInt32)StatusCode.Good;
            }

            return (UInt32)StatusCode.BadHistoryOperationUnsupported;
        }

        public override UInt32 HandleHistoryEventReadRequest(object session, object readDetails,
            HistoryReadValueId id, ContinuationPointHistory continuationPoint, List<object[]> results)
        {
            if (readDetails is ReadEventDetails)
            {
                var rd = readDetails as ReadEventDetails;

                var dt = rd.StartTime;
                for (int i = 0; i < 5; i++)
                {
                    var ev = GenerateSampleAlarmEvent(dt);
                    results.Add(NetDispatcher.MatchFilterClauses(rd.SelectClauses, ev));
                    dt = dt.AddMinutes(1);
                }

                return (UInt32)StatusCode.Good;
            }

            return (UInt32)StatusCode.BadHistoryOperationUnsupported;
        }

        protected int rowCount = 1;
        protected Random rnd = new Random();

        // These numbers are allowed to wrap in case of overflow
        // These are usually used by the client to match events
        protected UInt64 nextEventId = 1;

        private EventNotification GenerateSampleAlarmEvent(DateTime eventTime)
        {
            return new EventNotification(new EventNotification.Field[]
            {
                    // During publishing, operand BrowsePaths are matched
                    // against UA select clauses from the subscriber.
                    // The operands shown here are the most common requested (90% of cases).
                    // Types match operand BrowsePath, do not change them and remember
                    // casting when passing into a variant.

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
            //Console.WriteLine("Play row {0}", rowCount);

            foreach (var node in _trendNodes)
            {
                var dv = new DataValue((float)(rowCount + 0.1 * rnd.NextDouble()), StatusCode.Good, DateTime.Now);
                MonitorNotifyDataChange(node.Id, dv);
            }

            ++rowCount;

            var eventTime = DateTime.UtcNow;
            var ev = GenerateSampleAlarmEvent(eventTime);
            // MonitorNotifyEvent(new NodeId(UAConst.Server), ev);

            nextEventId++;
        }

        private void LoadCertificateAndPrivateKey()
        {
            try
            {
                // Try to load existing (public key) and associated private key
                _appCertifiacte = new X509Certificate2("ServerCert.der");
                _cryptPrivateKey = new RSACryptoServiceProvider();

                var rsaPrivParams = UASecurity.ImportRSAPrivateKey(File.ReadAllText("ServerKey.pem"));
                _cryptPrivateKey.ImportParameters(rsaPrivParams);
            }
            catch
            {
                // Make a new certificate (public key) and associated private key
                var dn = new X500DistinguishedName("CN=Client certificate;OU=Demo organization",
                    X500DistinguishedNameFlags.UseSemicolons);
                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddUri(new Uri("urn:volo.opcua.server"));

                using (RSA rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    request.CertificateExtensions.Add(sanBuilder.Build());

                    var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                        new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

                    _appCertifiacte = new X509Certificate2(certificate.Export(X509ContentType.Pfx, ""),
                        "", X509KeyStorageFlags.DefaultKeySet);

                    var certPrivateParams = rsa.ExportParameters(true);
                    File.WriteAllText("ServerCert.der", UASecurity.ExportPEM(_appCertifiacte));
                    File.WriteAllText("ServerKey.pem", UASecurity.ExportRSAPrivateKey(certPrivateParams));

                    _cryptPrivateKey = new RSACryptoServiceProvider();
                    _cryptPrivateKey.ImportParameters(certPrivateParams);
                }
            }
        }
    }
}