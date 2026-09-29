using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoireRecommencer : MonoBehaviour
{
    public void Recommencer()
    {
        SceneManager.LoadScene("Bootstrap");
    }
}