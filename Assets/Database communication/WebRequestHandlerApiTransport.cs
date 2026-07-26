using System;
using System.Text;
using FishyGame.Api;

/// <summary>
/// Binds the generated API client to the existing WebRequestHandler pump.
///
/// This file is HAND-WRITTEN. The generator never touches it, so this is where
/// anything game-specific belongs: base URL, auth headers, retries, logging.
///
/// Assign it once, on the Mirror server, before any generated call runs:
///
///     ApiConfig.Transport = new WebRequestHandlerApiTransport(EnvConfig.DatabaseAccessServer);
///
/// Per-call state (NetworkConnectionToClient, GameObjects, ...) is not threaded
/// through the transport. Capture it in the callback closure at the call site
/// instead, which is why the generated methods never mention Mirror.
/// </summary>
public class WebRequestHandlerApiTransport : IApiTransport
{
    private readonly string baseUrl;

    public WebRequestHandlerApiTransport(string serverAddress)
    {
        if (string.IsNullOrEmpty(serverAddress))
        {
            throw new ArgumentException(
                "serverAddress is empty. Check EnvConfig.DatabaseAccessServer.",
                nameof(serverAddress));
        }

        // Generated paths are server-absolute ("/auth/login") while
        // DatabaseAccessServer ends in a slash, so trim to avoid "//auth/login".
        baseUrl = serverAddress.TrimEnd('/');
    }

    public void Send(ApiRequest request, Action<ApiRawResponse> callback)
    {
        // WebRequestHandler hardcodes POST. Every operation in the spec is a
        // POST today, so this only fires if the backend grows a GET/PUT.
        if (request.Method != "POST")
        {
            callback(ApiRawResponse.Failed(
                0, "WebRequestHandler only supports POST, got " + request.Method));
            return;
        }

        // Endpoints without a request body still need a valid JSON payload.
        byte[] body = Encoding.UTF8.GetBytes(request.Body ?? "{}");

        WebRequestHandler.SendWebRequest(baseUrl + request.Path, body, response =>
        {
            switch (response.EndRequestReason)
            {
                case WebRequestHandler.RequestEndReason.success:
                    // WebRequestHandler does not surface the HTTP status code, so
                    // 200 is inferred from UnityWebRequest reporting success.
                    callback(ApiRawResponse.Ok(200, response.ResponseData));
                    break;

                case WebRequestHandler.RequestEndReason.timeout:
                    callback(ApiRawResponse.Failed(0, "request timed out"));
                    break;

                default:
                    callback(ApiRawResponse.Failed(
                        0, "request failed: " + response.ResponseData));
                    break;
            }
        });
    }
}
