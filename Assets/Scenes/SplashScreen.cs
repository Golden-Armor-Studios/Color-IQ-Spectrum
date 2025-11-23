using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

public class SlashScreen : MonoBehaviour
{
    private SpriteRenderer splashRenderer;
    [SerializeField] private string resourceSpritePath = "Splash/golden-armor-splashscreen";
    [SerializeField] private string nextSceneName = "StartScreen";
    [SerializeField] private float displayDuration = 5f;

    private void Start()
    {
        FirebaseInitializer.InitializeAsync();

        if (splashRenderer == null)
        {
            Debug.LogWarning("Splash renderer not assigned; attempting to locate one in the same object or children.");
            splashRenderer = GetComponent<SpriteRenderer>();
        }

        if (splashRenderer == null)
        {
            splashRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (splashRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found for splash screen.");
        }
        else
        {
            EnsureSpriteAssigned();

            FitSpriteToCamera();
        }

        StartCoroutine(ShowSplashThenLoad());
    }

    private System.Collections.IEnumerator ShowSplashThenLoad()
    {
        yield return new WaitForSeconds(displayDuration);
        SceneManager.LoadScene(nextSceneName);
    }

    private void FitSpriteToCamera()
    {
        if (splashRenderer == null || splashRenderer.sprite == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null || cam.orthographic == false)
        {
            return;
        }

        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * cam.aspect;

        const float marginPixels = 30f;
        float pixelsPerUnit = cam.pixelHeight / worldScreenHeight;
        float marginWorld = marginPixels / pixelsPerUnit;

        worldScreenWidth = Mathf.Max(0.01f, worldScreenWidth - marginWorld * 2f);
        worldScreenHeight = Mathf.Max(0.01f, worldScreenHeight - marginWorld * 2f);

        Vector2 spriteSize = splashRenderer.sprite.bounds.size;
        float scale = Mathf.Max(worldScreenWidth / spriteSize.x, worldScreenHeight / spriteSize.y);

        Vector3 targetScale = new Vector3(scale, scale, 1f);
        splashRenderer.transform.localScale = targetScale;
    }

    private void EnsureSpriteAssigned()
    {
        if (splashRenderer.sprite != null)
        {
            Debug.Log("[SplashScreen] Sprite already assigned on renderer.");
            return;
        }

        if (string.IsNullOrEmpty(resourceSpritePath))
        {
            Debug.LogWarning("No resource path configured for splash sprite.");
            return;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(resourceSpritePath);
        if (loadedSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourceSpritePath);
            if (sprites != null && sprites.Length > 0)
            {
                loadedSprite = sprites[0];
            }
        }

        if (loadedSprite == null)
        {
            loadedSprite = Resources.Load<Sprite>(resourceSpritePath + "_0");
        }

        if (loadedSprite == null)
        {
            Sprite[] allInFolder = Resources.LoadAll<Sprite>("Splash");
            if (allInFolder != null && allInFolder.Length > 0)
            {
                loadedSprite = allInFolder[0];
                Debug.LogWarning("[SplashScreen] Falling back to first sprite found in Resources/Splash/");
            }
        }

        if (loadedSprite != null)
        {
            splashRenderer.sprite = loadedSprite;
            splashRenderer.enabled = true;
            EnsureValidMaterial();
            Debug.Log($"[SplashScreen] Loaded splash sprite '{loadedSprite.name}'.");
        }
        else
        {
            Debug.LogError($"[SplashScreen] Unable to load splash sprite at Resources/{resourceSpritePath}. Check that the image is in Resources and set to Sprite (Single).");
        }
    }

    private void EnsureValidMaterial()
    {
        if (splashRenderer == null)
        {
            return;
        }

        var currentMaterial = splashRenderer.sharedMaterial;
        bool needsMaterial = currentMaterial == null || currentMaterial.shader == null || currentMaterial.shader.name == "Hidden/InternalErrorShader";

        if (!needsMaterial)
        {
            return;
        }

#if UNITY_RENDER_PIPELINE_UNIVERSAL
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset != null && urpAsset.default2DMaterial != null)
        {
            splashRenderer.sharedMaterial = urpAsset.default2DMaterial;
            return;
        }
#endif

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            splashRenderer.sharedMaterial = new Material(spriteShader);
        }
        else
        {
            Debug.LogWarning("[SplashScreen] Could not locate 'Sprites/Default' shader. Sprite may still appear pink.");
        }
    }
}
