using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CollectionManager : MonoBehaviour
{
    public TMP_Text itemNumberText;
    public TMP_Text itemNameText;
    public TMP_Text itemTypeText;
    public TMP_Text itemDescText;
    public TMP_Text itemObtainedText;

    public Image itemPreviewImage;

    public Sprite studentIDSprite;
    public Sprite redCrystalSprite;
    public Sprite cannedFoodSprite;
    public Sprite memoSprite;

    private void Start()
    {
        ShowStudentID();
    }

    public void ShowStudentID()
    {
        itemNumberText.text = "No. 001";
        itemNameText.text = "Student ID";
        itemTypeText.text = "Normal Item";
        itemDescText.text = "A high school student ID.\nThe name is written by hand.";
        itemObtainedText.text = "Obtained:\nChapter 1 First Battle Reward";

        itemPreviewImage.sprite = studentIDSprite;
    }

    public void ShowRedCrystal()
    {
        itemNumberText.text = "No. 002";
        itemNameText.text = "Red Crystal";
        itemTypeText.text = "Rare Item";
        itemDescText.text = "A strange red crystal.\nIt faintly glows in the dark.";
        itemObtainedText.text = "Obtained:\nUnknown Area";

        itemPreviewImage.sprite = redCrystalSprite;
    }

    public void ShowCannedFood()
    {
        itemNumberText.text = "No. 003";
        itemNameText.text = "Canned Food";
        itemTypeText.text = "Normal Item";
        itemDescText.text = "An old canned food.\nIt may still be edible.";
        itemObtainedText.text = "Obtained:\nAbandoned Shelter";

        itemPreviewImage.sprite = cannedFoodSprite;
    }

    public void ShowMemo()
    {
        itemNumberText.text = "No. 004";
        itemNameText.text = "Memo";
        itemTypeText.text = "Document";
        itemDescText.text = "A torn memo.\nSome words are difficult to read.";
        itemObtainedText.text = "Obtained:\nRuined Classroom";

        itemPreviewImage.sprite = memoSprite;
    }
}