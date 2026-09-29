using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public bool partieEnCours { private set; get; }
    public bool partieTerminee { private set; get; }

    // ==========================================
    // POSITIONS DES JOUEURS
    // ==========================================

    [Header("Positions des joueurs")]
    [SerializeField] private Transform spawnHost;
    [SerializeField] private Transform spawnClient;

    public Transform SpawnHost => spawnHost;
    public Transform SpawnClient => spawnClient;


    // ==========================================
    // UI
    // ==========================================

    [Header("UI à cacher")]
    [SerializeField] private GameObject image1;
    [SerializeField] private GameObject image2;
    [SerializeField] private GameObject image3;


    // ==========================================
    // AUDIO
    // ==========================================

    [Header("Audio")]
    [SerializeField] private AudioSource audioIntro;


    private void Awake()
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


    private void Update()
    {
        // Seulement l'hôte vérifie le lancement de la partie
        if (!IsHost)
            return;

        if (partieEnCours)
            return;

        if (partieTerminee)
            return;

        // Lance la partie lorsqu'il y a 2 joueurs
        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
        {
            NouvellePartie();
        }
    }


    // ==========================================
    // NOUVELLE PARTIE
    // ==========================================

    public void NouvellePartie()
    {
        if (Ballon.instance == null)
        {
            Debug.LogError(
                "Aucun ballon actif dans la scène Gameplay."
            );

            return;
        }

        partieEnCours = true;
        partieTerminee = false;

        Ballon.instance.LanceBalleMilieu();

        Debug.Log("Nouvelle partie commencée.");
    }


    // ==========================================
    // FIN DE PARTIE
    // ==========================================

    // true  = Host gagne
    // false = Client gagne

    public void FinPartie(bool hoteGagne)
    {
        // Seul le serveur décide qui gagne
        if (!IsServer)
            return;

        // Empêche plusieurs fins de partie
        if (partieTerminee)
            return;

        partieTerminee = true;
        partieEnCours = false;

        if (hoteGagne)
        {
            Debug.Log("FIN DE PARTIE : HOST GAGNE");
        }
        else
        {
            Debug.Log("FIN DE PARTIE : CLIENT GAGNE");
        }

        // Informe les deux joueurs
        FinPartieClientRpc(hoteGagne);
    }


    // ==========================================
    // VICTOIRE / ÉCHEC
    // ==========================================

    [ClientRpc]
    private void FinPartieClientRpc(bool hoteGagne)
    {
        // Identifiant du joueur sur cette machine
        ulong monClientId =
            NetworkManager.Singleton.LocalClientId;

        // Vérifie si cette machine est l'hôte
        bool jeSuisHote =
            monClientId ==
            NetworkManager.ServerClientId;

        bool jaiGagne;


        // ======================================
        // HOST GAGNE
        // ======================================

        if (hoteGagne)
        {
            // Le Host gagne,
            // donc seul le Host voit Victoire
            jaiGagne = jeSuisHote;
        }

        // ======================================
        // CLIENT GAGNE
        // ======================================

        else
        {
            // Le Client gagne,
            // donc celui qui n'est pas Host gagne
            jaiGagne = !jeSuisHote;
        }


        // ======================================
        // CHARGE LA SCÈNE LOCALE
        // ======================================

        if (jaiGagne)
        {
            Debug.Log("JE SUIS LE GAGNANT");

            SceneManager.LoadScene("Victoire");
        }
        else
        {
            Debug.Log("J'AI PERDU");

            SceneManager.LoadScene("Echec");
        }
    }


    // ==========================================
    // CACHE L'UI DE CONNEXION
    // ==========================================

    private void CacherUIConnexion()
    {
        if (image1 != null)
            image1.SetActive(false);

        if (image2 != null)
            image2.SetActive(false);

        if (image3 != null)
            image3.SetActive(false);
    }


    // ==========================================
    // ARRÊTE LE SON D'INTRO
    // ==========================================

    private void ArreterSonIntro()
    {
        if (audioIntro != null &&
            audioIntro.isPlaying)
        {
            audioIntro.Stop();
        }
    }


    // ==========================================
    // LANCER COMME HÔTE
    // ==========================================

    public void LanceCommeHote()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "Le NetworkManager est déjà lancé."
            );

            return;
        }

        // Arrête la musique / son d'intro
        ArreterSonIntro();

        // Démarre le Host
        NetworkManager.Singleton.StartHost();

        // Cache l'écran de connexion
        CacherUIConnexion();

        Debug.Log("Partie lancée comme HOST.");
    }


    // ==========================================
    // LANCER COMME CLIENT
    // ==========================================

    public void LanceCommeClient()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "Le NetworkManager est déjà lancé."
            );

            return;
        }

        // Arrête la musique / son d'intro
        ArreterSonIntro();

        // Démarre le Client
        NetworkManager.Singleton.StartClient();

        // Cache l'écran de connexion
        CacherUIConnexion();

        Debug.Log("Partie lancée comme CLIENT.");
    }
}