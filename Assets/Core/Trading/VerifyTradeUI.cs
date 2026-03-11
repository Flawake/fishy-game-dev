using TradeSystem;
using UnityEngine;

public class VerifyTradeUI : MonoBehaviour
{
    public void VerifyTrade()
    {
        GetComponentInParent<Trading>().VerifyTrade(true);
        gameObject.SetActive(false);
    }

    public void DenyTrade()
    {
        GetComponentInParent<Trading>().DenyVerifyTrade();
        gameObject.SetActive(false);
    }
}
