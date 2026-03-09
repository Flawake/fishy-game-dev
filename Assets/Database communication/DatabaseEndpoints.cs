public static class DatabaseEndpoints
{
    public static string serverAddress = EnvConfig.DatabaseAccessServer;
    public static string databaseAccessToken = EnvConfig.DatabaseAccessToken;
    
    public static string loginEndpoint = serverAddress + "auth/login";
    public static string registerEndpoint = serverAddress + "account/register";
    public static string buyItemEndpoint = serverAddress + "/shop/buy_item";
    public static string addNewItemEndpoint = serverAddress + "inventory/add";
    public static string removeItemEndpoint = serverAddress + "inventory/destroy";
    
    public static string addFishStatEndpoint = serverAddress + "stats/add_fish";
    public static string selectItemEndpoint = serverAddress + "stats/select_item";
    public static string addPlaytime = serverAddress + "stats/add_playtime";
    
    public static string addMailEndpoint = serverAddress + "mail/create";
    public static string readMailEndpoint = serverAddress + "mail/change_read_state";
    
    public static string createFriendRequestEndpoint = serverAddress + "friend/add_friend_request";
    public static string handleFriendRequestEndpoint = serverAddress + "friend/handle_request";
    public static string removeFriendEndpoint = serverAddress + "friend/remove_friend";
    
    public static string addActiveEffectEndpoint = serverAddress + "effects/add_effect";
    public static string removeExpiredEffectEndpoint = serverAddress + "effects/remove_expired";
    
    public static string getActiveCompetitionEndpoint = serverAddress + "competitions/active";
    public static string getUpcomingCompetitionsEndpoint = serverAddress + "competitions/upcoming";
    public static string submitCompetitionScoreEndpoint = serverAddress + "competitions/submit_score";
    
    public static string getPlayerDataEndpoint = serverAddress + "data/retreive_all_playerdata";
}
