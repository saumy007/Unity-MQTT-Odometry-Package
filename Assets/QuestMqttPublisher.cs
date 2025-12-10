using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.XR;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class QuestMqttPublisher : M2MqttUnityClient
{
    [SerializeField]
    TMP_InputField TMP_InputField;

    [SerializeField] Button connectButton;

    private TouchScreenKeyboard keyboard;

    [Header("MQTT Settings")]
    [SerializeField] private string topic = "test";

    [Header("mDNS Continuous Discovery")]
    [SerializeField] private float mdnsRetryDelay = 1.0f;  // Time between scans (seconds)

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI ipText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI poseText;

    [Header("Publish Settings")]
    [SerializeField] private float publishInterval = 0.1f;

    private InputDevice headset;
    private float nextPublishTime = 0f;
    private bool discoveryRunning = false;

    // ----------------------------------------------------------
    // START
    // ----------------------------------------------------------
    private void Start()
    {
        string questIP = GetLocalIPAddress();
        if (ipText != null)
            ipText.text = "Quest IP: " + questIP;

        UpdateStatus("Starting continuous mDNS discovery...");

        // Start infinite discovery loop
        connectButton.onClick.AddListener(() => {
            StartContinuousDiscovery();
            connectButton.gameObject.SetActive(false);
            });
        TryFindHeadset();
        
        TMP_InputField.onSelect.AddListener(OpenKeyboard);
    }

    void OpenKeyboard(string text)
    {
        keyboard = TouchScreenKeyboard.Open(TMP_InputField.text, TouchScreenKeyboardType.Default);
    }

    // ----------------------------------------------------------
    // Continuous mDNS Scanner (runs forever)
    // ----------------------------------------------------------
    private async void StartContinuousDiscovery()
    {
        if (discoveryRunning) return;   // Prevent duplicates
        discoveryRunning = true;

        string ip = TMP_InputField.text;
        var port = 1883;

        if (!string.IsNullOrEmpty(ip))
        {
            brokerAddress = ip;
            brokerPort = (port != 0 ? port : 1883);

            Debug.Log($"[mDNS] Found MQTT Broker at {brokerAddress}:{brokerPort}");
            UpdateStatus($"Broker found at {brokerAddress}. Connecting...");

            base.Start(); // Connect to MQTT
        }
        else
        {
            Debug.Log("[mDNS] No broker found. Retrying...");
        }
        // while (true)  // Infinite loop
        // {
        //     // Only scan if not connected
        //     if (client == null || !client.IsConnected)
        //     {
        //         UpdateStatus("Scanning for MQTT broker via mDNS...");

        //         //(string ip, int port) = await SimpleMDNSLookup("_mqtt._tcp");
        //         string ip = TMP_InputField.text;
        //         port = 1883;

        //         if (!string.IsNullOrEmpty(ip))
        //         {
        //             brokerAddress = ip;
        //             brokerPort = (port != 0 ? port : 1883);

        //             Debug.Log($"[mDNS] Found MQTT Broker at {brokerAddress}:{brokerPort}");
        //             UpdateStatus($"Broker found at {brokerAddress}. Connecting...");

        //             base.Start(); // Connect to MQTT
        //         }
        //         else
        //         {
        //             Debug.Log("[mDNS] No broker found. Retrying...");
        //         }
        //     }

        //     await Task.Delay((int)(mdnsRetryDelay * 1000));
        // }
    }

    // ----------------------------------------------------------
    // Simple mDNS Lookup
    // ----------------------------------------------------------
    private async Task<(string, int)> SimpleMDNSLookup(string serviceType)
    {
        return await Task.Run(async () =>
        {
            try
            {
                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true;
                    client.MulticastLoopback = true;
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    client.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"));

                    byte[] query = BuildMDNSQuery(serviceType);
                    IPEndPoint mcast = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

                    await client.SendAsync(query, query.Length, mcast);

                    var result = await client.ReceiveAsync();
                    return ParseMDNSResponse(result.Buffer);
                }
            }
            catch
            {
                return (null, 0);
            }
        });
    }

    private byte[] BuildMDNSQuery(string service)
    {
        string[] labels = service.Split('.');
        byte[] buffer = new byte[512];
        int pos = 0;

        buffer[pos++] = 0; buffer[pos++] = 0;
        buffer[pos++] = 0; buffer[pos++] = 0;
        buffer[pos++] = 0; buffer[pos++] = 1;
        buffer[pos++] = 0; buffer[pos++] = 0;
        buffer[pos++] = 0; buffer[pos++] = 0;
        buffer[pos++] = 0; buffer[pos++] = 0;

        foreach (string label in labels)
        {
            buffer[pos++] = (byte)label.Length;
            foreach (char c in label)
                buffer[pos++] = (byte)c;
        }

        buffer[pos++] = 0;
        buffer[pos++] = 0; buffer[pos++] = 12;
        buffer[pos++] = 0; buffer[pos++] = 1;

        byte[] final = new byte[pos];
        System.Array.Copy(buffer, final, pos);
        return final;
    }

    private (string, int) ParseMDNSResponse(byte[] data)
    {
        string ip = null;
        int port = 0;

        for (int i = 0; i < data.Length - 4; i++)
        {
            if (data[i] == 0 && data[i + 1] == 1 && data[i + 2] == 0 && data[i + 3] == 4)
            {
                ip = $"{data[i + 4]}.{data[i + 5]}.{data[i + 6]}.{data[i + 7]}";
                break;
            }
        }

        for (int i = 0; i < data.Length - 2; i++)
        {
            if (data[i] == 0 && data[i + 1] == 33)
            {
                port = (data[i + 8] << 8) | data[i + 9];
                break;
            }
        }

        return (ip, port);
    }

    // ----------------------------------------------------------
    // UPDATE LOOP
    // ----------------------------------------------------------
    private void Update()
    {
        base.Update();

        if (!headset.isValid)
            TryFindHeadset();

        if (client != null && client.IsConnected && headset.isValid)
        {
            if (Time.time >= nextPublishTime)
            {
                PublishHeadsetData();
                nextPublishTime = Time.time + publishInterval;
            }
        }

        if (keyboard != null && keyboard.active && TMP_InputField.text != keyboard.text)
            TMP_InputField.text = keyboard.text;
    }

    private void TryFindHeadset()
    {
        var list = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, list);

        if (list.Count > 0)
            headset = list[0];
    }

    private void PublishHeadsetData()
    {
        if (!headset.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) ||
            !headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            if (poseText != null)
                poseText.text = "Pose not available.";

            return;
        }

        float x = pos.z;
        float y = -pos.x;
        float z = pos.y;

        Quaternion coordChange = Quaternion.Euler(90, 0, 0);
        Quaternion rosRot = coordChange * rot * Quaternion.Inverse(coordChange);

        string json = JsonUtility.ToJson(new HeadsetData(new Vector3(x, y, z), rosRot));

        
            
        poseText.text = json;
        
        client.Publish(topic, Encoding.UTF8.GetBytes(json),
            MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
    }

    // ----------------------------------------------------------
    // MQTT CALLBACKS
    // ----------------------------------------------------------
    protected override void OnConnected()
    {
        UpdateStatus("Connected to MQTT!");
        Debug.Log("[MQTT] Connected");
    }

    protected override void OnDisconnected()
    {
        UpdateStatus("Disconnected. mDNS scanning will continue.");
        Debug.Log("[MQTT] Disconnected");
    }

    protected override void OnConnectionFailed(string err)
    {
        UpdateStatus("Connection Failed: " + err);
        Debug.Log("[MQTT] Connection Failed: " + err);
    }

    protected override void OnConnectionLost()
    {
        UpdateStatus("Connection lost. Scanning will restart.");
        Debug.Log("[MQTT] Connection lost");
    }

    private void UpdateStatus(string msg)
    {
        if (statusText)
            statusText.text = msg;

        Debug.Log("[Quest] " + msg);
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            foreach (var ni in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ni.AddressFamily == AddressFamily.InterNetwork)
                    return ni.ToString();
        }
        catch { }
        return "Unknown IP";
    }

    [System.Serializable]
    public class HeadsetData
    {
        public Position position;
        public Rotation rotation;

        [System.Serializable]
        public class Position { public float x, y, z; public Position(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } }

        [System.Serializable]
        public class Rotation { public float x, y, z, w; public Rotation(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }

        public HeadsetData(Vector3 pos, Quaternion rot)
        {
            position = new Position(pos.x, pos.y, pos.z);
            rotation = new Rotation(rot.x, rot.y, rot.z, rot.w);
        }
    }
}
