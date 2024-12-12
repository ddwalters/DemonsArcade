using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PixelationEffect : MonoBehaviour
{
    public Camera cam;

    public RenderTexture RT;

    public RawImage image;

    public bool makePixelated = false;
    public bool makeNormal = false;

    private int originalWidth;
    private int originalHeight;
    private bool isTransitioning = false;
    private float transitionDuration = 0.3f; // Duration of each effect (pixelation/normalization) in seconds

    // Start is called before the first frame update
    void Start()
    {
        // Save the screen's resolution
        originalWidth = Screen.width;
        originalHeight = Screen.height;
    }

    // Update is called once per frame
    void Update()
    {
        if (makePixelated)
        {
            StartPixelationToNormal();
            makePixelated = false;
        }
        if (makeNormal)
        {
            SwapToNorm();
            makeNormal = false;
        }
    }

    [ContextMenu("make pixelated")]
    public void StartPixelationToNormal()
    {
        if (isTransitioning) return;

        // Start the sequence of pixelating and then normalizing
        StartCoroutine(PixelationToNormalCoroutine());
    }

    [ContextMenu("make normal")]
    public void SwapToNorm()
    {
        if (isTransitioning) return;

        StartCoroutine(NormalizationEffectCoroutine());
    }

    private IEnumerator PixelationToNormalCoroutine()
    {
        isTransitioning = true;

        // First, pixelate the screen
        yield return StartCoroutine(PixelationEffectCoroutine());

        // Then, normalize it back
        yield return StartCoroutine(NormalizationEffectCoroutine());

        isTransitioning = false;
    }

    private IEnumerator PixelationEffectCoroutine()
    {
        int targetWidth = originalWidth / 7;
        int targetHeight = originalHeight / 7;

        float timeElapsed = 0f;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionDuration;

            // Lerp the RenderTexture size
            int currentWidth = (int)Mathf.Lerp(originalWidth, targetWidth, t);
            int currentHeight = (int)Mathf.Lerp(originalHeight, targetHeight, t);

            UpdateRenderTexture(currentWidth, currentHeight);

            yield return null;
        }

        // Ensure it finishes at the target size
        UpdateRenderTexture(targetWidth, targetHeight);
    }

    private IEnumerator NormalizationEffectCoroutine()
    {
        int targetWidth = originalWidth;
        int targetHeight = originalHeight;

        float timeElapsed = 0f;
        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionDuration;

            // Lerp the RenderTexture size back to the original
            int currentWidth = (int)Mathf.Lerp(originalWidth / 7, targetWidth, t);
            int currentHeight = (int)Mathf.Lerp(originalHeight / 7, targetHeight, t);

            UpdateRenderTexture(currentWidth, currentHeight);

            yield return null;
        }

        // Reset to normal rendering
        cam.targetTexture = null;
        image.enabled = false;
    }

    private void UpdateRenderTexture(int width, int height)
    {
        if (RT != null)
        {
            RT.Release();
        }
        RT = new RenderTexture(width, height, 24);
        RT.Create();

        image.texture = RT;
        cam.targetTexture = RT;
    }
}
