using System;
using System.Text;
using UnityEngine; // for JsonUtility

[Serializable]
public class JwtPayload
{
    public string user_id;
    // Add other fields if needed
}

public static class JwtUtils
{
    public static string GetUuidFromJwt(string jwt)
    {
        // Split the JWT into parts
        string[] parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid JWT format");

        string payload = parts[1];

        // Convert base64url to base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
            case 0: break;
            default: throw new FormatException("Invalid base64url string!");
        }

        // Decode and parse the payload
        byte[] jsonBytes = Convert.FromBase64String(payload);
        string jsonString = Encoding.UTF8.GetString(jsonBytes);

        JwtPayload payloadData = JsonUtility.FromJson<JwtPayload>(jsonString);
        return payloadData.user_id;
    }
}