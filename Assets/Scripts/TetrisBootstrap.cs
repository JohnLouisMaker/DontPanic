using UnityEngine;

/// <summary>Creates the Tetris controller automatically when a scene is opened.</summary>
public static class TetrisBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartGame()
    {
        if (Object.FindAnyObjectByType<TetrisGame>() != null) return;
        new GameObject("Tetris Game").AddComponent<TetrisGame>();
    }
}
