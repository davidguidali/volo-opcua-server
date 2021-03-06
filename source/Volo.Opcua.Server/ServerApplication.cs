﻿using LibUA.Core;
using LibUA.Server;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Volo.Opcua.Server.Shared;

namespace Volo.Opcua.Server
{
    public partial class ServerApplication : Application
    {
        private readonly ApplicationDescription _appDescription;
        private readonly NodeObject _itemsRoot;
        private readonly Dictionary<NodeId, float> _nodes = new Dictionary<NodeId, float>();
        private readonly SecurityProvider _securityProvider;
        private readonly AppSettings _settings;

        public ServerApplication(AppSettings settings, SecurityProvider securityProvider)
        {
            _settings = settings;
            _securityProvider = securityProvider;

            _securityProvider.LoadCertificateAndPrivateKey();

            _appDescription = new ApplicationDescription(_settings.ApplicationUri, _settings.ProductUri,
                new LocalizedText(_settings.Locale, _settings.ApplicationName), ApplicationType.Server, null, null, null);

            _itemsRoot = new NodeObject(new NodeId(2, 0), new QualifiedName(_settings.RootItemName), new LocalizedText(_settings.RootItemName),
            new LocalizedText(_settings.RootItemName), 0, 0, 0);

            AddressSpaceTable[new NodeId(UAConst.ObjectsFolder)].References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(2, 0), false));
            _itemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), new NodeId(UAConst.ObjectsFolder), true));
            AddressSpaceTable.TryAdd(_itemsRoot.Id, _itemsRoot);
        }

        public override X509Certificate2 ApplicationCertificate
        {
            get { return _securityProvider.Cert; }
        }

        public override RSACryptoServiceProvider ApplicationPrivateKey
        {
            get { return _securityProvider.Key; }
        }

        public void AddDatapoint(NodeId nodeId, float value)
        {
            var node = new NodeVariable(nodeId, new QualifiedName(nodeId.StringIdentifier),
                new LocalizedText(nodeId.StringIdentifier), new LocalizedText(nodeId.StringIdentifier), 0, 0,
                AccessLevel.CurrentRead, AccessLevel.CurrentRead, 0, false, new NodeId(0, 10));

            _itemsRoot.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), node.Id, false));
            node.References.Add(new ReferenceNode(new NodeId(UAConst.Organizes), _itemsRoot.Id, true));
            AddressSpaceTable.TryAdd(node.Id, node);

            _nodes.Add(nodeId, value);
        }

        public override ApplicationDescription GetApplicationDescription(string endpointUrlHint)
        {
            return _appDescription;
        }

        public override IList<EndpointDescription> GetEndpointDescriptions(string endpointUrlHint)
        {
            var epNoSecurity = new EndpointDescription(
                endpointUrlHint, _appDescription, null,
                MessageSecurityMode.None, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None],
                new UserTokenPolicy[]
                {
                        new UserTokenPolicy("0", UserTokenType.Anonymous, null, null, Types.SLSecurityPolicyUris[(int)SecurityPolicy.None]),
                }, Types.TransportProfileBinary, 0);

            return new EndpointDescription[]
            {
                    epNoSecurity
            };
        }

        public void PlayRow()
        {
            foreach (var node in _nodes)
            {
                MonitorNotifyDataChange(node.Key, new DataValue(node.Value, StatusCode.Good, DateTime.Now));
            }
        }

        public void UpdateDatapoint(NodeId nodeId, float value)
        {
            _nodes[nodeId] = value;
        }

        public float GetDatapoint(NodeId nodeId)
        {
            return _nodes[nodeId];
        }

        public Dictionary<NodeId, float> GetDatapoints()
        {
            return _nodes;
        }

        public bool HasDatapoint(NodeId nodeId)
        {
            return _nodes.ContainsKey(nodeId);
        }

        protected override DataValue HandleReadRequestInternal(NodeId id)
        {
            if (_nodes.ContainsKey(id))
            {
                return new DataValue(_nodes[id], StatusCode.Good, DateTime.Now);
            }

            return base.HandleReadRequestInternal(id);
        }
    }
}