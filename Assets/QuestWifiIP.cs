using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;

public class QuestWifiIP : MonoBehaviour
{
    public TextMeshProUGUI textUI;

    void Start()
    {
        string ip = GetQuestWifiIP();
        Debug.Log("Device IP: " + ip);

        if (textUI != null)
            textUI.text = ip;
        else
            Debug.LogWarning("textUI is not assigned in the Inspector!");
    }

    public static string GetQuestWifiIP()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Running on Quest / Android device
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            if (wifiManager == null)
                return "WifiManager not available";

            AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
            int ipAddress = wifiInfo.Call<int>("getIpAddress");

            if (ipAddress == 0)
                return "No WiFi IP (wifi disabled?)";

            string ip =
                $"{(ipAddress & 0xff)}." +
                $"{(ipAddress >> 8 & 0xff)}." +
                $"{(ipAddress >> 16 & 0xff)}." +
                $"{(ipAddress >> 24 & 0xff)}";

            return "Quest IP: " + ip;
        }
        catch (System.Exception e)
        {
            return "Error: " + e.Message;
        }

#else
        // Running in Unity Editor or Windows/Mac/Linux Standalone
        return "Device IP: " + GetLocalIPAddress();
#endif
    }

    // Fallback function for PC/macOS/Linux/Editor
    private static string GetLocalIPAddress()
    {
        try
        {
            foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ni.AddressFamily == AddressFamily.InterNetwork)
                    return ni.ToString();
            }
        }
        catch { }

        return "No Local IP Found";
    }
}
