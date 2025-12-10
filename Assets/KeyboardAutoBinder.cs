using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyboardAutoBinder : MonoBehaviour
{
    [Header("Assign Buttons Here")]
    [SerializeField] private Button[] buttons;

    [Header("TMP Input Field Target")]
    [SerializeField] private TMP_InputField inputField;

    private void Start()
    {
        foreach (Button btn in buttons)
        {
            // Find the TMP text or normal text inside the button
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
            Text legacyText = btn.GetComponentInChildren<Text>();

            string key = "";

            if (tmp != null)
                key = tmp.text;
            else if (legacyText != null)
                key = legacyText.text;
            else
            {
                Debug.LogWarning("No text found in button: " + btn.name);
                continue;
            }

            // Cache the key for closure safety
            string capturedKey = key;

            // Add listener to button
            btn.onClick.AddListener(() => PressKey(capturedKey));
        }
    }

    private void PressKey(string key)
    {
        if (inputField == null) return;

        if (key == "Backspace")
        {
            if (inputField.text.Length > 0)
                inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
        else
        {
            inputField.text += key;
        }
    }
}
