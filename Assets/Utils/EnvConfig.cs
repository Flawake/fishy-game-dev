using System;
using UnityEngine;

// "https://fishydatabase.djoleden.nl/", "http://127.0.0.1:8080/"
public static class EnvConfig
{
    public static string DatabaseAccessServer = "";
    public static string DatabaseAccessToken = "";
    public static ushort Port = 24468;
    public static ushort ClientPort = 24469;
    
    public static void LoadEnv()
    {
        #if UNITY_EDITOR
            DatabaseAccessServer = "https://fishydatabase.djoleden.nl/";
            DatabaseAccessToken = "t";
            Port = 24468;
            ClientPort = 24469;
        #elif !UNITY_WEBGL
            DatabaseAccessServer = Environment.GetEnvironmentVariable("DATABASE_ACCESS_SERVER");
            DatabaseAccessToken = Environment.GetEnvironmentVariable("DATABASE_ACCESS_TOKEN");
            Port = ushort.Parse(Environment.GetEnvironmentVariable("SERVER_PORT"));
            ClientPort = ushort.Parse(Environment.GetEnvironmentVariable("CLIENT_PORT"));
        #endif
    }
}
