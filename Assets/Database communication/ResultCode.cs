using System.Collections.Generic;

public static class HttpResultTranslator
{
    private static Dictionary<int, string> codeToInt = new Dictionary<int, string>();

    static HttpResultTranslator()
    {
        codeToInt.Add(200, "OK");
        codeToInt.Add(401, "Could not find username password combination");
        codeToInt.Add(409, "Username or password was already taken");
        codeToInt.Add(500, "Internal server error");
        codeToInt.Add(502, "Bad gateway");
        codeToInt.Add(503, "Service unavailable");
        codeToInt.Add(504, "Gateway timeout");
    }

    public static string GetResponseAsString(int code)
    {
        codeToInt.TryGetValue(code, out var result);
        if (result == null)
        {
            return "Unknown error, please try again later";
        }
        return result;
    }
}
