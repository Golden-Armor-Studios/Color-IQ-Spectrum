using UnityEngine;
using System.Collections.Generic;

public class GameBoard
{
    public int GameBoardHeight;
    public int GameBoardWidth;

    public List<Color> colorList = new List<Color>();

    public Color selectedColor;

    public GameObject SelectedColorIndicator;

    private Color tempColor;

    public int score = 0;

    private int level;

    private Color levelColor;
    private GameObject CorrectPointObject;
    private GameObject InccorectPointObject;
    private GameObject LevelUpObject;
    private AudioSource CorrectPoint;
    private AudioSource IncorrectPoint;
    private AudioSource LevelUp;
    private GameObject VideoBackground;
    private SpriteRenderer VideoBackgroundSR;
    private float gamePieceSize;
    private int gamePieceCount;
    private float gamePieceRadius;
    public delegate void setTimeDelegate(float timeAdded);
    private readonly List<GameObject> spawnedPieces = new List<GameObject>();
    private int bonusTileIndex = -1;

    private const float CorrectPieceRatio = 0.15f;
    private const int MinimumCorrectPieces = 2;
    private const int LateGameReductionStartScore = 50;
    private const int LateGameReductionEndScore = 100;
    private const float MinHueContrast = 0.25f;
    private const float MaxHueContrast = 0.5f;
    private const float MaxPieceSize = 0.72f;
    private const float MinPieceSize = 0.34f;
    private const float PieceRadiusScale = 0.45f;
    private const float DifficultyRampSpan = 8f;

    private readonly GameController controller;

    public GameBoard (MonoBehaviour host) {
        bonusTileIndex = -1;
        GameBoardHeight = 800;
        GameBoardWidth = 600;
        level = 2;
        gamePieceCount = 40;
        controller = host as GameController;
        CorrectPointObject = GameObject.Find("DTSound-Correct_Point");
        InccorectPointObject = GameObject.Find("DTSound-Incorrect_Point");
        LevelUpObject = GameObject.Find("DTSound-Level-Up");
        CorrectPoint = CorrectPointObject.GetComponent<AudioSource>();
        IncorrectPoint = InccorectPointObject.GetComponent<AudioSource>();
        LevelUp = LevelUpObject.GetComponent<AudioSource>();
        VideoBackground = GameObject.Find("Video_Background");
        VideoBackgroundSR = VideoBackground.GetComponent<SpriteRenderer>();
        float worldScreenHeight = Camera.main.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
        float spriteWidth = VideoBackgroundSR.sprite.bounds.size.x;
        float spriteHeight = VideoBackgroundSR.sprite.bounds.size.y;
        float scaleX = worldScreenWidth / spriteWidth;
        float scaleY = worldScreenHeight / spriteHeight;
        VideoBackgroundSR.transform.localScale = new Vector3(scaleX, scaleY, 1);
        VideoBackgroundSR.transform.position = new Vector3(0, -5, 1);
    }

    public void buildGameBoard(bool isTheSameColor = false) {
        DestroySpawnedPieces();
        bonusTileIndex = -1;

        colorList = new List<Color>();
        bool usePastelPalette = ShouldUsePastelPalette(score);
        Color targetColor = isTheSameColor ? levelColor : GenerateTargetColor(usePastelPalette);
        levelColor = targetColor;

        ApplyDifficultyScaling(level, isTheSameColor);

        Rect playArea = CalculatePlayableArea();
        float stepX = gamePieceSize;
        float stepY = gamePieceSize;

        int columns = Mathf.Max(1, Mathf.FloorToInt(playArea.width / stepX));
        int rows = Mathf.Max(1, Mathf.FloorToInt(playArea.height / stepY));

        if (columns < 1) {
            columns = 1;
        }
        if (rows < 1) {
            rows = 1;
        }

        float totalWidth = gamePieceSize + (columns - 1) * stepX;
        float totalHeight = gamePieceSize + (rows - 1) * stepY;

        float startX = playArea.xMin + (playArea.width - totalWidth) * 0.5f + gamePieceSize * 0.5f;
        float startY = playArea.yMin + (playArea.height - totalHeight) * 0.5f + gamePieceSize * 0.5f;

        List<Vector3> gridPositions = new List<Vector3>(rows * columns);
        for (int row = 0; row < rows; row++) {
            for (int column = 0; column < columns; column++) {
                gridPositions.Add(new Vector3(
                    startX + column * stepX,
                    startY + row * stepY,
                    0f
                ));
            }
        }

        gamePieceCount = gridPositions.Count;

        int correctPiecesTarget = CalculateCorrectPieceTarget(gamePieceCount, isTheSameColor);

        HashSet<int> correctPieceIndices = isTheSameColor
            ? BuildSequentialIndexSet(gamePieceCount)
            : BuildRandomIndexSet(gamePieceCount, correctPiecesTarget);

        for (int index = 0; index < gamePieceCount; index++) {
            bool shouldUseTargetColor = isTheSameColor || correctPieceIndices.Contains(index);

            tempColor = shouldUseTargetColor
                ? targetColor
                : GenerateContrastingColor(targetColor, usePastelPalette);

            colorList.Add(tempColor);

            GameObject piece = new ColorGamePiece(gridPositions[index], tempColor, level, gamePieceSize, gamePieceRadius, false).Render();
            spawnedPieces.Add(piece);
        }

        selectedColor = targetColor;
        SelectedColorIndicator = GameObject.Find("SelectedColor");
        SpriteRenderer SelectedColorSR = SelectedColorIndicator.GetComponent<SpriteRenderer>();
        SelectedColorSR.color = selectedColor;
    }

    private void ApplyDifficultyScaling(int currentLevel, bool isUniformBoard) {
        float normalizedLevel = Mathf.Clamp01((currentLevel - 1f) / DifficultyRampSpan);

        float baseSize = Mathf.Lerp(MaxPieceSize, MinPieceSize, normalizedLevel);
        float boardModifier = isUniformBoard ? 0.95f : 1f;

        gamePieceSize = Mathf.Clamp(baseSize * boardModifier, MinPieceSize, MaxPieceSize);
        float calculatedRadius = gamePieceSize * PieceRadiusScale;
        gamePieceRadius = Mathf.Clamp(calculatedRadius, gamePieceSize * 0.35f, gamePieceSize * 0.5f);
    }

    private Rect CalculatePlayableArea() {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) {
            return new Rect(-GameBoardWidth * 0.5f, -GameBoardHeight * 0.5f, GameBoardWidth, GameBoardHeight);
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        float left = cameraPosition.x - halfWidth;
        float right = cameraPosition.x + halfWidth;
        float top = cameraPosition.y + halfHeight;
        float bottom = cameraPosition.y - halfHeight;

        float bottomLimit = bottom;

        GameObject indicator = GameObject.Find("SelectedColor");
        if (indicator != null) {
            SpriteRenderer indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
            if (indicatorRenderer != null) {
                bottomLimit = Mathf.Max(bottomLimit, indicatorRenderer.bounds.max.y);
            }
        }

        const float padding = 0.25f;
        const float topUiMargin = 1.5f;
        const float bottomUiMargin = 0.75f;
        float playableLeft = left + padding;
        float playableRight = right - padding;
        float playableTop = top - padding - topUiMargin;
        float playableBottom = bottomLimit + padding + bottomUiMargin;

        if (playableRight <= playableLeft) {
            playableRight = playableLeft + 1f;
        }
        if (playableTop <= playableBottom) {
            playableTop = playableBottom + 1f;
        }

        return Rect.MinMaxRect(playableLeft, playableBottom, playableRight, playableTop);
    }

    // Gradually shrink the pool of matching tiles once the player hits late-game levels.
    private int CalculateCorrectPieceTarget(int pieceCount, bool isUniformBoard) {
        if (pieceCount <= 0) {
            return 0;
        }

        if (isUniformBoard) {
            return pieceCount;
        }

        int baseCount = Mathf.Clamp(Mathf.RoundToInt(pieceCount * CorrectPieceRatio), MinimumCorrectPieces, pieceCount);
        if (score < LateGameReductionStartScore) {
            return baseCount;
        }

        float reductionSpan = Mathf.Max(1f, LateGameReductionEndScore - LateGameReductionStartScore);
        float normalizedProgress = Mathf.Clamp01((score - LateGameReductionStartScore) / reductionSpan);
        int rampedCount = Mathf.RoundToInt(Mathf.Lerp(baseCount, 1f, normalizedProgress));
        return Mathf.Clamp(rampedCount, 1, pieceCount);
    }

    public void addPoints(string name, RaycastHit2D hit, setTimeDelegate setTime) {
        GameObject touchedPiece = hit.collider != null ? hit.collider.gameObject : null;
        if (!IsGamePiece(touchedPiece)) {
            return;
        }

        bool isBonusPiece = touchedPiece.CompareTag("GamePiece_Level_2");
        bool touchedCorrect = selectedColor.ToColorKey() == touchedPiece.name;

        if (!isBonusPiece && !touchedCorrect) {
            ProcessIncorrectSelection();
            return;
        }

        touchedPiece.SetActive(false);
        if (isBonusPiece) {
            bonusTileIndex = -1;
        }

        if (level >= 2 && touchedCorrect) {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(hit.point, 1f);
            foreach (Collider2D collider in hitColliders) {
                if (!IsGamePiece(collider.gameObject)) {
                    continue;
                }

                if (selectedColor.ToColorKey() == collider.gameObject.name) {
                    collider.gameObject.SetActive(false);
                }
            }
        }

        CorrectPoint.Play();
        score++;
        if (isBonusPiece) {
            setTime(10.0f);
        }
        if (score % 10 == 0) {
            level++;
            LevelUp.Play();
            setTime(10.0f);
            System.Action rebuildLevelBoard = () => {
                VideoBackgroundSR.transform.position = new Vector3(0, -5, 1);
                removeGamePieces();
                buildGameBoard(true);
            };
            bool shouldShowLevelVideoAd = level == 10;
            if (controller != null && shouldShowLevelVideoAd) {
                controller.RequestRewardedAd(rebuildLevelBoard);
            } else {
                rebuildLevelBoard();
            }
        } else {
            VideoBackgroundSR.transform.position = new Vector3(0, VideoBackgroundSR.transform.position.y + 0.5f , 1);
            removeGamePieces();
            buildGameBoard();
        }
    }
        

    public bool SpawnBonusTile() {
        if (spawnedPieces.Count == 0) {
            return false;
        }

        if (bonusTileIndex >= 0) {
            GameObject existing = spawnedPieces[bonusTileIndex];
            if (existing != null && existing.activeSelf && existing.CompareTag("GamePiece_Level_2")) {
                return false;
            }
            bonusTileIndex = -1;
        }

        List<int> candidates = new List<int>();
        for (int i = 0; i < spawnedPieces.Count; i++) {
            GameObject piece = spawnedPieces[i];
            if (piece == null) {
                continue;
            }
            if (!piece.activeSelf) {
                continue;
            }
            if (piece.CompareTag("GamePiece_Level_2")) {
                continue;
            }
            candidates.Add(i);
        }

        if (candidates.Count == 0) {
            return false;
        }

        int targetIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        GameObject oldPiece = spawnedPieces[targetIndex];
        if (oldPiece == null) {
            return false;
        }

        Vector3 position = oldPiece.transform.position;
        Color pieceColor = selectedColor;

        Object.Destroy(oldPiece);

        GameObject bonusPiece = new ColorGamePiece(position, pieceColor, level, gamePieceSize, gamePieceRadius, true).Render();
        spawnedPieces[targetIndex] = bonusPiece;
        if (targetIndex < colorList.Count) {
            colorList[targetIndex] = pieceColor;
        }

        Animator animator = bonusPiece.GetComponent<Animator>();
        if (animator != null) {
            animator.SetTrigger("AddLevel2");
        }

        bonusTileIndex = targetIndex;
        return true;
    }

    public bool HasActiveBonusTile() {
        if (bonusTileIndex < 0) {
            return false;
        }
        if (bonusTileIndex >= spawnedPieces.Count) {
            bonusTileIndex = -1;
            return false;
        }
        GameObject piece = spawnedPieces[bonusTileIndex];
        if (piece == null || !piece.activeSelf) {
            bonusTileIndex = -1;
            return false;
        }
        return true;
    }


    public void removeGamePieces() {
        DestroySpawnedPieces();
    }

    public int getScore() {
        return score;
    }

    private void ProcessIncorrectSelection() {
        IncorrectPoint.Play();
        removeGamePieces();
        buildGameBoard();
    }

    private void DestroySpawnedPieces() {
        foreach (GameObject piece in spawnedPieces) {
            if (piece != null) {
                Object.Destroy(piece);
            }
        }
        spawnedPieces.Clear();
        bonusTileIndex = -1;
    }

    private bool IsGamePiece(GameObject obj) {
        return obj != null && (obj.CompareTag("GamePiece") || obj.CompareTag("GamePiece_Level_2"));
    }

    private bool ShouldUsePastelPalette(int currentScore) {
        if (currentScore <= 0) {
            return false;
        }
        return (currentScore / 5) % 2 == 1;
    }

    private Color GenerateTargetColor(bool usePastelPalette) {
        float hue = UnityEngine.Random.Range(0f, 1f);
        float saturation = usePastelPalette ? UnityEngine.Random.Range(0.25f, 0.45f) : UnityEngine.Random.Range(0.65f, 0.95f);
        float value = usePastelPalette ? UnityEngine.Random.Range(0.85f, 1f) : UnityEngine.Random.Range(0.55f, 0.9f);
        return Color.HSVToRGB(hue, saturation, value);
    }

    private Color GenerateContrastingColor(Color referenceColor, bool referenceIsPastel) {
        Color.RGBToHSV(referenceColor, out float baseHue, out float baseSaturation, out float baseValue);

        float hueDelta = UnityEngine.Random.Range(MinHueContrast, MaxHueContrast);
        if (UnityEngine.Random.value < 0.5f) {
            hueDelta = -hueDelta;
        }
        float newHue = Mathf.Repeat(baseHue + hueDelta, 1f);

        float newSaturation;
        float newValue;
        if (referenceIsPastel) {
            newSaturation = UnityEngine.Random.Range(0.65f, 1f);
            newValue = UnityEngine.Random.Range(0.35f, 0.65f);
        } else {
            newSaturation = UnityEngine.Random.Range(0.2f, 0.5f);
            newValue = UnityEngine.Random.Range(0.8f, 1f);
        }

        return Color.HSVToRGB(newHue, newSaturation, newValue);
    }

    private HashSet<int> BuildSequentialIndexSet(int total) {
        HashSet<int> indices = new HashSet<int>();
        for (int i = 0; i < total; i++) {
            indices.Add(i);
        }
        return indices;
    }

    private HashSet<int> BuildRandomIndexSet(int total, int desiredCount) {
        desiredCount = Mathf.Clamp(desiredCount, 1, total);
        HashSet<int> indices = new HashSet<int>();
        while (indices.Count < desiredCount) {
            indices.Add(UnityEngine.Random.Range(0, total));
        }
        return indices;
    }
}
