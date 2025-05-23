using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class MessagePanel : MonoBehaviour
{
    public static MessagePanel Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Image messageIcon;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Icons")]
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite warningIcon;

    [Header("Colors")]
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color warningColor = new Color(0.8f, 0.2f, 0.2f);

    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Coroutine currentMessageCoroutine;

    public event Action<string, string> OnMessageShown;
    public event Action OnMessageHidden;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ValidateReferences();
        HideMessage();
    }

    private void ValidateReferences()
    {
        if (messageIcon == null)
            Debug.LogError($"[{nameof(MessagePanel)}] Message icon reference is missing!");
        if (headerText == null)
            Debug.LogError($"[{nameof(MessagePanel)}] Header text reference is missing!");
        if (messageText == null)
            Debug.LogError($"[{nameof(MessagePanel)}] Message text reference is missing!");
        if (successIcon == null)
            Debug.LogError($"[{nameof(MessagePanel)}] Success icon sprite is missing!");
        if (warningIcon == null)
            Debug.LogError($"[{nameof(MessagePanel)}] Warning icon sprite is missing!");
    }

    public void ShowSuccessMessage(string header, string message)
    {
        if (!ValidateMessageInput(header, message)) return;
        
        messageIcon.sprite = successIcon;
        headerText.color = successColor;
        ShowMessage(header, message);
    }

    public void ShowWarningMessage(string header, string message)
    {
        if (!ValidateMessageInput(header, message)) return;
        
        messageIcon.sprite = warningIcon;
        headerText.color = warningColor;
        ShowMessage(header, message);
    }

    private bool ValidateMessageInput(string header, string message)
    {
        if (string.IsNullOrEmpty(header))
        {
            Debug.LogWarning($"[{nameof(MessagePanel)}] Attempted to show message with empty header");
            return false;
        }
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[{nameof(MessagePanel)}] Attempted to show message with empty content");
            return false;
        }
        return true;
    }

    private void ShowMessage(string header, string message)
    {
        headerText.text = header;
        messageText.text = message;
        
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }
        
        currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine());
        OnMessageShown?.Invoke(header, message);
    }

    private IEnumerator ShowMessageCoroutine()
    {
        // Fade in
        yield return FadeCoroutine(0f, 1f, fadeInDuration);

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return FadeCoroutine(1f, 0f, fadeOutDuration);
        
        OnMessageHidden?.Invoke();
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    private void HideMessage()
    {
        canvasGroup.alpha = 0f;
        OnMessageHidden?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 