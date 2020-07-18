# Volo OPCUA Server
OPCUA server implementation for the Volo project.

## Configuration
Map host port to port 50051 for Grpc and 7718 for OPC-UA. Mount config file from host into C:app\config on container. 

Example:

```
docker run -p 50051:50051 -p 7718:7718 --name volo-opcua-server volo-opcua-server
```