using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class StackingGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int baseScore = 100;
    [SerializeField] private float comboMultiplier = 1.5f;
    [SerializeField] private int maxCombo = 10;
    [SerializeField] private float comboResetTime = 3f;

    [Header("Spawning")]
    [SerializeField] private GameObject[] stackablePrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float autoSpawnDelay = 1f;
    [SerializeField] private bool autoSpawn = true;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI heightText;
    [SerializeField] private Slider perfectMeter;
    [SerializeField] private GameObject comboPopup;
    [SerializeField] private TextMeshProUGUI comboPopupText;
    [SerializeField] private GameObject perfectPopup;

    [Header("Camera Effects")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem backgroundParticles;
    [SerializeField] private Light mainLight;
    [SerializeField] private Gradient lightColorGradient;
    [SerializeField] private AnimationCurve lightIntensityCurve;
    //[SerializeField] private PostProcessingProfile normalProfile;
    //[SerializeField] private PostProcessingProfile excitementProfile;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip perfectSound;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip[] ambientSounds;

    [Header("Tension System")]
    [SerializeField] private float tensionBuildRate = 0.1f;
    [SerializeField] private float tensionDecayRate = 0.05f;
    [SerializeField] private float maxTension = 1f;
    [SerializeField] private AnimationCurve tensionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Game State
    private int currentScore = 0;
    private int currentCombo = 0;
    private float currentHeight = 0f;
    private float currentTension = 0f;
    private bool isGameActive = true;
    private float lastActionTime;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private int objectsPlaced = 0;
    private int perfectPlacements = 0;

    // Stack tracking
    private List<GameObject> stackedObjects = new List<GameObject>();
    private float highestPoint = 0f;

    // Coroutines
    private Coroutine shakeCoroutine;
    private Coroutine comboResetCoroutine;
    private Coroutine tensionUpdateCoroutine;

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        originalCameraPos = mainCamera.transform.position;
        originalCameraRot = mainCamera.transform.rotation;
        lastActionTime = Time.time;

        UpdateUI();

        if (autoSpawn)
            StartCoroutine(AutoSpawnRoutine());

        tensionUpdateCoroutine = StartCoroutine(UpdateTensionSystem());

        // Start ambient audio
        PlayRandomAmbientSound();
    }

    IEnumerator AutoSpawnRoutine()
    {
        yield return new WaitForSeconds(1f); // Initial delay

        while (isGameActive)
        {
            if (CanSpawnNewObject())
            {
                SpawnNewObject();
                yield return new WaitForSeconds(autoSpawnDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // Check again soon
            }
        }
    }

    bool CanSpawnNewObject()
    {
        // Check if there's already an object being dragged
        JuicyDragAndDrop[] allDragObjects = FindObjectsByType<JuicyDragAndDrop>(FindObjectsSortMode.None);
        foreach (var obj in allDragObjects)
        {
            if (obj.gameObject.GetComponent<Rigidbody>().isKinematic)
                return false; // Something is being dragged
        }

        // Check if recent objects have settled
        return Time.time - lastActionTime > autoSpawnDelay * 0.5f;
    }

    void SpawnNewObject()
    {
        if (stackablePrefabs.Length == 0) return;

        // Choose object based on current height/difficulty
        GameObject prefabToSpawn = ChooseObjectPrefab();

        Vector3 spawnPos = spawnPoint.position + Vector3.up * spawnHeight;

        // Add slight random horizontal offset
        float randomOffset = 0.5f;
        spawnPos += new Vector3(
            Random.Range(-randomOffset, randomOffset),
            0,
            Random.Range(-randomOffset, randomOffset)
        );

        GameObject newObject = Instantiate(prefabToSpawn, spawnPos, Random.rotation);

        // Ensure it has the drag script
        if (newObject.GetComponent<JuicyDragAndDrop>() == null)
            newObject.AddComponent<JuicyDragAndDrop>();

        // Add to tracking
        stackedObjects.Add(newObject);

        // Visual spawn effect
        StartCoroutine(SpawnEffect(newObject));
    }

    GameObject ChooseObjectPrefab()
    {
        // Simple progression: more variety as height increases
        int availableObjects = Mathf.Min(stackablePrefabs.Length, 1 + (int)(currentHeight / 5f));
        return stackablePrefabs[Random.Range(0, availableObjects)];
    }

    IEnumerator SpawnEffect(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, tensionCurve.Evaluate(t));
            yield return null;
        }

        obj.transform.localScale = originalScale;
    }

    public void OnObjectGrabbed(JuicyDragAndDrop dragObject)
    {
        lastActionTime = Time.time;

        // Increase tension when grabbing
        ModifyTension(tensionBuildRate);

        // Stop combo reset timer
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
    }

    public void OnObjectReleased(JuicyDragAndDrop dragObject, PlacementQuality quality)
    {
        lastActionTime = Time.time;
        objectsPlaced++;

        // Calculate height
        float objectHeight = dragObject.transform.position.y;
        if (objectHeight > highestPoint)
        {
            highestPoint = objectHeight;
            currentHeight = highestPoint;
        }

        // Score calculation
        int scoreGain = CalculateScore(quality);
        currentScore += scoreGain;

        // Combo system
        UpdateComboSystem(quality);

        // Tension system
        float tensionChange = quality == PlacementQuality.Perfect ? -tensionDecayRate * 2f : tensionDecayRate;
        ModifyTension(tensionChange);

        // Visual and audio feedback
        ProcessPlacementFeedback(quality, scoreGain);

        // Update UI
        UpdateUI();

        // Start combo reset timer
        comboResetCoroutine = StartCoroutine(ComboResetTimer());

        // Check for milestones
        CheckMilestones();
    }

    int CalculateScore(PlacementQuality quality)
    {
        int score = baseScore;

        // Quality multiplier
        switch (quality)
        {
            case PlacementQuality.Perfect:
                score = (int)(score * 2f);
                perfectPlacements++;
                break;
            case PlacementQuality.Good:
                score = (int)(score * 1.5f);
                break;
        }

        // Combo multiplier
        if (currentCombo > 0)
        {
            float multiplier = 1f + (currentCombo * 0.1f);
            score = (int)(score * multiplier);
        }

        // Height bonus
        score += (int)(currentHeight * 10f);

        return score;
    }

    void UpdateComboSystem(PlacementQuality quality)
    {
        if (quality == PlacementQuality.Perfect)
        {
            currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
        }
        else if (quality == PlacementQuality.Good)
        {
            // Don't increase combo, but don't reset it immediately
        }
        else
        {
            // Reset combo on normal placement
            currentCombo = 0;
        }
    }

    void ProcessPlacementFeedback(PlacementQuality quality, int scoreGain)
    {
        // Camera shake based on quality
        float shakeAmount = quality == PlacementQuality.Perfect ? shakeIntensity * 1.5f : shakeIntensity;
        TriggerCameraShake(shakeAmount, shakeDuration);

        // Audio feedback
        switch (quality)
        {
            case PlacementQuality.Perfect:
                PlaySFX(perfectSound);
                if (currentCombo >= 3)
                    PlaySFX(comboSound);
                break;
            case PlacementQuality.Good:
                PlaySFX(comboSound, 0.7f);
                break;
        }

        // Visual popups
        if (quality == PlacementQuality.Perfect)
        {
            ShowPerfectPopup();
        }

        if (currentCombo >= 3)
        {
            ShowComboPopup();
        }

        // Particle effects
        TriggerPlacementParticles(quality);
    }

    void ShowPerfectPopup()
    {
        if (perfectPopup != null)
        {
            StartCoroutine(AnimatePopup(perfectPopup));
        }
    }

    void ShowComboPopup()
    {
        if (comboPopup != null && comboPopupText != null)
        {
            comboPopupText.text = $"COMBO x{currentCombo}!";
            StartCoroutine(AnimatePopup(comboPopup));
        }
    }

    IEnumerator AnimatePopup(GameObject popup)
    {
        popup.SetActive(true);
        Vector3 originalScale = popup.transform.localScale;

        // Pop in
        popup.transform.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            popup.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * 1.2f, tensionCurve.Evaluate(t));
            yield return null;
        }

        // Settle
        elapsed = 0f;
        duration = 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            popup.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }

        // Wait
        yield return new WaitForSeconds(1f);

        // Fade out
        elapsed = 0f;
        duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            popup.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        popup.SetActive(false);
    }

    void TriggerPlacementParticles(PlacementQuality quality)
    {
        if (backgroundParticles != null)
        {
            var emission = backgroundParticles.emission;
            var burst = emission.GetBurst(0);

            switch (quality)
            {
                case PlacementQuality.Perfect:
                    burst.count = 20;
                    break;
                case PlacementQuality.Good:
                    burst.count = 10;
                    break;
                default:
                    burst.count = 5;
                    break;
            }

            emission.SetBurst(0, burst);
            backgroundParticles.Emit((int)burst.count.constant);
        }
    }

    IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);

        if (currentCombo > 0)
        {
            currentCombo = 0;
            UpdateUI();
        }
    }

    IEnumerator UpdateTensionSystem()
    {
        while (isGameActive)
        {
            // Natural tension decay over time
            if (currentTension > 0)
            {
                currentTension = Mathf.Max(0, currentTension - (tensionDecayRate * Time.deltaTime));
            }

            // Update visual effects based on tension
            UpdateTensionEffects();

            yield return null;
        }
    }

    void ModifyTension(float amount)
    {
        currentTension = Mathf.Clamp(currentTension + amount, 0, maxTension);
    }

    void UpdateTensionEffects()
    {
        float tensionNormalized = currentTension / maxTension;
        float curvedTension = tensionCurve.Evaluate(tensionNormalized);

        // Update lighting
        if (mainLight != null)
        {
            mainLight.color = lightColorGradient.Evaluate(curvedTension);
            mainLight.intensity = lightIntensityCurve.Evaluate(curvedTension);
        }

        // Update music pitch/intensity
        if (musicSource != null)
        {
            musicSource.pitch = Mathf.Lerp(0.9f, 1.1f, curvedTension);
            musicSource.volume = Mathf.Lerp(0.6f, 0.8f, curvedTension);
        }

        // Update particle effects
        if (backgroundParticles != null)
        {
            var velocityOverLifetime = backgroundParticles.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(curvedTension * 2f);
        }

        // Update perfect meter UI
        if (perfectMeter != null)
        {
            perfectMeter.value = 1f - tensionNormalized; // Inverse - higher tension = lower "perfect" chance
        }
    }

    public void TriggerCameraShake(float intensity, float duration)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(CameraShake(intensity, duration));
    }

    IEnumerator CameraShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float shakeAmount = intensity * shakeCurve.Evaluate(1f - t);

            Vector3 randomOffset = Random.insideUnitSphere * shakeAmount;
            randomOffset.z = 0; // Keep camera on same Z plane for 2D-ish game

            mainCamera.transform.position = originalCameraPos + randomOffset;
            mainCamera.transform.rotation = originalCameraRot * Quaternion.Euler(
                Random.Range(-shakeAmount, shakeAmount) * 10f,
                Random.Range(-shakeAmount, shakeAmount) * 10f,
                0
            );

            yield return null;
        }

        // Return to original position
        mainCamera.transform.position = originalCameraPos;
        mainCamera.transform.rotation = originalCameraRot;
    }

    void CheckMilestones()
    {
        // Height milestones
        int heightMilestone = (int)(currentHeight / 5f) * 5;
        if (heightMilestone > 0 && heightMilestone % 10 == 0)
        {
            TriggerHeightMilestone(heightMilestone);
        }

        // Score milestones
        int scoreMilestone = (currentScore / 1000) * 1000;
        if (scoreMilestone > 0 && scoreMilestone % 5000 == 0)
        {
            TriggerScoreMilestone(scoreMilestone);
        }

        // Perfect placement streaks
        if (perfectPlacements > 0 && perfectPlacements % 5 == 0)
        {
            TriggerPerfectStreak(perfectPlacements);
        }
    }

    void TriggerHeightMilestone(int height)
    {
        PlaySFX(levelUpSound);
        TriggerCameraShake(shakeIntensity * 2f, shakeDuration * 2f);

        // Dramatic lighting change
        StartCoroutine(MilestoneFlash());

        Debug.Log($"Height Milestone: {height} units!");
    }

    void TriggerScoreMilestone(int score)
    {
        PlaySFX(levelUpSound, 1.2f);
        ModifyTension(-0.2f); // Release tension on big achievements

        Debug.Log($"Score Milestone: {score} points!");
    }

    void TriggerPerfectStreak(int streak)
    {
        PlaySFX(perfectSound, 1.5f);

        // Bonus score for streaks
        currentScore += streak * 50;

        Debug.Log($"Perfect Streak: {streak} in a row!");
    }

    IEnumerator MilestoneFlash()
    {
        if (mainLight == null) yield break;

        Color originalColor = mainLight.color;
        float originalIntensity = mainLight.intensity;

        // Flash bright
        mainLight.color = Color.white;
        mainLight.intensity = originalIntensity * 2f;

        yield return new WaitForSeconds(0.1f);

        // Return to normal
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainLight.color = Color.Lerp(Color.white, originalColor, t);
            mainLight.intensity = Mathf.Lerp(originalIntensity * 2f, originalIntensity, t);

            yield return null;
        }

        mainLight.color = originalColor;
        mainLight.intensity = originalIntensity;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore:N0}";

        if (comboText != null)
        {
            if (currentCombo > 0)
            {
                comboText.text = $"Combo: x{currentCombo}";
                comboText.color = Color.Lerp(Color.white, Color.yellow, (float)currentCombo / maxCombo);
            }
            else
            {
                comboText.text = "";
            }
        }

        if (heightText != null)
            heightText.text = $"Height: {currentHeight:F1}m";
    }

    void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = pitch + Random.Range(-0.1f, 0.1f);
            sfxSource.PlayOneShot(clip);
        }
    }

    void PlayRandomAmbientSound()
    {
        if (ambientSounds.Length > 0 && sfxSource != null)
        {
            AudioClip randomClip = ambientSounds[Random.Range(0, ambientSounds.Length)];
            sfxSource.PlayOneShot(randomClip, 0.3f);

            // Schedule next ambient sound
            Invoke(nameof(PlayRandomAmbientSound), Random.Range(10f, 30f));
        }
    }

    // Public methods for external access
    public int GetCurrentScore() => currentScore;
    public int GetCurrentCombo() => currentCombo;
    public float GetCurrentHeight() => currentHeight;
    public float GetCurrentTension() => currentTension;
    public int GetObjectsPlaced() => objectsPlaced;
    public int GetPerfectPlacements() => perfectPlacements;

    // Cheat methods for testing
    [ContextMenu("Add Score")]
    void CheatAddScore() => currentScore += 1000;

    [ContextMenu("Reset Game")]
    void CheatResetGame()
    {
        currentScore = 0;
        currentCombo = 0;
        currentHeight = 0f;
        objectsPlaced = 0;
        perfectPlacements = 0;
        UpdateUI();
    }
}