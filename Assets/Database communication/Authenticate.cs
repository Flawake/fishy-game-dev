using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
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

    private void Start()
    {
        // Register event handler for password field changes instead of checking in Update()
        if (RegisterPasswordField != null)
        {
            RegisterPasswordField.onValueChanged.AddListener(OnPasswordFieldChanged);
            // Initialize strength indicator with current password value
            UpdatePasswordStrengthIndicator(RegisterPasswordField.text);
        }
    }

    private void OnDestroy()
    {
        // Clean up event listener
        if (RegisterPasswordField != null)
        {
            RegisterPasswordField.onValueChanged.RemoveListener(OnPasswordFieldChanged);
        }
    }

    /// <summary>
    /// Updates password strength indicator when password field changes
    /// </summary>
    /// <param name="password">Current password value</param>
    private void OnPasswordFieldChanged(string password)
    {
        UpdatePasswordStrengthIndicator(password);
    }

    /// <summary>
    /// Calculates and displays password strength based on complexity
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    private void UpdatePasswordStrengthIndicator(string password)
    {
        if (passStrengthSlider == null)
            return;

        float strengthConst = 10f;
        float regexCount = Regex.Matches(password, PASSWORD_STRENGTH_REGEX).Count;
        passStrengthSlider.value = Mathf.Clamp(regexCount / strengthConst, passStrengthSlider.minValue, passStrengthSlider.maxValue);
        
        Color strengthColor = new Color(1, 0, 0);
        if (regexCount <= strengthConst / 2)
        {
            strengthColor.r = 1;
            strengthColor.g = regexCount / (strengthConst / 2) - (1 / (strengthConst / 2));
        }
        else
        {
            strengthColor.r = 1 - (regexCount / strengthConst - (1 / (strengthConst / 2)));
            strengthColor.g = 1;
        }
        
        Transform fillTransform = passStrengthSlider.transform.Find("Fill Area")?.Find("Fill");
        if (fillTransform != null)
        {
            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = strengthColor;
            }
        }
    }

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
}
