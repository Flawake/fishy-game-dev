using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-authenticators
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

public class PlayerAuthenticator : NetworkAuthenticator
{
    #region Messages

    [SerializeField] TMP_InputField usernameField;
    [SerializeField] TMP_InputField passwordField;

    [SerializeField] TMP_InputField registerUsernameField;
    [SerializeField] TMP_InputField registerPasswordField;
    [SerializeField] TMP_InputField registerEmailField;

    [SerializeField] Authenticate authBeginScript;

    readonly HashSet<NetworkConnection> connectionsPendingDisconnect = new HashSet<NetworkConnection>();

    public struct LoginRequestMessage : NetworkMessage {
        public string authUsername;
        public string authPassword;
    }

    public struct RegisterRequestMessage : NetworkMessage
    {
        //TODO: username should conform to regex: /^[_0-9a-zA-Z]*$/
        //TODO: email should conform to regex: /^[0-9a-zA-Z][-\._a-zA-Z0-9]*@([0-9a-zA-Z][-\._0-9a-zA-Z]*\.)+[a-zA-Z]{2,4}$/
        public string registerUsername;
        public string registerPassword;
        public string registerEmail;
    }

    public struct LoginResponseMessage : NetworkMessage {
        public int code;
    }

    public struct RegisterResponseMessage : NetworkMessage
    {
        public int code;
    }

    #endregion

    #region Server

    // RuntimeInitializeOnLoadMethod -> fast playmode without domain reload
    [UnityEngine.RuntimeInitializeOnLoadMethod]
    static void ResetStatics()
    {
        GameNetworkManager.connNames.Clear();
    }

    /// <summary>
    /// Called on server from StartServer to initialize the Authenticator
    /// <para>Server message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartServer()
    {
        // register a handler for the authentication request we expect from client
        NetworkServer.RegisterHandler<LoginRequestMessage>(OnAuthRequestMessage, false);
        NetworkServer.RegisterHandler<RegisterRequestMessage>(OnRegisterRequestMessage, false);
    }

    /// <summary>
    /// Called on server from OnServerConnectInternal when a client needs to authenticate
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    /// <summary>
    /// Called on server when the client's AuthRequestMessage arrives
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    /// <param name="msg">The message payload</param>
    public void OnAuthRequestMessage(NetworkConnectionToClient conn, LoginRequestMessage msg)
    {
        if (connectionsPendingDisconnect.Contains(conn))
        {
            return;
        }

        string username = msg.authUsername;
        string password = msg.authPassword;

        if (GameNetworkManager.connNames.ContainsValue(username))
        {
            connectionsPendingDisconnect.Add(conn);

            // create and send msg to client so it knows to disconnect
            LoginResponseMessage authResponseMessage = new LoginResponseMessage
            {
                code = 200,
            };

            conn.Send(authResponseMessage);

            // must set NetworkConnection isAuthenticated = false
            conn.isAuthenticated = false;

            // disconnect the client after 1 second so that response message gets delivered
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }

        GameNetworkManager.connNames.Add(conn, username);

        WWWForm loginForm = new WWWForm();
        loginForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        loginForm.AddField("user", username);
        loginForm.AddField("password", password);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.loginEndpoint, loginForm, conn, EndLoginRequestMessage);
    }

    void OnRegisterRequestMessage(NetworkConnectionToClient conn, RegisterRequestMessage msg)
    {
        if (connectionsPendingDisconnect.Contains(conn))
        {
            return;
        }

        string username = msg.registerUsername;
        string password = msg.registerPassword;
        string email = msg.registerEmail;

        GameNetworkManager.connNames.Add(conn, username);

        WWWForm registerForm = new WWWForm();
        registerForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        registerForm.AddField("user", username);
        registerForm.AddField("password", password);
        registerForm.AddField("email", email);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.registerEndpoint, registerForm, conn, EndRegisterRequestMessage);
    }

    void EndLoginRequestMessage(WebRequestHandler.ResponseMessageData response)
    {
        EndAuthRequestMessage(response, true);
    }

    void EndRegisterRequestMessage(WebRequestHandler.ResponseMessageData response)
    {
        EndAuthRequestMessage(response, false);
    }

    void EndAuthRequestMessage(WebRequestHandler.ResponseMessageData response, bool isLogin)
    {
        NetworkConnectionToClient conn = response.Connection;

        if (response.EndRequestReason == WebRequestHandler.RequestEndReason.success)
        {
            try
            {
                AuthResponse loginResponse = JsonUtility.FromJson<AuthResponse>(response.ResponseData);
                Debug.Log($"loginResponse code = {loginResponse.code}");
                if (loginResponse.code == 0)
                {
                    Guid playerUuid = Guid.Parse(loginResponse.uuid);
                    GameNetworkManager.connUUID.Add(conn, playerUuid);
                    SendResponse(0, isLogin, conn);


                    // Accept the successful authentication
                    ServerAccept(conn);
                }
                else
                {
                    SendResponse(loginResponse.code, isLogin, conn);

                    // must set NetworkConnection isAuthenticated = false
                    conn.isAuthenticated = false;

                    // disconnect the client after 1 second so that response message gets delivered
                    StartCoroutine(DelayedDisconnect(conn, 1f));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);

                SendResponse(3, isLogin, conn);

                // must set NetworkConnection isAuthenticated = false
                conn.isAuthenticated = false;

                // disconnect the client after 1 second so that response message gets delivered
                StartCoroutine(DelayedDisconnect(conn, 1f));
            }
        }
        else
        {
            connectionsPendingDisconnect.Add(conn);

            // create and send msg to client so it knows to disconnect
            SendResponse(2, isLogin, conn);

            // must set NetworkConnection isAuthenticated = false
            conn.isAuthenticated = false;

            // disconnect the client after 1 second so that response message gets delivered
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }
    }

    void SendResponse(int _code, bool isLogin, NetworkConnectionToClient conn)
    {
        if (isLogin)
        {
            LoginResponseMessage authResponseMessage = new LoginResponseMessage
            {
                code = _code
            };

            conn.Send(authResponseMessage);
        }
        else
        {
            RegisterResponseMessage authResponseMessage = new RegisterResponseMessage
            {
                code = _code
            };

            conn.Send(authResponseMessage);
        }
    }

    IEnumerator DelayedDisconnect(NetworkConnectionToClient conn, float waitTime)
    {
        conn.Send(new DisconnectMessage
        {
            reason = ClientDisconnectReason.InvalidLoginCredentials,
            reasonText = "",
        });

        yield return new WaitForSeconds(waitTime);
        
            // Reject the unsuccessful authentication
        ServerReject(conn);

        yield return null;

        // remove conn from pending connections
        connectionsPendingDisconnect.Remove(conn);
    }

    #endregion

    #region Client

    /// <summary>
    /// Called on client from StartClient to initialize the Authenticator
    /// <para>Client message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartClient()
    {
        // register a handler for the authentication response we expect from server
        NetworkClient.RegisterHandler<LoginResponseMessage>(OnLoginResponseMessage, false);
        NetworkClient.RegisterHandler<RegisterResponseMessage>(OnRegisterResponseMessage, false);
    }

    /// <summary>
    /// Called on client from OnClientConnectInternal when a client needs to authenticate
    /// </summary>
    public override void OnClientAuthenticate()
    {
        if(authBeginScript.loggingIn) {
            string username = usernameField.text;
            string password = passwordField.text;

            //https://mirror-networking.gitbook.io/docs/manual/transports/websockets-transport/reverse-proxy
            //https://mirror-networking.gitbook.io/docs/manual/transports/encryption-transport
            LoginRequestMessage authRequestMessage = new LoginRequestMessage
            {
                authUsername = usernameField.text,
                authPassword = passwordField.text
            };

            //TODO: let Ronald look at this when I implemented security

            NetworkClient.Send(authRequestMessage);
        }
        else
        {
            string username = registerUsernameField.text;
            string password = registerPasswordField.text;
            string email = registerEmailField.text;

            //https://mirror-networking.gitbook.io/docs/manual/transports/websockets-transport/reverse-proxy
            //https://mirror-networking.gitbook.io/docs/manual/transports/encryption-transport
            Debug.LogWarning("Username and password are being send in plain text. This should be encrypted.");
            RegisterRequestMessage authRequestMessage = new RegisterRequestMessage
            {
                registerUsername = username,
                registerPassword = password,
                registerEmail = email, 
            };

            //TODO: let Ronald look at this when I implemented security

            NetworkClient.Send(authRequestMessage);
        }
    }

    /// <summary>
    /// Called on client when the server's AuthResponseMessage arrives
    /// </summary>
    /// <param name="msg">The message payload</param>
    public void OnLoginResponseMessage(LoginResponseMessage msg)
    {
        if (msg.code == 0)
        {
            //Debug.Log($"Authentication Response: {msg.message}");

            //Authentication has been accepted
            ClientAccept();
        }
        else
        {
            Debug.LogError($"Authentication Response: {msg.code}");

            // Authentication has been rejected
            ClientReject();
        }
    }

    public void OnRegisterResponseMessage(RegisterResponseMessage msg)
    {
        Debug.Log("Register response arrived");
        if (msg.code == 0)
        {
            Debug.Log($"msg.code == 0, Authentication Response: {msg.code}");

            //Authentication has been accepted
            ClientAccept();
        }
        else
        {
            Debug.LogError($"Authentication Response: {msg.code}");

            // Authentication has been rejected
            ClientReject();
        }
    }

    #endregion
}
