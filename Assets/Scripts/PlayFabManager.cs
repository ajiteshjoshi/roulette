using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;
public class PlayFabManager : MonoBehaviour
{

    public static PlayFabManager Instance;

    private string playFabId;

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    private string email;
    private string password;
    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {

        if(PlayerPrefs.GetString("Email") != null)
        {
            email = PlayerPrefs.GetString("Email");
            password = PlayerPrefs.GetString("Password");

            LoginWithEmail(email, password);
        }
        Login();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Register(string username, string password)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = username,
            Password = password,
            RequireBothUsernameAndEmail= false
            
        };
        PlayFabClientAPI.RegisterPlayFabUser(request,OnRegisterSuccess,OnError);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {

    }
    void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnSuccess, OnError);
    }


    void LoginButton()
    {
        email = emailInput.text;
        password = passwordInput.text;

        LoginWithEmail(email,password);

    }
    void LoginWithEmail(string email,string password)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnSuccess, OnError);
    }

    void LoginWithGoogle(string code)
    {
        var request = new LoginWithGoogleAccountRequest
        {
            CreateAccount = true,
            ServerAuthCode = code, 
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, OnSuccessGoogleLogin, OnError);
    }

    void OnSuccessGoogleLogin(LoginResult result)
    {

    }
    void OnSuccess(LoginResult result)
    {
        playFabId = result.PlayFabId;
        Debug.Log("PlayfabId " + playFabId);
        SceneManager.LoadScene("StartUp");
        GetMoneyInAccount();
    }

    void OnError(PlayFabError error)
    {

    }


    void GetMoneyInAccount()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventorySuccess, OnError);

    }

    void OnGetUserInventorySuccess(GetUserInventoryResult result)
    {
        int money = result.VirtualCurrency["MD"]; // MD is short MetaDoge that is Ser in PlayFab

        // assign Money here ;
    }

    public void CutMoney(int amount)
    {
        var request = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = "MD",
            Amount = amount
        };
        PlayFabClientAPI.SubtractUserVirtualCurrency(request, OnSubtractCoinsSuccess,OnError);
    }

    public void GrantMoney()
    {
        var request = new AddUserVirtualCurrencyRequest {
            VirtualCurrency = "MD",
            Amount = 50
        };
        PlayFabClientAPI.AddUserVirtualCurrency(request, OnGrantVirtualCurrencySuccess, OnError);
    }

    void OnGrantVirtualCurrencySuccess(ModifyUserVirtualCurrencyResult result)
    {
        Debug.Log("Money Added");
    }

    void OnSubtractCoinsSuccess(ModifyUserVirtualCurrencyResult result)
    {
        Debug.Log("Money is Dicted");
    }
}