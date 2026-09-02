using UnityEngine;
using UnityEngine.SceneManagement;

/* Script pour charger la scène du jeu (bootstrap).
- Création du singleton pour s'assurer qu'il n'y a qu'une seule instance de ce script
- Chargement de la scène "JeuMulti" au démarrage
*/
public class Lanceur : MonoBehaviour
{
    public static Lanceur instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        SceneManager.LoadScene("Gameplay");
    }

}