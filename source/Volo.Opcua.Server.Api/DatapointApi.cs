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
    }
}