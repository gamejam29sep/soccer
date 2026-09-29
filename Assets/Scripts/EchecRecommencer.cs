using UnityEngine;
using UnityEngine.SceneManagement;

public class EchecRecommencer : MonoBehaviour
{
    public void Recommencer()
    {
        SceneManager.LoadScene("Bootstrap");
    }
}
