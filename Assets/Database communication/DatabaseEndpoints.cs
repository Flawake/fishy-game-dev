public static class DatabaseEndpoints
{
    public static string serverAddress = EnvConfig.DatabaseAccessServer;
    public static string databaseAccessToken = EnvConfig.DatabaseAccessToken;
    
    public static string loginEndpoint = serverAddress + "login";
    public static string registerEndpoint = serverAddress + "account/register";
    
    public static string addNewItemEndpoint = serverAddress + "inventory/add";
    public static string addExistingItemEndpoint = serverAddress + "inventory/increase";
    public static string removeItemEndpoint = serverAddress + "inventory/destroy";
    public static string reduceItemEndpoint = serverAddress + "inventory/degrade";
    
    public static string addFishStatEndpoint = serverAddress + "stats/add_fish";
    public static string selectItemEndpoint = serverAddress + "stats/select_item";
    public static string changeCoinsEndpoint = serverAddress + "stats/change_coins";
    public static string changeBucksEndpoint = serverAddress + "stats/change_bucks";
    public static string addXPEndpoint = serverAddress + "stats/add_xp";
    public static string addPlaytime = serverAddress + "stats/add_playtime";
    
    public static string addMailEndpoint = serverAddress + "mail/create";
    public static string readMailEndpoint = serverAddress + "mail/change_read_state";
    
    public static string getPlayerDataEndpoint = serverAddress + "data/retreive_all_playerdata";
}
