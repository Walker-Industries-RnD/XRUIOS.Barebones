using EclipseLCL;
using MagicOnion;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Pariah_Cybersecurity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static EclipseProject.Security;
using static Pariah_Cybersecurity.DataHandler;

namespace EclipseProject
{
    public class Ocean
    {
        [MessagePackObject(keyAsPropertyName: true, AllowPrivate = true)]
        public class RegisteredFunction
        {
            public Type DeclaringType { get; set; }
            public MethodInfo Method { get; set; }

            internal RegisteredFunction(Type declaringType, MethodInfo method)
            {
                DeclaringType = declaringType;
                Method = method;
            }
        }


        //All our registered functions
        internal Dictionary<string, List<RegisteredFunction>> Ark = new();

        //We will only flood blessed land, so we will only register functions with the SeaOfDirac attribute
        private System.Reflection.Assembly localAssembly = Assembly.GetExecutingAssembly();

        //Add all the functions to the sea; let these souls be free to roam the sea and be used by other applications
        public void FloodTheSea(System.Reflection.Assembly? assembly)
        {
            assembly ??= localAssembly;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    var attr = method.GetCustomAttribute<SeaOfDirac>();
                    if (attr != null)
                    {
                        if (!Ark.TryGetValue(attr.Name, out var list))
                        {
                            list = new List<RegisteredFunction>();
                            Ark[attr.Name] = list;
                        }
                        list.Add(new RegisteredFunction(type, method));

                    }
                }
            }

        }


        //This function will be called by the ATField; it will handle the logic
        //of calling the correct function and returning the data to the ATField, which will then return it to the caller

        public async Task<DiracResponse> HandleRequestAsync(string functionName, Dictionary<string, object?> parameters, SessionState session)
        {

            try
            {
                if (!TryResolveOverload(functionName, parameters, out var regFunc) || regFunc == null)
                    return new DiracResponse(false, Array.Empty<byte>(), "Function not found in Eclipse Ocean");

                AeadChannel channel = session.ToClient;
                Dictionary<string, object?> vault = session.Vault;

                // Get the SeaOfDirac attribute
                var attr = regFunc.Method.GetCustomAttribute<SeaOfDirac>();
                if (attr == null)
                    throw new Exception("Registered function missing SeaOfDirac attribute");

                // Parameter count check
                if (attr.ParameterTypes.Length != parameters.Count)
                    throw new Exception("Parameter count mismatch");

                // Unpack parameters using attribute types
                object?[] args = new object?[attr.ParameterTypes.Length];
                int i = 0;
                foreach (var kv in parameters)
                {
                    if (kv.Value == null)
                    {
                        args[i++] = null;
                        continue;
                    }

                    object? param = kv.Value;
                    if (IsXruiosType(attr.ParameterTypes[i]))
                    {
                        byte[] packageBytes = MessagePackSerializer.Serialize(param, ContractlessStandardResolver.Options);
                        DiracPackage package = (DiracPackage)MessagePackSerializer.Deserialize(typeof(DiracPackage), packageBytes, ContractlessStandardResolver.Options)!;
                        if (package.Collection != null)
                        {
                            var resolved = new List<object?>();
                            foreach (var pkg in package.Collection)
                                resolved.Add(vault[pkg.UUID!] ?? throw new Exception("Client sent invalid DiracPackage in collection."));
                            param = resolved;
                        }
                        else if (package.Map != null)
                        {
                            var resolved = new Dictionary<string, object?>();
                            foreach (var kvp in package.Map)
                                resolved[kvp.Key] = vault[kvp.Value.UUID!] ?? throw new Exception("Client sent invalid DiracPackage in map.");
                            param = resolved;
                        }
                        else
                        {
                            param = vault[package.UUID!] ?? throw new Exception("Client sent invalid DiracPackage.");
                        }
                    }

                    byte[] paramBytes = MessagePackSerializer.Serialize(param, ContractlessStandardResolver.Options);
                    args[i] = MessagePackSerializer.Deserialize(attr.ParameterTypes[i], paramBytes, ContractlessStandardResolver.Options);
                    i++;
                }

                // Handle static vs instance
                object? instance = regFunc.Method.IsStatic ? null : Activator.CreateInstance(regFunc.DeclaringType);

                // Invoke the method
                var rawResult = regFunc.Method.Invoke(instance, args);
                object? result;
                if (rawResult is Task task)
                {
                    await task;
                    var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
                    result = taskResult?.GetType().Name == "VoidTaskResult" ? null : taskResult;
                }
                else
                {
                    result = rawResult;
                }
                
                byte[] serializedResult;
                if (result == null)
                {
                    serializedResult = [];
                }
                else if (IsXruiosType(result.GetType()))
                {
                    Type resultType = result.GetType();
                    if (IsXruiosDictionary(resultType))
                    {
                        var map = new Dictionary<string, DiracPackage>();
                        foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)result)
                        {
                            if (entry.Value == null) continue;
                            string itemUUID = Guid.NewGuid().ToString();
                            vault.Add(itemUUID, entry.Value);
                            map[entry.Key.ToString()!] = new DiracPackage(itemUUID, entry.Value.GetType(), SnapshotFields(entry.Value));
                        }
                        serializedResult = MessagePackSerializer.Serialize(typeof(DiracPackage), new DiracPackage(map), ContractlessStandardResolver.Options);
                    }
                    else if (IsXruiosCollection(resultType, out Type? elementType))
                    {
                        var packages = new List<DiracPackage>();
                        foreach (object? item in (System.Collections.IEnumerable)result)
                        {
                            if (item == null) continue;
                            string itemUUID = Guid.NewGuid().ToString();
                            vault.Add(itemUUID, item);
                            packages.Add(new DiracPackage(itemUUID, item.GetType(), SnapshotFields(item)));
                        }
                        serializedResult = MessagePackSerializer.Serialize(typeof(DiracPackage), new DiracPackage(packages), ContractlessStandardResolver.Options);
                    }
                    else
                    {
                        string classUUID = Guid.NewGuid().ToString();
                        vault.Add(classUUID, result);
                        serializedResult = MessagePackSerializer.Serialize(typeof(DiracPackage), new DiracPackage(classUUID, resultType, SnapshotFields(result)), ContractlessStandardResolver.Options);
                    }
                } else
                {
                    /*Type resultType = result.GetType();
                    bool isTuple = resultType.IsGenericType && resultType.FullName?.StartsWith("System.ValueTuple") == true;
                    bool isKvp = resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
                    if (isTuple || isKvp)
                    {
                        var list = new List<object?>();
                        foreach (var field in resultType.GetFields())
                            list.Add(field.GetValue(result));
                        serializedResult = MessagePackSerializer.Serialize(list, ContractlessStandardResolver.Options);
                    }
                    else
                    {*/
                        serializedResult = MessagePackSerializer.Serialize(result, ContractlessStandardResolver.Options);
                    //}
                }
                
                EncryptedEnvelope env = channel.Encrypt(functionName, serializedResult);

                byte[] serializedEnv = MessagePackSerializer.Serialize(env, ContractlessStandardResolver.Options);

                return new DiracResponse(true, serializedEnv, "Success");
            }
            catch (Exception ex)
            {
                return new DiracResponse(false, Array.Empty<byte>(), ex.Message);
            }
        }




        //Let the third impact begin.

        //Launch the server; this will allow other applications to call our functions


        private static Dictionary<string, object?> SnapshotFields(object item)
        {
            Type itemType = item.GetType();
            Dictionary<string, object?> fields = new();
            foreach (var field in itemType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields[field.Name] = field.GetValue(item);
            foreach (var prop in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
                fields[prop.Name] = prop.GetValue(item);
            return fields;
        }

        private static bool IsXruiosType(Type? type)
        {
            if (type == null) return false;
            if (type.Namespace?.StartsWith("XRUIOS") == true) return true;
            if (type.IsArray) return IsXruiosType(type.GetElementType());
            if (type.IsGenericType) return type.GetGenericArguments().Any(IsXruiosType);
            return false;
        }

        private static bool IsXruiosDictionary(Type type)
        {
            var dictIface = type.GetInterfaces().Concat([type])
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (dictIface == null) return false;
            return IsXruiosType(dictIface.GetGenericArguments()[1]);
        }

        private static bool IsXruiosCollection(Type type, out Type? elementType)
        {
            elementType = null;
            if (type == typeof(string)) return false;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return elementType != null && IsXruiosType(elementType);
            }
            var enumerableIface = type.GetInterfaces().Concat([type])
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableIface != null)
            {
                elementType = enumerableIface.GetGenericArguments()[0];
                return IsXruiosType(elementType);
            }
            return false;
        }

        private static Type UnwrapTask(Type type)
        {
            if (type == typeof(Task)) return typeof(void);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                return type.GetGenericArguments()[0];
            return type;
        }

        //List all the functions
        public IEnumerable<DiracFunction> ListFunctionDetails()
        {
            return Ark.SelectMany(kvp =>
                kvp.Value.Select(regFunc =>
                {
                    var attr = regFunc.Method.GetCustomAttribute<SeaOfDirac>();
                    var parameters = new List<FunctionParameter>();
                    if (attr != null)
                    {
                        for (int j = 0; j < attr.ParameterTypes.Length; j++)
                        {
                            parameters.Add(new FunctionParameter
                            {
                                Name = attr.ParameterNames?[j] ?? $"param{j}",
                                Type = UnwrapTask(attr.ParameterTypes[j]).ToString()
                            });
                        }
                    }
                    return new DiracFunction(kvp.Key, parameters, attr == null ? null : UnwrapTask(attr.ReturnType).ToString());
                })
            );
        }

        private bool TryResolveOverload(string name, Dictionary<string, object?> parameters, out RegisteredFunction? match)
        {
            match = null;
            if (!Ark.TryGetValue(name, out var candidates)) return false;

            foreach (var candidate in candidates)
            {
                var attr = candidate.Method.GetCustomAttribute<SeaOfDirac>()!;
                if (attr.ParameterTypes.Length != parameters.Count) continue;

                // If parameter names are provided on the attribute, use them to verify ordering
                // otherwise fall back to positional type matching
                match = candidate;
                return true;
            }
            return false;
        }

    }
}
