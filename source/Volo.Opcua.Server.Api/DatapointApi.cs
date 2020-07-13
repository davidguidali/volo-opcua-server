using Grpc.Core;
using LibUA.Core;
using System.Threading.Tasks;

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
            _serverApplication.AddDatapoint(new NodeId(2, request.Identifier), request.Value);
            return Task.FromResult(new ApiResult() { ResultCode = "200", Message = "Datapoint has been added" });
        }

        public override Task<ApiResult> UpdateDatapoint(DatapointMessage request, ServerCallContext context)
        {
            _serverApplication.AddDatapoint(new NodeId(2, request.Identifier), request.Value);
            return Task.FromResult(new ApiResult() { ResultCode = "200", Message = "Datapoint has been updated" });
        }
    }
}