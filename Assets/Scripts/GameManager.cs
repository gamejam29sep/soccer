using UnityEngine;
using Unity.Netcode; // namespace pour utiliser Netcode
using UnityEngine.SceneManagement; // namespace pour la gestion des scènes


public class GameManager : NetworkBehaviour //pour un network object
{
    public static GameManager instance;// Singleton pour parler au GameManager de n'importe où

    public bool partieEnCours { private set; get; } //permet de savoir si une partie est en cours
    public bool partieTerminee { private set; get; } // permet de savoir si une partie est terminée

    // Création du singleton si nécessaire
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

    void Update()
    {
        if (!IsHost) return;
        if (partieEnCours) return;

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
        {
            NouvellePartie();
        }

    }

    // Activation d'une nouvelle partie lorsque 2 joueurs. On appelle la fonction de la balle qui
    // la place au milieu et qui lui donne une vélocité.
    public void NouvellePartie()
    {
        partieEnCours = true;
        Ballon.instance.LanceBalleMilieu();
    }

    // Fonction appelée par le ScoreManager pour terminer la partie (nous l'utilserons plus tard)
    public void FinPartie()
    {
        partieTerminee = true;
    }

    // Fonction appelée pour le bouton qui permet de se connecter comme hôte
    public void LanceCommeHote() // Public pour être appeler de l'extérieur (par le bouton Hôte)
    {
        NetworkManager.Singleton.StartHost(); // Fonction du NetworkManager pour démarrer une partie comme hôte
    }

    // Fonction appelée pour le bouton qui permet de se connecter comme client
    public void LanceCommeClient() // Public pour être appeler de l'extérieur (par le bouton Client)
    {
        NetworkManager.Singleton.StartClient(); // Fonction du NetworkManager pour démarrer une partie comme client
    }
}