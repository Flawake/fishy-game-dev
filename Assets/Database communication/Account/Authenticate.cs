using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Mirror;
using System.Linq;
using System.Text.RegularExpressions;

public class Authenticate : MonoBehaviour
{
    const float timeOutTime = 10.0f;
    public bool loggingIn = true;

    private const string USERNAME_REGEX = "[^0-9a-zA-Z_]";
    private const string PASSWORD_STRENGTH_REGEX = "(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{6,})";

    [SerializeField] GameObject loginPage;
    [SerializeField] GameObject registerPage;
    [SerializeField] Slider passStrengthSlider;

    [SerializeField] private TMP_Text loginStatusText;
    [SerializeField] private TMP_Text registerStatusText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_InputField loginUsernameField;
    [SerializeField] private TMP_InputField loginPasswordField;
    [SerializeField] private TMP_InputField RegisterUsernameField;
    [SerializeField] private TMP_InputField RegisterPasswordField;
    [SerializeField] private TMP_InputField emailField;

    [SerializeField] private NetworkManager manager;

    //Called from the sign in button
    public void OnLoginClick()
    {
        manager.StartClient();
    }

    public void OnRegisterClick()
    {
        manager.StartClient();
    }

    public void OnRegisterInsteadClick()
    {
        loginPage.SetActive(false);
        registerPage.SetActive(true);
        loggingIn = false;
    }

    public void OnLoginInsteadClick()
    {
        registerPage.SetActive(false);
        loginPage.SetActive(true);
        loggingIn = true;
    }

    bool checkPasswordAndUsername(string username, string password, TMP_Text statusText)
    {
        //username checks
        if (username.Length < 3)
        {
            statusText.text = "Username should be at least 3 characters long.";
            return false;
        }
        if (username.Length > 20)
        {
            statusText.text = "username should not be longer that 15 characters.";
            return false;
        }
        if (Regex.IsMatch(username, USERNAME_REGEX))
        {
            statusText.text = "Username should only contian letters, numbers and _";
            return false;
        }

        //password checks
        if (password.Length < 6)
        {
            statusText.text = "Password should be at least 6 characters long.";
            return false;
        }
        if (password.Length > 60)
        {
            statusText.text = "Password should not be longer that 60 characters.";
            return false;
        }
        return true;
    }

    private void Update()
    {
        //TODO: Don't do this in update but when the text field changes
        float strengthConst = 10f;
        float regexCount = Regex.Matches(RegisterPasswordField.text, PASSWORD_STRENGTH_REGEX).Count;
        passStrengthSlider.value = Mathf.Clamp(regexCount / strengthConst, passStrengthSlider.minValue, passStrengthSlider.maxValue);
        Color strengthColor = new Color(1, 0, 0);
        if (regexCount <= strengthConst / 2)
        {
            strengthColor.r = 1;
            strengthColor.g = regexCount / (strengthConst / 2) - (1 / (strengthConst / 2));
        }
        else
        {
            strengthColor.r = 1 - (regexCount / (strengthConst) - (1 / (strengthConst / 2)));
            strengthColor.g = 1;
        }
        passStrengthSlider.transform.Find("Fill Area").Find("Fill").GetComponent<Image>().color = strengthColor;
    }
}
