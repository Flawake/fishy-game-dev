using TradeSystem;
using UnityEngine;

public class VerifyTradeUI : MonoBehaviour
{
    // Called from button in game
    public void VerifyTrade()
    {
        GetComponentInParent<Trading>().VerifyTrade(true);
        gameObject.SetActive(false);
    }

    // Called from button in game
    public void DenyTrade()
    {
        GetComponentInParent<Trading>().DenyVerifyTrade();
        gameObject.SetActive(false);
    }
}
