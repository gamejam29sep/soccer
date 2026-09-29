using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager instance;

    [SerializeField] private TMP_Text scoreTxt;

    // ==========================================
    // POINTAGE CIBLE
    // ==========================================

    // La partie se termine uniquement à 5 points
    private const int pointageCible = 5;


    // ==========================================
    // AUDIO
    // ==========================================

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonBut;


    // ==========================================
    // SCORES RÉSEAU
    // ==========================================

    private NetworkVariable<int> scoreHote =
        new NetworkVariable<int>();

    private NetworkVariable<int> scoreClient =
        new NetworkVariable<int>();


    // ==========================================
    // SINGLETON
    // ==========================================

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


    // ==========================================
    // SPAWN RÉSEAU
    // ==========================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Seul le serveur initialise les scores
        if (IsServer)
        {
            scoreHote.Value = 0;
            scoreClient.Value = 0;
        }

        // Détecte les changements de score
        scoreHote.OnValueChanged += OnChangementPointageHote;
        scoreClient.OnValueChanged += OnChangementPointageClient;

        // Affichage initial
        if (scoreTxt != null)
        {
            scoreTxt.text =
                scoreHote.Value + " - " + scoreClient.Value;
        }
    }


    // ==========================================
    // DESPAWN RÉSEAU
    // ==========================================

    public override void OnNetworkDespawn()
    {
        scoreHote.OnValueChanged -= OnChangementPointageHote;
        scoreClient.OnValueChanged -= OnChangementPointageClient;

        base.OnNetworkDespawn();
    }


    // ==========================================
    // SCORE HOST
    // ==========================================

    public void AugmenteHoteScore()
    {
        // Seul le serveur peut modifier le score
        if (!IsServer)
            return;

        // Si la partie est déjà terminée,
        // aucun nouveau point n'est ajouté
        if (GameManager.instance != null &&
            GameManager.instance.partieTerminee)
        {
            return;
        }

        // Ajoute 1 point au Host
        scoreHote.Value++;

        Debug.Log(
            "Score Hôte : " +
            scoreHote.Value +
            " / 5"
        );


        // ======================================
        // FIN DE PARTIE SEULEMENT À 5 POINTS
        // ======================================

        if (scoreHote.Value == pointageCible)
        {
            Debug.Log(
                "HOST A 5 POINTS → FIN DE PARTIE"
            );

            if (GameManager.instance != null)
            {
                GameManager.instance.FinPartie(true);
            }
        }
    }


    // ==========================================
    // SCORE CLIENT
    // ==========================================

    public void AugmenteScoreClient()
    {
        // Seul le serveur peut modifier le score
        if (!IsServer)
            return;

        // Si la partie est déjà terminée,
        // aucun nouveau point n'est ajouté
        if (GameManager.instance != null &&
            GameManager.instance.partieTerminee)
        {
            return;
        }

        // Ajoute 1 point au Client
        scoreClient.Value++;

        Debug.Log(
            "Score Client : " +
            scoreClient.Value +
            " / 5"
        );


        // ======================================
        // FIN DE PARTIE SEULEMENT À 5 POINTS
        // ======================================

        if (scoreClient.Value == pointageCible)
        {
            Debug.Log(
                "CLIENT A 5 POINTS → FIN DE PARTIE"
            );

            if (GameManager.instance != null)
            {
                GameManager.instance.FinPartie(false);
            }
        }
    }


    // ==========================================
    // AFFICHAGE SCORE HOST
    // ==========================================

    private void OnChangementPointageHote(
        int ancienScoreHote,
        int nouveauScoreHote)
    {
        if (ancienScoreHote == nouveauScoreHote)
            return;

        if (scoreTxt != null)
        {
            scoreTxt.text =
                scoreHote.Value +
                " - " +
                scoreClient.Value;
        }

        JouerSonBut();
    }


    // ==========================================
    // AFFICHAGE SCORE CLIENT
    // ==========================================

    private void OnChangementPointageClient(
        int ancienScoreClient,
        int nouveauScoreClient)
    {
        if (ancienScoreClient == nouveauScoreClient)
            return;

        if (scoreTxt != null)
        {
            scoreTxt.text =
                scoreHote.Value +
                " - " +
                scoreClient.Value;
        }

        JouerSonBut();
    }


    // ==========================================
    // SON DU BUT
    // ==========================================

    private void JouerSonBut()
    {
        if (audioSource == null ||
            sonBut == null)
        {
            return;
        }

        audioSource.Stop();

        audioSource.clip = sonBut;

        // Commence à 1 seconde
        audioSource.time = 1f;

        audioSource.Play();

        StartCoroutine(
            ArreterSonButApres3Secondes()
        );
    }


    // ==========================================
    // ARRÊTER LE SON
    // ==========================================

    private IEnumerator ArreterSonButApres3Secondes()
    {
        yield return new WaitForSecondsRealtime(3f);

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}