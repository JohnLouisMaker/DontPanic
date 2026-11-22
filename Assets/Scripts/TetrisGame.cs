using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Core rules and board rendering for the mobile Tetris game.</summary>
public class TetrisGame : MonoBehaviour
{
    public const int Width = 10;
    public const int Height = 20;

    static readonly Vector2Int[][] Shapes =
    {
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, // I
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }, // J
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) }, // L
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // O
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // S
        new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }, // T
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) }  // Z
    };
    static readonly Color[] PieceColors =
    {
        new Color(0.18f, 0.88f, 0.95f), new Color(0.28f, 0.42f, 0.98f),
        new Color(1f, 0.57f, 0.2f), new Color(1f, 0.83f, 0.25f),
        new Color(0.35f, 0.88f, 0.45f), new Color(0.72f, 0.4f, 0.95f),
        new Color(0.98f, 0.32f, 0.42f)
    };

    int[,] board = new int[Width, Height];
    int piece, nextPiece, rotation, pieceX, pieceY;
    float fallTimer;
    bool gameOver;
    int score, lines, level = 1;
    Image[,] cells;
    Image[] nextCells;
    Text scoreText, linesText, levelText, statusText;
    RectTransform boardRoot, nextRoot;
    Color emptyColor = new Color(0.06f, 0.09f, 0.16f);

    void Start()
    {
        BuildInterface();
        nextPiece = Random.Range(0, Shapes.Length);
        SpawnPiece();
        RefreshAll();
    }

    void Update()
    {
        if (gameOver) return;
        float interval = Mathf.Max(0.09f, 0.72f - (level - 1) * 0.055f);
        fallTimer += Time.deltaTime;
        if (fallTimer >= interval) { fallTimer = 0; StepDown(); }

        // Keyboard controls are useful in the Unity Editor; Android uses the on-screen buttons.
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.leftArrowKey.wasPressedThisFrame) Move(-1);
        if (keyboard.rightArrowKey.wasPressedThisFrame) Move(1);
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame) Rotate();
        if (keyboard.downArrowKey.wasPressedThisFrame) StepDown();
        if (keyboard.spaceKey.wasPressedThisFrame) HardDrop();
    }

    public void MoveLeft() { Move(-1); }
    public void MoveRight() { Move(1); }
    public void RotatePiece() { Rotate(); }
    public void SoftDrop() { if (!gameOver) StepDown(); }
    public void HardDrop()
    {
        if (gameOver) return;
        while (CanPlace(pieceX, pieceY - 1, rotation)) pieceY--;
        LockPiece();
    }
    public void Restart()
    {
        board = new int[Width, Height]; score = 0; lines = 0; level = 1;
        gameOver = false; fallTimer = 0; nextPiece = Random.Range(0, Shapes.Length);
        SpawnPiece(); RefreshAll();
    }

    void Move(int dx)
    {
        if (!gameOver && CanPlace(pieceX + dx, pieceY, rotation)) { pieceX += dx; RefreshBoard(); }
    }
    void Rotate()
    {
        if (gameOver) return;
        int rotated = (rotation + 1) % 4;
        int[] kicks = { 0, -1, 1, -2, 2 };
        foreach (int kick in kicks)
            if (CanPlace(pieceX + kick, pieceY, rotated)) { pieceX += kick; rotation = rotated; RefreshBoard(); return; }
    }
    void StepDown()
    {
        if (CanPlace(pieceX, pieceY - 1, rotation)) { pieceY--; RefreshBoard(); }
        else LockPiece();
    }
    void SpawnPiece()
    {
        piece = nextPiece; nextPiece = Random.Range(0, Shapes.Length);
        rotation = 0; pieceX = Width / 2; pieceY = Height - 2;
        if (!CanPlace(pieceX, pieceY, rotation))
        {
            gameOver = true;
            if (statusText) statusText.text = "FIM DE JOGO";
        }
        RefreshNext();
    }
    bool CanPlace(int x, int y, int rot)
    {
        foreach (Vector2Int offset in Shapes[piece])
        {
            Vector2Int p = RotateOffset(offset, rot);
            int bx = x + p.x, by = y + p.y;
            if (bx < 0 || bx >= Width || by < 0) return false;
            if (by < Height && board[bx, by] != 0) return false;
        }
        return true;
    }
    static Vector2Int RotateOffset(Vector2Int p, int turns)
    {
        for (int i = 0; i < turns; i++) p = new Vector2Int(p.y, -p.x);
        return p;
    }
    void LockPiece()
    {
        foreach (Vector2Int offset in Shapes[piece])
        {
            Vector2Int p = RotateOffset(offset, rotation);
            int x = pieceX + p.x, y = pieceY + p.y;
            if (y >= Height) { gameOver = true; continue; }
            if (y >= 0 && x >= 0 && x < Width) board[x, y] = piece + 1;
        }
        ClearLines();
        if (!gameOver) SpawnPiece();
        RefreshAll();
    }
    void ClearLines()
    {
        int cleared = 0;
        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++) if (board[x, y] == 0) { full = false; break; }
            if (!full) continue;
            cleared++;
            for (int row = y; row < Height - 1; row++)
                for (int x = 0; x < Width; x++) board[x, row] = board[x, row + 1];
            for (int x = 0; x < Width; x++) board[x, Height - 1] = 0;
            y--;
        }
        if (cleared > 0)
        {
            int[] points = { 0, 100, 300, 500, 800 };
            score += points[Mathf.Clamp(cleared, 0, 4)] * level;
            lines += cleared; level = lines / 10 + 1;
        }
    }

    void BuildInterface()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Canvas canvas = new GameObject("Tetris Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(432, 900);
        scaler.matchWidthOrHeight = 0.5f;
        Image background = MakeImage(canvas.transform, "Background", new Color(0.025f, 0.04f, 0.08f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MakeText(canvas.transform, "TETRIS", 36, new Color(0.36f, 0.9f, 1f), new Vector2(0, -48), new Vector2(400, 60), FontStyle.Bold);
        scoreText = MakeTopText(canvas.transform, "PONTOS  0", 17, new Color(0.5f, 0.78f, 1f), 0.875f, new Vector2(200, 34), new Vector2(-100, 0), FontStyle.Bold);
        linesText = MakeTopText(canvas.transform, "LINHAS  0", 17, new Color(1f, 0.72f, 0.44f), 0.875f, new Vector2(200, 34), new Vector2(100, 0), FontStyle.Bold);
        levelText = MakeTopText(canvas.transform, "NÍVEL  1", 14, new Color(0.63f, 0.72f, 0.86f), 0.838f, new Vector2(160, 26), Vector2.zero, FontStyle.Normal);

        float screenHeight = canvas.GetComponent<RectTransform>().rect.height;
        float boardWidth = Mathf.Min(400, canvas.GetComponent<RectTransform>().rect.width - 28);
        float boardHeight = boardWidth * 1.55f;
        float boardTop = screenHeight * 0.80f;
        boardRoot = MakePanel(canvas.transform, "Board", new Color(0.09f, 0.13f, 0.21f), new Vector2(0, -screenHeight * 0.17f), new Vector2(boardWidth, boardHeight));
        cells = new Image[Width, Height];
        float gap = 2.5f, cell = (boardWidth - gap * (Width + 1)) / Width;
        for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
        {
            Image tile = MakeImage(boardRoot, "Cell " + x + "," + y, emptyColor);
            RectTransform rt = tile.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2(gap + x * (cell + gap), -gap - y * (cell + gap));
            cells[x, y] = tile;
        }
        float boardBottom = screenHeight * 0.17f + boardHeight;
        float controlsY = -Mathf.Max(boardBottom + 55, screenHeight * 0.79f);
        nextRoot = MakePanel(canvas.transform, "Next Piece", new Color(0.09f, 0.13f, 0.21f), new Vector2(0, controlsY + 10), new Vector2(106, 80));
        nextCells = new Image[16];
        for (int i = 0; i < nextCells.Length; i++)
        {
            Image tile = MakeImage(nextRoot, "Next " + i, emptyColor);
            tile.rectTransform.sizeDelta = new Vector2(17, 17);
            tile.rectTransform.anchoredPosition = new Vector2(-25 + (i % 4) * 18, 20 - (i / 4) * 18);
            nextCells[i] = tile;
        }
        MakeTopText(canvas.transform, "PRÓXIMA", 12, new Color(0.63f, 0.72f, 0.86f), 1f + (controlsY - 80) / screenHeight, new Vector2(150, 20), Vector2.zero, FontStyle.Bold);
        statusText = MakeTopText(canvas.transform, "", 24, Color.white, 0.55f, new Vector2(360, 50), Vector2.zero, FontStyle.Bold);

        float buttonY = controlsY - 105;
        MakeButton(canvas.transform, "◀", new Vector2(-136, buttonY), new Vector2(72, 58), MoveLeft);
        MakeButton(canvas.transform, "▼", new Vector2(-46, buttonY), new Vector2(72, 58), SoftDrop);
        MakeButton(canvas.transform, "↻", new Vector2(46, buttonY), new Vector2(72, 58), RotatePiece);
        MakeButton(canvas.transform, "▶", new Vector2(136, buttonY), new Vector2(72, 58), MoveRight);
        MakeButton(canvas.transform, "⬇⬇", new Vector2(-83, buttonY - 65), new Vector2(150, 42), HardDrop);
        MakeButton(canvas.transform, "RECOMEÇAR", new Vector2(83, buttonY - 65), new Vector2(150, 42), Restart);
    }

    void RefreshAll()
    {
        RefreshBoard(); RefreshNext();
        if (scoreText) scoreText.text = "PONTOS  " + score;
        if (linesText) linesText.text = "LINHAS  " + lines;
        if (levelText) levelText.text = "NÍVEL  " + level;
        if (statusText && !gameOver) statusText.text = "";
    }
    void RefreshBoard()
    {
        if (cells == null) return;
        for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
            cells[x, y].color = board[x, y] == 0 ? emptyColor : PieceColors[board[x, y] - 1];
        if (gameOver) return;
        foreach (Vector2Int offset in Shapes[piece])
        {
            Vector2Int p = RotateOffset(offset, rotation);
            int x = pieceX + p.x, y = pieceY + p.y;
            if (x >= 0 && x < Width && y >= 0 && y < Height) cells[x, y].color = PieceColors[piece];
        }
    }
    void RefreshNext()
    {
        if (nextCells == null) return;
        for (int i = 0; i < nextCells.Length; i++) nextCells[i].color = emptyColor;
        foreach (Vector2Int p in Shapes[nextPiece])
        {
            int x = p.x + 1, y = p.y + 1;
            if (x >= 0 && x < 4 && y >= 0 && y < 4) nextCells[y * 4 + x].color = PieceColors[nextPiece];
        }
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false); image.color = color; return image;
    }
    static RectTransform MakePanel(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        Image image = MakeImage(parent, name, color); RectTransform rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1); rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = position; rt.sizeDelta = size; return rt;
    }
    static Text MakeText(Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions, FontStyle style)
    {
        Text text = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.color = color; text.fontStyle = style; text.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = text.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = position; rt.sizeDelta = dimensions;
        return text;
    }
    static Text MakeTopText(Transform parent, string value, int size, Color color, float anchorY, Vector2 dimensions, Vector2 offset, FontStyle style)
    {
        Text text = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.color = color; text.fontStyle = style; text.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = text.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = offset; rt.sizeDelta = dimensions;
        return text;
    }
    static void MakeButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        Image image = MakeImage(parent, label + " Button", new Color(0.14f, 0.21f, 0.34f));
        Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(0.22f, 0.37f, 0.55f); colors.pressedColor = new Color(0.28f, 0.53f, 0.68f); button.colors = colors;
        RectTransform rt = image.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = position; rt.sizeDelta = size;
        Text text = MakeText(image.transform, label, 21, Color.white, Vector2.zero, size, FontStyle.Bold);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
    }
    static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }
}
