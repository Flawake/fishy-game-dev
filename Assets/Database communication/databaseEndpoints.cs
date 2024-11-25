public static class DatabaseEndpoints
{
    public static string serverAddress = "https://fishydatabase.djoamersfoort.nl/";
    //public static string serverAddress = "http://127.0.0.1:8080/";
    public static string databaseAccessToken = "t";
    //TODO: make this /account/...
    public static string loginEndpoint = serverAddress + "login";
    public static string registerEndpoint = serverAddress + "register";
    //TODO: make this /inventory/...
    public static string getInventoryEndpoint = serverAddress + "getAllPlayerData";
    public static string addNewItemEndpoint = serverAddress + "addNewItem";
    public static string addExistingItemEndpoint = serverAddress + "addExistingItem";
    public static string removeItemEndpoint = serverAddress + "removeItem";
    public static string reduceItemEndpoint = serverAddress + "reduceItem";
    //TODO: make this /stats/...
    public static string addStatEndpoint = serverAddress + "increaseStat";
    public static string selectOtherItemEndpoint = serverAddress + "selectItem";
}
