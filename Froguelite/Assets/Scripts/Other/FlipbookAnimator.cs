using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum FlipbookRenderType
{
    SpriteRenderer,
    UIImage
}

public enum FlipbookLoopMethod
{
    Loop,
    PingPong,
    Random,
    Once
}

public class FlipbookAnimator : MonoBehaviour
{

    // FlipbookAnimator is a simple script to animate a series of sprites like a flipbook.


    #region VARIABLES


    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float frameDuration = 0.5f;
    [SerializeField] private FlipbookLoopMethod loopMethod = FlipbookLoopMethod.Loop;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool autoClearSpriteOnCompletion = true;
    [SerializeField] private bool autoClearSpriteOnStart = false;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private FlipbookRenderType componentType;
    private float timer;
    private int currentSpriteIndex = 0;
    private bool movingForward = true;
    private bool animationCompleted = false;
    private bool isPlaying = false;
    private float storedAlpha = 1f; // Store the alpha value before clearing
    private bool isCleared = false; // Track if the sprite is currently cleared
    private Coroutine fadeCoroutine = null; // Track the current fade operation


    #endregion


    #region MONOBEHAVIOUR


    // Awake
    //-------------------------------------//
    void Awake()
    //-------------------------------------//
    {
        // Auto-detect component type
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiImage != null)
        {
            componentType = FlipbookRenderType.UIImage;
            // Store initial alpha value
            storedAlpha = uiImage.color.a;
            
            if (playOnStart && sprites.Length > 0)
            {
                uiImage.sprite = sprites[0];
            }
            else if (!playOnStart && autoClearSpriteOnStart)
            {
                ClearSprite(); // Use proper clear method
            }
        }
        else if (spriteRenderer != null)
        {
            componentType = FlipbookRenderType.SpriteRenderer;
            // Store initial alpha value
            storedAlpha = spriteRenderer.color.a;
            
            if (playOnStart && sprites.Length > 0)
            {
                spriteRenderer.sprite = sprites[0];
            }
            else if (!playOnStart && autoClearSpriteOnStart)
            {
                ClearSprite(); // Use proper clear method
            }
        }
        else
        {
            Debug.LogWarning("FlipbookAnimator: No Image or SpriteRenderer component found on " + gameObject.name);
        }

        // Initialize playing state based on playOnStart setting
        isPlaying = playOnStart;

    } // END Start


    // Update
    //-------------------------------------//
    void Update()
    //-------------------------------------//
    {
        HandleSwapSprites();

    } // END Update


    #endregion


    #region SPRITE SWAPPING


    // Sets the sprites to be used in the flipbook
    //-------------------------------------//
    public void SetSprites(Sprite[] newSprites, float frameDuration, FlipbookLoopMethod loopMethod)
    //-------------------------------------//
    {
        sprites = newSprites;
        this.frameDuration = frameDuration;
        this.loopMethod = loopMethod;
        currentSpriteIndex = 0;
        timer = 0f;
        animationCompleted = false;
        SetCurrentSprite();

    } // END SetSprites


    // Handles swapping sprites based on the frame duration
    //-------------------------------------//
    private void HandleSwapSprites()
    //-------------------------------------//
    {
        if (sprites.Length == 0 || !isPlaying) return;

        // Don't update if animation is completed for "Once" loop method
        if (loopMethod == FlipbookLoopMethod.Once && animationCompleted) return;

        timer += Time.deltaTime;

        if (timer >= frameDuration)
        {
            timer = 0f;

            if (loopMethod == FlipbookLoopMethod.PingPong)
            {
                HandlePingPongMovement();
            }
            else if (loopMethod == FlipbookLoopMethod.Random)
            {
                HandleRandomMovement();
            }
            else if (loopMethod == FlipbookLoopMethod.Once)
            {
                HandleOnceMovement();
            }
            else if (loopMethod != FlipbookLoopMethod.Once)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
            }

            SetCurrentSprite();
        }

    } // END HandleSwapSprites


    // Handles ping-pong movement through the sprite array
    //-------------------------------------//
    private void HandlePingPongMovement()
    //-------------------------------------//
    {
        if (sprites.Length <= 1) return;

        if (movingForward)
        {
            currentSpriteIndex++;
            if (currentSpriteIndex >= sprites.Length - 1)
            {
                currentSpriteIndex = sprites.Length - 1;
                movingForward = false;
            }
        }
        else
        {
            currentSpriteIndex--;
            if (currentSpriteIndex <= 0)
            {
                currentSpriteIndex = 0;
                movingForward = true;
            }
        }

    } // END HandlePingPongMovement


    // Handles random movement through the sprite array
    //-------------------------------------//
    private void HandleRandomMovement()
    //-------------------------------------//
    {
        if (sprites.Length <= 1) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, sprites.Length);
        } while (newIndex == currentSpriteIndex);

        currentSpriteIndex = newIndex;

    } // END HandleRandomMovement


    // Handles once movement through the sprite array (plays once and stops)
    //-------------------------------------//
    private void HandleOnceMovement()
    //-------------------------------------//
    {
        if (sprites.Length <= 1)
        {
            animationCompleted = true;
            if (autoClearSpriteOnCompletion)
            {
                ClearSprite(); // Clear sprite when animation completes
            }
            return;
        }

        currentSpriteIndex++;
        if (currentSpriteIndex >= sprites.Length)
        {
            currentSpriteIndex = sprites.Length - 1; // Stay on the last sprite
            animationCompleted = true;
            if (autoClearSpriteOnCompletion)
            {
                ClearSprite(); // Clear sprite when animation completes
            }
        }

    } // END HandleOnceMovement


    // Sets the current sprite on the appropriate component
    //-------------------------------------//
    private void SetCurrentSprite()
    //-------------------------------------//
    {
        if (sprites.Length == 0 || currentSpriteIndex >= sprites.Length) return;

        // Restore alpha if we were previously cleared
        if (isCleared)
        {
            RestoreAlpha();
        }

        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            uiImage.sprite = sprites[currentSpriteIndex];
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            spriteRenderer.sprite = sprites[currentSpriteIndex];
        }

    } // END SetCurrentSprite


    #endregion


    #region ANIMATION CONTROL


    // Plays the flipbook animation
    //-------------------------------------//
    public void Play(bool setFirstImmediate = false)
    //-------------------------------------//
    {
        isPlaying = true;

        if (setFirstImmediate)
        {
            // Set the first sprite immediately
            currentSpriteIndex = 0;
            SetCurrentSprite();
        }

    } // END Play


    // Pauses the flipbook animation
    //-------------------------------------//
    public void Pause()
    //-------------------------------------//
    {
        isPlaying = false;

    } // END Pause


    // Stops the flipbook animation and resets to the first frame
    //-------------------------------------//
    public void Stop()
    //-------------------------------------//
    {
        isPlaying = false;
        ResetAnimation();

    } // END Stop


    // Toggles the playing state of the animation
    //-------------------------------------//
    public void TogglePlayPause()
    //-------------------------------------//
    {
        if (isPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }

    } // END TogglePlayPause


    // Returns whether the animation is currently playing
    //-------------------------------------//
    public bool IsPlaying()
    //-------------------------------------//
    {
        return isPlaying;

    } // END IsPlaying


    // Returns whether the animation has completed (only relevant for "Once" loop method)
    //-------------------------------------//
    public bool IsCompleted()
    //-------------------------------------//
    {
        return animationCompleted;

    } // END IsCompleted


    // Resets the animation back to the beginning
    //-------------------------------------//
    public void ResetAnimation()
    //-------------------------------------//
    {
        currentSpriteIndex = 0;
        timer = 0f;
        movingForward = true;
        animationCompleted = false;
        SetCurrentSprite();

    } // END ResetAnimation


    // Sets the animation to play automatically on start
    //-------------------------------------//
    public void SetPlayOnStart(bool shouldPlayOnStart)
    //-------------------------------------//
    {
        playOnStart = shouldPlayOnStart;

    } // END SetPlayOnStart


    // Sets a random start offset for the animation then plays it
    // Returns the offset value that was used
    //-------------------------------------//
    public float SetRandomStartThenPlay()
    //-------------------------------------//
    {
        if (sprites.Length == 0) return 0f;

        // Calculate total animation duration
        float totalDuration = sprites.Length * frameDuration;
        
        // Generate random offset between 0 and total duration
        float randomOffset = Random.Range(0f, totalDuration);
        
        // Set the offset and play
        SetStartOffset(randomOffset);
        Play();
        
        return randomOffset;

    } // END SetRandomStartThenPlay


    // Sets a specific start offset for the animation
    //-------------------------------------//
    public void SetStartOffset(float offset)
    //-------------------------------------//
    {
        if (sprites.Length == 0) return;

        // Calculate total animation duration
        float totalDuration = sprites.Length * frameDuration;
        
        // Wrap the offset to stay within animation bounds
        offset = offset % totalDuration;
        if (offset < 0) offset += totalDuration;
        
        // Calculate which frame index to start at based on the offset
        int startFrameIndex = Mathf.FloorToInt(offset / frameDuration);
        
        // Calculate the timer offset within the current frame
        float frameTimeOffset = offset % frameDuration;
        
        // Ensure frame index is within bounds
        startFrameIndex = Mathf.Clamp(startFrameIndex, 0, sprites.Length - 1);
        
        // Set the starting frame
        currentSpriteIndex = startFrameIndex;
        
        // Set the timer to the offset within the frame
        timer = frameTimeOffset;
        
        // Reset animation state
        movingForward = true;
        animationCompleted = false;
        
        // Set the current sprite
        SetCurrentSprite();

    } // END SetStartOffset


    // Gets the current array of sprites used in the flipbook
    //-------------------------------------//
    public Sprite[] GetSprites()
    //-------------------------------------//
    {
        return sprites;

    } // END GetSprites


    // Gets the current frame duration
    //-------------------------------------//
    public float GetFrameDuration()
    //-------------------------------------//
    {
        return frameDuration;

    } // END GetFrameDuration


    // Clears the current sprite (sets to null so nothing renders)
    //-------------------------------------//
    public void ClearSprite()
    //-------------------------------------//
    {
        // Store current alpha before clearing
        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            storedAlpha = uiImage.color.a;
            Color color = uiImage.color;
            color.a = 0f; // Make fully transparent
            uiImage.color = color;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            storedAlpha = spriteRenderer.color.a;
            Color color = spriteRenderer.color;
            color.a = 0f; // Make fully transparent
            spriteRenderer.color = color;
        }

        isCleared = true;

    } // END ClearSprite


    #endregion


    #region ALPHA


    // Restores the alpha value that was stored before clearing
    //-------------------------------------//
    private void RestoreAlpha()
    //-------------------------------------//
    {
        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            Color color = uiImage.color;
            color.a = storedAlpha;
            uiImage.color = color;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = storedAlpha;
            spriteRenderer.color = color;
        }

        isCleared = false;

    } // END RestoreAlpha


    // Sets the alpha value of the sprite or UI image
    //-------------------------------------//
    public void SetAlpha(float alpha)
    //-------------------------------------//
    {
        // Clamp alpha between 0 and 1
        alpha = Mathf.Clamp01(alpha);

        // Update stored alpha if we're not currently cleared
        if (!isCleared)
        {
            storedAlpha = alpha;
        }

        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            Color color = uiImage.color;
            color.a = alpha;
            uiImage.color = color;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        // If we're setting alpha to 0, mark as cleared
        if (alpha == 0f)
        {
            isCleared = true;
        }
        else
        {
            isCleared = false;
        }

    } // END SetAlpha


    // Smoothly fades the alpha value over time
    //-------------------------------------//
    public void FadeAlpha(float targetAlpha, float duration)
    //-------------------------------------//
    {
        // Clamp target alpha between 0 and 1
        targetAlpha = Mathf.Clamp01(targetAlpha);
        
        // Stop any existing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // Start the fade coroutine
        fadeCoroutine = StartCoroutine(FadeAlphaCoroutine(targetAlpha, duration));

    } // END FadeAlpha


    // Coroutine that handles the alpha fading over time
    //-------------------------------------//
    private IEnumerator FadeAlphaCoroutine(float targetAlpha, float duration)
    //-------------------------------------//
    {
        // Get current alpha
        float currentAlpha = 0f;
        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            currentAlpha = uiImage.color.a;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            currentAlpha = spriteRenderer.color.a;
        }
        
        float startAlpha = currentAlpha;
        float elapsedTime = 0f;
        
        // Fade over duration
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Use smooth interpolation
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(newAlpha);
            
            yield return null;
        }
        
        // Ensure we reach the exact target value
        SetAlpha(targetAlpha);
        
        // Clear the coroutine reference
        fadeCoroutine = null;

    } // END FadeAlphaCoroutine


    #endregion


    #region SPRITE FLIPPING


    // Sets the X flip state of the sprite or UI image
    // true = not flipped (normal), false = flipped
    //-------------------------------------//
    public void SetFlipX(bool flip)
    //-------------------------------------//
    {
        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            // For UI Image, we need to modify the RectTransform scale
            RectTransform rectTransform = uiImage.rectTransform;
            Vector3 scale = rectTransform.localScale;
            scale.x = flip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            // For SpriteRenderer, use the built-in flipX property
            spriteRenderer.flipX = flip;
        }

    } // END SetFlipX


    // Sets the Y flip state of the sprite or UI image
    // true = not flipped (normal), false = flipped
    //-------------------------------------//
    public void SetFlipY(bool flip)
    //-------------------------------------//
    {
        if (componentType == FlipbookRenderType.UIImage && uiImage != null)
        {
            // For UI Image, we need to modify the RectTransform scale
            RectTransform rectTransform = uiImage.rectTransform;
            Vector3 scale = rectTransform.localScale;
            scale.y = flip ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            rectTransform.localScale = scale;
        }
        else if (componentType == FlipbookRenderType.SpriteRenderer && spriteRenderer != null)
        {
            // For SpriteRenderer, use the built-in flipY property
            spriteRenderer.flipY = flip;
        }

    } // END SetFlipY


    #endregion
    
    
} // END FlipbookAnimator.cs
