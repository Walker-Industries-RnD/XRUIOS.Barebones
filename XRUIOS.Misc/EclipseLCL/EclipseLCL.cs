using System.Runtime.Serialization;
using MagicOnion;
using MessagePack;

namespace EclipseLCL
{

    [MessagePackObject(keyAsPropertyName: true)]
    [DataContract]
    public class DiracPackage
    {
        // Single object members
        [DataMember] public string? UUID;
        [DataMember] public Type? ObjType;
        [DataMember] public Dictionary<string, object?>? Fields;
        // Collection members — only one of these will be set
        [DataMember] public List<DiracPackage>? Collection;
        [DataMember] public Dictionary<string, DiracPackage>? Map;

        public DiracPackage(string uuid, Type objType, Dictionary<string, object?> fields)
        {
            UUID = uuid;
            ObjType = objType;
            Fields = fields;
        }
        public DiracPackage(List<DiracPackage> collection) => Collection = collection;
        public DiracPackage(Dictionary<string, DiracPackage> map) => Map = map;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class FunctionParameter
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    [MessagePackObject(keyAsPropertyName: true)]
    [DataContract]
    public class DiracFunction
    {
        public string FunctionName;
        public List<FunctionParameter> Parameters;
        public string? ReturnType;

        public DiracFunction(string functionName, List<FunctionParameter> parameters, string? returnType)
        {
            FunctionName = functionName;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    [DataContract]
    public class DiracResponse
    {
        [DataMember] public bool Success { get; set; }  //Did this work?
        [DataMember] public byte[] EncryptedData { get; set; } //The data returned from the function, handled with UnpackData
        [DataMember] public string ServerMessage { get; set; } //Information about the run

        public DiracResponse(bool success, byte[] encryptedData, string serverMessage)
        {
            Success = success;
            EncryptedData = encryptedData;
            ServerMessage = serverMessage;
        }
    }

}


