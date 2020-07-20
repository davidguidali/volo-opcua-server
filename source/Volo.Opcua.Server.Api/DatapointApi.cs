using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibUA.Core;
using System.Threading.Tasks;
using static ApiResult.Types;

namespace Volo.Opcua.Server.Api
{
    public class DatapointApi : DatapointService.DatapointServiceBase
    {
        private readonly ServerApplication _serverApplication;

        public DatapointApi(ServerApplication serverApplication)
        {
            _serverApplication = serverApplication;
        }

        public override Task<ApiResult> AddDatapoint(DatapointMessage request, ServerCallContext context)
        {
            var nodeId = new NodeId(2, request.Identifier);

            if (_serverApplication.HasDatapoint(nodeId))
            {
                return Task.FromResult(new ApiResult() { ResultCode = ResultCode.Error, Message = "Datapoint already exists" });
            }

            _serverApplication.AddDatapoint(nodeId, request.Value);

            return Task.FromResult(new ApiResult() { ResultCode = ResultCode.Success, Message = "Datapoint has been added" });
        }

        public override Task<ApiResult> UpdateDatapoint(DatapointMessage request, ServerCallContext context)
        {
            var nodeId = new NodeId(2, request.Identifier);

            if (!_serverApplication.HasDatapoint(nodeId))
            {
                return Task.FromResult(new ApiResult() { ResultCode = ResultCode.Error, Message = "Datapoint does not exist" });
            }

            _serverApplication.UpdateDatapoint(nodeId, request.Value);

            return Task.FromResult(new ApiResult() { ResultCode = ResultCode.Success, Message = "Datapoint has been updated" });
        }

        public override Task<DatapointResult> GetDatapoint(IdentifierMessage request, ServerCallContext context)
        {
            var nodeId = new NodeId(2, request.Identifier);

            DatapointResult datapointResult = new DatapointResult();

            if (!_serverApplication.HasDatapoint(nodeId))
            {
                datapointResult.ApiResult = new ApiResult { ResultCode = ResultCode.Error, Message = "Datapoint does not exist" };
                return Task.FromResult(datapointResult);
            }

            var value = _serverApplication.GetDatapoint(nodeId);

            datapointResult.ApiResult = new ApiResult { ResultCode = ResultCode.Success, Message = "Datapoint has been retrieved" };
            datapointResult.Datapoints.Add(new DatapointMessage() { Identifier = request.Identifier, Value = value });

            return Task.FromResult(datapointResult);
        }

        public override Task<DatapointResult> GetDatapoints(Empty request, ServerCallContext context)
        {
            var datapoints = _serverApplication.GetDatapoints();

            DatapointResult datapointResult = new DatapointResult();
            datapointResult.ApiResult = new ApiResult { ResultCode = ResultCode.Success, Message = "Datapoints have been retrieved" };

            foreach (var datapoint in datapoints)
            {
                datapointResult.Datapoints.Add(new DatapointMessage() { Identifier = datapoint.Key.StringIdentifier, Value = datapoint.Value });
            }

            return Task.FromResult(datapointResult);
        }

        public override Task<ApiResult> SetDatapoint(DatapointMessage request, ServerCallContext context)
        {
            var nodeId = new NodeId(2, request.Identifier);

            if (_serverApplication.HasDatapoint(nodeId))
            {
                return AddDatapoint(request, context);
            }

            return UpdateDatapoint(request, context);
        }
    }
}