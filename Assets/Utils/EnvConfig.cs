using System;
using UnityEngine;

public static class EnvConfig
{
    public static string DatabaseAccessServer = "https://127.0.0.1:8000/";
    public static ushort Port = 24469;
    public static ushort ClientPort = 443;

    public static void LoadEnv()
    {
#if UNITY_EDITOR
        DatabaseAccessServer = "http://127.0.0.1:8000/";
        Port = 24468;
        ClientPort = 24469;
#elif !UNITY_WEBGL
            DatabaseAccessServer = Environment.GetEnvironmentVariable("DATABASE_ACCESS_SERVER");
            Port = ushort.Parse(Environment.GetEnvironmentVariable("SERVER_PORT"));
            ClientPort = ushort.Parse(Environment.GetEnvironmentVariable("CLIENT_PORT"));
#endif
    }
}
