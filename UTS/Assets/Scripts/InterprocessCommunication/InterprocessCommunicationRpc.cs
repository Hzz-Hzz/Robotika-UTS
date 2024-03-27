using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


/**
 * E is a generic parameter for an Enum. The enum's underlying data type MUST BE an integer (int32).
 */
public class InterprocessCommunicationRpc<E> where E: System.Enum
{
    public const int INVALID_RETURN_TYPE_EXCEPTION = Int32.MaxValue;
    public const int INVALID_PARAMETER_EXCEPTION = Int32.MaxValue - 1;
    public const int INVALID_QUERY_CODE = Int32.MaxValue - 2;
    public bool doNotSendErrorOnAlreadyClosedPipe = false;

    private InterprocessCommunicationWithTypes _interprocessCommunication;

    public InterprocessCommunicationRpc(InterprocessCommunicationWithTypes _interprocessCommunication) {
        this._interprocessCommunication = _interprocessCommunication;
    }

    public async void startListeningAsyncFireAndForgetButKeepException(Action<Exception> onException) {
        startListeningAsync().ContinueWith((t) => {
            onException.Invoke(t.Exception.InnerException);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    public async Task startListeningAsync() {
        _interprocessCommunication.onReceiveMessage += messageReceived;
        await _interprocessCommunication.startListeningAsync();
    }

    private System.Random random = new System.Random();
    private Dictionary<Tuple<int, long>, Action<object>> requestQueue = new ();
    private Dictionary<int, Delegate> registeredMethods = new ();

    public Task<R> call<R>(E queryCode, params object[] parameters) {
        var requestId = random.Next();
        return call<R>((int)(object)queryCode, requestId, parameters);
    }

    public void registerMethod(E query, Delegate method) {
        var queryCode = (int)(object)query;
        if (registeredMethods.ContainsKey(queryCode))
            throw new Exception($"Method that handles {query.ToString()} alread exists");
        registeredMethods[queryCode] = method;
    }


    private Task<R> call<R>(int queryCode, long requestId, object[] parameters) {
        var completionSource = new TaskCompletionSource<R>();
        var key = new Tuple<int, long>(queryCode, requestId);
        requestQueue.Add(key, o => {
            var returnTypeIsNullable = (Nullable.GetUnderlyingType(typeof(R)) != null);
            if (typeof(R) == typeof(NoReturn)) {
                completionSource.SetResult(default(R));
            }
            if (o == null || Equals(o, default(R)) ) {
                completionSource.SetResult(default(R));
                return;
            }
            try {
                var jtoken = JToken.FromObject(o);
                var retVal = jtoken.ToObject<R>();
                completionSource.SetResult(retVal);
                requestQueue.Remove(key);
            }
            catch (InvalidCastException e) {
                raiseInvalidReturnTypeErrorToOtherParty(queryCode, requestId, o, completionSource);
            }
        });
        return sendQueryAndWaitUntilSent(queryCode, requestId, parameters, completionSource);
    }

    private Task<R> sendQueryAndWaitUntilSent<R>(int queryCode, long requestId, object[] parameters, TaskCompletionSource<R> completionSource) {
        var data = new Tuple<int, long, object[]>(queryCode, requestId, parameters);
        var sendMessage = _interprocessCommunication.writeMessage(data);  // do not await
        Func<Task<R>> ret = (async () => {
            var success = await sendMessage;
            if (!success && !doNotSendErrorOnAlreadyClosedPipe)
                throw new IOException("Communication already closed");
            return await completionSource.Task;
        });
        return ret.Invoke();
    }

    private void messageReceived(InterprocessCommunicationWithTypes sender) {
        var msg = sender.readMessage<Tuple<int, long, object[]>>();
        var commands = msg.Item1;
        var id = msg.Item2;
        var parameters = msg.Item3;
        if (commands == INVALID_RETURN_TYPE_EXCEPTION || commands == INVALID_PARAMETER_EXCEPTION
                                                      || commands == INVALID_QUERY_CODE) {
            var reqId = (long) parameters[0];
            var message = (string) parameters[1];
            throw new Exception(message);
        }

        var commandEnum = getEnumFromInt(commands);
        if (handleResponseData(commands, id, parameters))
            return;
        handleCallingFunction(commands, id, parameters);

    }



    private E getEnumFromInt(int intCode) {
        if (!Enum.IsDefined(typeof(E), intCode))
            throw new Exception("Invalid int code");
        return (E)(object)intCode;
    }

    private bool handleResponseData(int command, long id, object[] parameters) {
        var key = new Tuple<int, long>(command, id);
        if (!requestQueue.ContainsKey(key))
            return false;
        var action = requestQueue[key];
        var returnData = parameters.Length > 0? parameters[0] : default;
        action.Invoke(returnData);
        return true;
    }

    private void handleCallingFunction(int queryCode, long id, object[] parameters) {
        if (!registeredMethods.ContainsKey(queryCode)) {
            raiseQueryCodeErrorToOtherParty(queryCode, id);
            return;
        }

        var method = registeredMethods[queryCode];
        var methodParamTypes = method.GetMethodInfo().GetParameters().Select(p => p.ParameterType)
            .ToArray();
        try {
            var convertedParameters = new object[parameters.Length].Select((_, i) => {
                if (parameters[i] == null)
                    return null;
                var jtoken = JToken.FromObject(parameters[i]);
                return jtoken.ToObject(methodParamTypes[i]);
            }).ToArray();

            var ret = method.DynamicInvoke(convertedParameters);
            call<NoReturn>(queryCode, id, new object[]{ret});
        }
        catch (Exception e) when (e is TargetParameterCountException || e is ArgumentException || e is JsonSerializationException) {
            var paramsTypes = parameters.Select(e => e.GetType().Name).ToString();
            var paramsTypesJoined = String.Join(",", paramsTypes);

            call<object>(INVALID_PARAMETER_EXCEPTION, id, new object[]{id, e.Message});
            Console.Error.WriteLine(e.ToString());

            throw new WarningException($"Received an invalid parameter for {getEnumFromInt(queryCode).ToString()}. " +
                                       $"Given params count: {parameters.Length}. Given params type: {paramsTypesJoined}", e);
        }
    }


    private void raiseQueryCodeErrorToOtherParty(int queryCode, long requestId) {
        var errMsg = $"Query code {queryCode} is not a valid or registered code. Related req ID: {requestId}";
        call<object>(INVALID_QUERY_CODE, requestId, new object[]{requestId, errMsg});
        throw new WarningException($"Received an invalid queryCode: {queryCode}");
    }
    private void raiseInvalidReturnTypeErrorToOtherParty<R>(int queryCode, long requestId, object o, TaskCompletionSource<R> completionSource) {
        call<object>(INVALID_RETURN_TYPE_EXCEPTION, requestId,
            new object[]{requestId,
                $"Given type is {o.GetType().Name}, but expected {typeof(R).Name}. " +
                $"Related query: {getEnumFromInt(queryCode).ToString()}. Related req ID: {requestId}"});
        completionSource.SetException(new InvalidCastException("Fail when casting the return type"));
    }



    #region delegates

    public event Logging onLog {
        add => _interprocessCommunication.onLog += value;
        remove => _interprocessCommunication.onLog -= value;
    }

    public event WaitingForClient onWaitingForClient {
        add => _interprocessCommunication.onWaitingForClient += value;
        remove => _interprocessCommunication.onWaitingForClient -= value;
    }

    public event Connected onConnectedToClient {
        add => _interprocessCommunication.onConnectedToClient += value;
        remove => _interprocessCommunication.onConnectedToClient -= value;
    }

    public event Disconnected onDisconnected {
        add => _interprocessCommunication.onDisconnected += value;
        remove => _interprocessCommunication.onDisconnected -= value;
    }

    public event FailSendMessage onFailToSendMessage {
        add => _interprocessCommunication.onFailToSendMessage += value;
        remove => _interprocessCommunication.onFailToSendMessage -= value;
    }

    public event NewMessageNotification onReceiveMessage {
        add => _interprocessCommunication.onReceiveMessage += value;
        remove => _interprocessCommunication.onReceiveMessage -= value;
    }

    #endregion

    public async Task stopListeningAndDisconnect() {
        await _interprocessCommunication.stopListeningAndDisconnect();
    }
}


public class NoReturn
{
    public static NoReturn instance = new NoReturn();
}