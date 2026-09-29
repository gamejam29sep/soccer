using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class GrenouilleJoueur : NetworkBehaviour
{
    [SerializeField] private float vitesse = 4f;
    [SerializeField] private float forcePousseeBallon = 3f;

    private Rigidbody rb;
    private NetworkTransform networkTransform;

    private Renderer[] renderersGrenouille;

    // Direction locale du joueur
    private Vector3 direction;

    private Vector3 positionInitiale;
    private Quaternion rotationInitiale;

    private Vector3 ancienneDirection;

    // ==============================
    // POSITIONS DE DÉPART
    // ==============================

    private Vector3 positionHost =
        new Vector3(-3.6f, 1f, -0.61f);

    private Vector3 positionClient =
        new Vector3(3.6f, 1f, -0.61f);


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        networkTransform =
            GetComponent<NetworkTransform>();

        renderersGrenouille =
            GetComponentsInChildren<Renderer>();
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            $"[SPAWN] OwnerClientId={OwnerClientId} | " +
            $"LocalClientId={NetworkManager.Singleton.LocalClientId} | " +
            $"IsOwner={IsOwner} | " +
            $"IsLocalPlayer={IsLocalPlayer} | " +
            $"IsServer={IsServer}",
            this
        );


        // ==========================================
        // DÉTERMINE QUI EST CETTE GRENOUILLE
        // ==========================================

        if (OwnerClientId == NetworkManager.ServerClientId)
        {
            // HOST = gauche
            positionInitiale = positionHost;

            // Regarde vers la droite
            rotationInitiale =
                Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            // CLIENT = droite
            positionInitiale = positionClient;

            // Regarde vers la gauche
            rotationInitiale =
                Quaternion.Euler(0f, -90f, 0f);


            // Grenouille client = rouge
            foreach (Renderer r in renderersGrenouille)
            {
                r.material.color = Color.red;
            }
        }


        // ==========================================
        // COPIE DISTANTE
        // ==========================================

        // Si cette grenouille n'appartient pas à cette machine,
        // elle ne fait pas sa propre physique.
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }


        // ==========================================
        // GRENOUILLE LOCALE
        // ==========================================

        rb.isKinematic = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;


        // Position + rotation de départ
        transform.SetPositionAndRotation(
            positionInitiale,
            rotationInitiale
        );


        // NetworkTransform en OWNER AUTHORITY
        networkTransform.Teleport(
            positionInitiale,
            rotationInitiale,
            transform.localScale
        );
    }

    private void Update()
    {
        // Seulement le propriétaire lit ses touches
        if (!IsSpawned || !IsOwner)
            return;


        Keyboard clavier = Keyboard.current;

        if (clavier == null)
        {
            direction = Vector3.zero;
            return;
        }


        float x = 0f;
        float z = 0f;


        // ==========================================
        // HOST = FLÈCHES
        // ==========================================

        if (OwnerClientId == NetworkManager.ServerClientId)
        {
            if (clavier.leftArrowKey.isPressed)
                x = -1f;

            if (clavier.rightArrowKey.isPressed)
                x = 1f;

            if (clavier.upArrowKey.isPressed)
                z = 1f;

            if (clavier.downArrowKey.isPressed)
                z = -1f;
        }


        // ==========================================
        // CLIENT = WASD
        // ==========================================

        else
        {
            if (clavier.aKey.isPressed)
                x = -1f;

            if (clavier.dKey.isPressed)
                x = 1f;

            if (clavier.wKey.isPressed)
                z = 1f;

            if (clavier.sKey.isPressed)
                z = -1f;
        }


        direction =
            new Vector3(x, 0f, z).normalized;


        // Debug seulement lorsque la direction change
        if (direction != ancienneDirection)
        {
            Debug.Log(
                $"[INPUT] Joueur {OwnerClientId} : " +
                direction
            );

            ancienneDirection = direction;
        }
    }


    // ==========================================
    // DÉPLACEMENT LOCAL
    // ==========================================

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner)
            return;

        rb.MovePosition(
            rb.position +
            direction *
            vitesse *
            Time.fixedDeltaTime
        );

        rb.MoveRotation(rotationInitiale);
    }
    // ==========================================
    // SORTIE
    // ==========================================

    private void OnTriggerEnter(Collider other)
    {
        // Chaque joueur gère seulement
        // sa propre grenouille.
        if (!IsOwner)
            return;


        if (other.CompareTag("sortie"))
        {
            Debug.Log(
                $"Grenouille {OwnerClientId} " +
                $"a touché une sortie."
            );

            ReplacerGrenouille();
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!IsOwner)
            return;

        if (!collision.gameObject.CompareTag("ballon"))
            return;

        // Pas de poussée si le joueur ne bouge pas
        if (direction == Vector3.zero)
            return;

        // HOST : peut pousser directement
        if (IsServer)
        {
            if (Ballon.instance != null)
            {
                Ballon.instance.PousserBallon(
                    direction,
                    forcePousseeBallon
                );
            }
        }
        // CLIENT : demande au serveur de pousser le ballon
        else
        {
            PousserBallonServerRpc(direction);
        }
    }
    [ServerRpc]
    private void PousserBallonServerRpc(Vector3 directionPoussee)
    {
        if (Ballon.instance == null)
            return;

        Ballon.instance.PousserBallon(
            directionPoussee,
            forcePousseeBallon
        );
    }



    // ==========================================
    // RESPAWN
    // ==========================================

    private void ReplacerGrenouille()
    {
        if (!IsOwner)
            return;


        // Arrête le déplacement
        direction = Vector3.zero;


        // Arrête complètement la physique
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;


        // Replace la grenouille
        transform.SetPositionAndRotation(
            positionInitiale,
            rotationInitiale
        );


        // Synchronise la nouvelle position
        networkTransform.Teleport(
            positionInitiale,
            rotationInitiale,
            transform.localScale
        );


        Debug.Log(
            $"[RESPAWN] Grenouille " +
            $"{OwnerClientId} replacée à : " +
            $"{positionInitiale}"
        );
    }
}