using UnityEngine;

public class PuzzleSlotCanvas : MonoBehaviour
{
    public static PuzzleSlotCanvas Instance { get; private set; }

    [SerializeField] private RectTransform slotRoot; // сюда будут переноситься головоломки

    void Awake()
    {
        Instance = this;
    }

    public RectTransform SlotRoot => slotRoot;
}