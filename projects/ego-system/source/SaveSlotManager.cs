using UnityEngine;
using UnityEngine.UI;

public class SaveSlotManager : MonoBehaviour
{
    public Image slot1;
    public Image slot2;
    public Image slot3;

    private Color normalColor = new Color(0.04f, 0.05f, 0.07f, 1f);
    private Color selectedColor = new Color(0.35f, 0.07f, 0.10f, 1f);

    public void SelectSlot1()
    {
        ResetSlots();
        slot1.color = selectedColor;
    }

    public void SelectSlot2()
    {
        ResetSlots();
        slot2.color = selectedColor;
    }

    public void SelectSlot3()
    {
        ResetSlots();
        slot3.color = selectedColor;
    }

    void ResetSlots()
    {
        slot1.color = normalColor;
        slot2.color = normalColor;
        slot3.color = normalColor;
    }
}