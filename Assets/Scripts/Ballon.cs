using UnityEngine;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Ballon : NetworkBehaviour
{
    public static Ballon instance;

    [SerializeField] private float nombreDeBonds;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonEchec;

    private Rigidbody rb;
    private NetworkTransform networkTransform;

    private Collider[] collidersBallon;
    private Renderer[] renderersBallon;

    private Vector3 positionInitiale;

    // Empêche plusieurs respawns en même temps
    private bool enRespawn = false;


    public void PousserBallon(Vector3 direction, float force)
    {
        if (!IsServer)
            return;

        if (rb == null)
            return;

        // Ne pas pousser le ballon pendant son respawn
        if (enRespawn)
            return;

        Vector3 directionPoussee =
            new Vector3(direction.x, 0f, direction.z).normalized;

        rb.AddForce(
            directionPoussee * force,
            ForceMode.Force
        );
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        if (instance != null && instance != this)
        {
            Debug.LogError(
                "Deux ballons réseau actifs sur le serveur.",
                this
            );

            return;
        }

        instance = this;

        rb = GetComponent<Rigidbody>();

        networkTransform =
            GetComponent<NetworkTransform>();

        // Récupère automatiquement les colliders
        collidersBallon =
            GetComponentsInChildren<Collider>();

        // Récupère automatiquement les renderers
        renderersBallon =
            GetComponentsInChildren<Renderer>();

        // Sauvegarde la position de départ
        positionInitiale = transform.position;
    }


    public override void OnNetworkDespawn()
    {
        if (instance == this)
            instance = null;

        base.OnNetworkDespawn();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (enRespawn)
            return;

        // ==========================================
        // SORTIE
        // ==========================================

        if (other.CompareTag("sortie"))
        {
            Debug.Log("SORTIE détectée");

            // Joue le son d'échec chez Host + Client
            JouerSonEchecClientRpc();

            ReplacerBalle();

            return;
        }

        // ==========================================
        // GOAL HOST
        // ==========================================

        if (other.CompareTag("goalhost"))
        {
            Debug.Log("GOAL HOST détecté");

            if (ScoreManager.instance != null)
            {
                ScoreManager.instance.AugmenteHoteScore();
                LanceBalleMilieu();
            }
            else
            {
                Debug.LogError("ScoreManager.instance est NULL");
            }

            return;
        }

        // ==========================================
        // GOAL CLIENT
        // ==========================================

        if (other.CompareTag("goalclient"))
        {
            Debug.Log("GOAL CLIENT détecté");

            if (ScoreManager.instance != null)
            {
                ScoreManager.instance.AugmenteScoreClient();
                LanceBalleMilieu();
            }
            else
            {
                Debug.LogError("ScoreManager.instance est NULL");
            }
        }
    }

    [ClientRpc]
    private void JouerSonEchecClientRpc()
    {
        if (audioSource == null || sonEchec == null)
            return;

        StartCoroutine(JouerSonEchec());
    }

    private IEnumerator JouerSonEchec()
    {
        // Arrête un éventuel son déjà en cours
        audioSource.Stop();

        // Assigne le clip
        audioSource.clip = sonEchec;

        // Commence à la 6e seconde du fichier
        audioSource.time = 6f;

        // Lance le son
        audioSource.Play();

        // Attend 2 secondes
        yield return new WaitForSecondsRealtime(2f);

        // Arrête donc à la 8e seconde du fichier
        audioSource.Stop();
    }

    // ==========================================
    // SORTIE
    // ==========================================

    private void ReplacerBalle()
    {
        if (!IsServer)
            return;

        if (enRespawn)
            return;

        // false = ne pas relancer automatiquement
        StartCoroutine(
            RespawnBalle(false)
        );
    }


    // ==========================================
    // APRÈS UN BUT / DÉBUT DE PARTIE
    // ==========================================

    public void LanceBalleMilieu()
    {
        if (!IsServer)
            return;

        if (enRespawn)
            return;

        // true = relancer après 1 seconde
        StartCoroutine(
            RespawnBalle(true)
        );
    }


    // ==========================================
    // RESPAWN COMPLET
    // ==========================================

    private IEnumerator RespawnBalle(bool relancer)
    {
        enRespawn = true;

        nombreDeBonds = 0;


        // ------------------------------------------
        // ARRÊTE LA PHYSIQUE
        // ------------------------------------------

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Le ballon ne peut plus pousser les objets
        rb.isKinematic = true;


        // ------------------------------------------
        // FAIT DISPARAÎTRE LE BALLON
        // ------------------------------------------

        ChangerEtatBallonClientRpc(false);


        // ------------------------------------------
        // DÉSACTIVE L'INTERPOLATION
        // ------------------------------------------

        networkTransform.Interpolate = false;


        // ------------------------------------------
        // REPLACE LE BALLON PENDANT QU'IL EST CACHÉ
        // ------------------------------------------

        transform.position = positionInitiale;

        networkTransform.Teleport(
            positionInitiale,
            transform.rotation,
            transform.localScale
        );


        // ------------------------------------------
        // ATTEND 1 SECONDE
        // ------------------------------------------

        yield return new WaitForSecondsRealtime(1f);


        // Si la partie est terminée,
        // on peut quand même faire réapparaître la balle
        // mais on ne la relance pas.

        networkTransform.Interpolate = true;


        // ------------------------------------------
        // RÉACTIVE LA PHYSIQUE
        // ------------------------------------------

        rb.isKinematic = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;


        // ------------------------------------------
        // FAIT RÉAPPARAÎTRE LE BALLON
        // ------------------------------------------

        ChangerEtatBallonClientRpc(true);

        enRespawn = false;


        // ------------------------------------------
        // RELANCE LA BALLE SI NÉCESSAIRE
        // ------------------------------------------

        if (!relancer)
            yield break;


        if (GameManager.instance != null &&
            GameManager.instance.partieTerminee)
        {
            yield break;
        }


        System.Random random =
            new System.Random();

        float aleaX =
            random.Next(0, 2) == 0 ? -10 : 10;

        float aleaZ =
            random.Next(0, 2) == 0 ? -10 : 10;


        rb.AddForce(
            aleaX,
            0f,
            aleaZ,
            ForceMode.Impulse
        );
    }


    // ==========================================
    // VISIBILITÉ / COLLIDERS RÉSEAU
    // ==========================================

    [ClientRpc]
    private void ChangerEtatBallonClientRpc(bool actif)
    {
        // Affichage
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.enabled = actif;
        }


        // Collisions
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            c.enabled = actif;
        }
    }
}