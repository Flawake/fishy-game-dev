public static class DatabaseEndpoints
{
    public static string serverAddress = EnvConfig.DatabaseAccessServer;
    public static string databaseAccessToken = EnvConfig.DatabaseAccessToken;
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
    public static string addFishStatEndpoint = serverAddress + "increaseFishStat";
    public static string selectOtherItemEndpoint = serverAddress + "selectItem";
    public static string adjustMoneyEndpoint = serverAddress + "adjustMoney";
    public static string addXPEndpoint = serverAddress + "addXP";
    public static string addPlaytime = serverAddress + "addPlaytime";
    
    public static string addMailEndpoint = serverAddress + "addMail";
    public static string retreiveMailsEndpoint = serverAddress + "retreiveMails";
}
