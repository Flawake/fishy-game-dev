using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebRequestHandlerInterface : MonoBehaviour
{
    private void Update()
    {
        if (!NetworkServer.active)
        {
            return;
        }
        WebRequestHandler.CheckOngoingRequests();
    }
}

public static class WebRequestHandler
{
    // Define a delegate type for the callback function
    public delegate void WebRequestCallback(ResponseMessageData response);

    readonly static List<RequestMessageData> ongoingRequests = new List<RequestMessageData>();

    //time for a request to time out in seconds
    static readonly float timeOutTime = 10f;

    struct RequestMessageData
    {
        public NetworkConnectionToClient Connection { get; }
        public UnityWebRequest WebRequest { get; }
        public WebRequestCallback Callback { get; }
        public GameObject[] Objects;
        public float ElapsedTime { get; set; }

        public RequestMessageData(NetworkConnectionToClient conn, UnityWebRequest _webRequest, GameObject[] _objects, WebRequestCallback _callback)
        {
            Connection = conn;
            WebRequest = _webRequest;
            Callback = _callback;
            Objects = _objects;
            ElapsedTime = 0;
        }
    }

    public readonly struct ResponseMessageData
    {
        public string ResponseData { get; }
        public RequestEndReason EndRequestReason { get; }
        public NetworkConnectionToClient Connection { get; }
        public GameObject[] Objects { get; }

        public ResponseMessageData(string _responseData, RequestEndReason _endRequestReason, GameObject[] _objects, NetworkConnectionToClient _connection)
        {
            ResponseData = _responseData;
            EndRequestReason = _endRequestReason;
            Objects = _objects;
            Connection = _connection;
        }
    }

    public enum RequestEndReason
    {
        success,
        timeout,
        unknown,
    }

    public static void SendWebRequest(string uri, WWWForm data)
    {
        SendWebRequest(uri, data, null, null, null);
    }

    public static void SendWebRequest(string uri, WWWForm data, WebRequestCallback callback)
    {
        SendWebRequest(uri, data, null, null, callback);
    }

    public static void SendWebRequest(string uri, WWWForm data, NetworkConnectionToClient connection, WebRequestCallback callback)
    {
        SendWebRequest(uri, data, connection, null, callback);
    }

    public static void SendWebRequest(string uri, WWWForm data, NetworkConnectionToClient connection, GameObject[] _objects, WebRequestCallback callback)
    {
        UnityWebRequest webRequest = UnityWebRequest.Post(uri, data);
        webRequest.SendWebRequest();
        ongoingRequests.Add(new RequestMessageData(connection, webRequest, _objects, callback));
    }

    public static void CheckOngoingRequests()
    {
        //Reverse order because it does remove items inside the loop and will skip over items if done from the front
        for(int i = ongoingRequests.Count - 1; i >=0; i--)
        {
            RequestMessageData data = ongoingRequests[i];
            if(data.WebRequest.isDone)
            {
                HandleDoneRequest(data, i);
            }
            else
            {
                HandleInProgressRequest(data, i);
            }
        }
    }

    static void HandleInProgressRequest(RequestMessageData data, int index)
    {
        data.ElapsedTime = data.ElapsedTime += Time.deltaTime;
        ongoingRequests[index] = data;
        if (data.ElapsedTime > timeOutTime)
        {
            Debug.Log("Response took too long, took " + ongoingRequests[index].ElapsedTime + " seconds");
            data.Callback?.Invoke(new ResponseMessageData(data.WebRequest.downloadHandler.text, RequestEndReason.timeout, data.Objects, data.Connection));
            data.WebRequest.Dispose();
            ongoingRequests.RemoveAt(index);
        }
    }

    static void HandleDoneRequest(RequestMessageData data, int index) 
    {
        RequestEndReason reason;
        if (data.WebRequest.result == UnityWebRequest.Result.Success)
        {
            reason = RequestEndReason.success;
        }
        else
        {
            reason = RequestEndReason.unknown;
        }
        data.Callback?.Invoke(new ResponseMessageData(data.WebRequest.downloadHandler.text, reason, data.Objects, data.Connection));
        data.WebRequest.Dispose();
        ongoingRequests.RemoveAt(index);
    }
}
