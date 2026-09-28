using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class GrenouilleJoueur : NetworkBehaviour
{
    [SerializeField] private float vitesse = 4f;

    private Rigidbody rb;
    private Vector3 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            $"Grenouille créée : IsOwner={IsOwner}, " +
            $"IsLocalPlayer={IsLocalPlayer}, IsServer={IsServer}",
            this
        );
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner) return;

        GestionDeplacement();
    }

    private void GestionDeplacement()
    {
        Keyboard clavier = Keyboard.current;
        if (clavier == null)
        {
            direction = Vector3.zero;
            return;
        }

        float x = 0f;
        float z = 0f;

        if (clavier.leftArrowKey.isPressed) x = -1f;
        if (clavier.rightArrowKey.isPressed) x = 1f;
        if (clavier.upArrowKey.isPressed) z = 1f;
        if (clavier.downArrowKey.isPressed) z = -1f;

        direction = new Vector3(x, 0f, z).normalized;
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        rb.MovePosition(
            rb.position + direction * vitesse * Time.fixedDeltaTime
        );
    }
}