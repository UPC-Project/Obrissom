using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private TextMeshProUGUI _ingredient1Name;
    [SerializeField] private Image _ingredient1Image;
    [SerializeField] private TextMeshProUGUI _ingredient2Name;
    [SerializeField] private Image _ingredient2Image;
    [SerializeField] private TextMeshProUGUI _resultName;
    [SerializeField] private Image _resultImage;

    public void SetRecipe(Recipe recipe)
    {
        _title.text = recipe.title;
        _description.text = recipe.description;
    }
}
