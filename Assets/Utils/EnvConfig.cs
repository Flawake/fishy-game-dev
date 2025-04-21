using System;

// "https://fishydatabase.djoleden.nl/", "http://127.0.0.1:8080/"
public static class EnvConfig
{
    public static string DatabaseAccessServer = "";
    public static string DatabaseAccessToken = "";
    public static ushort Port = 0;
    public static ushort ClientPort = 0;
    
    public static void LoadEnv()
    {
        #if UNITY_EDITOR
            DatabaseAccessServer = "https://fishydatabase.djoleden.nl/";
            DatabaseAccessToken = "t";
            Port = 25568;
            ClientPort = 25569;
        #else
            DatabaseAccessServer = Environment.GetEnvironmentVariable("DATABASE_ACCESS_SERVER");
            DatabaseAccessToken = Environment.GetEnvironmentVariable("DATABASE_ACCESS_TOKEN");
            Port = Environment.GetEnvironmentVariable("PORT");
            ClientPort = Environment.GetEnvironmentVariable("CLIENT_PORT");
        #endif
    }
}
