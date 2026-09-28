using UnityEngine;
using System;
using System.Collections;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Components;// pour accéder aux propriétés du NetworkTransform


public class Ballon : NetworkBehaviour // objet réseau
{
    public static Ballon instance; // Singleton
    float maxDistanceX = 25f; // moitié de la largeur de la table, pour savoir si un but est compté
    [SerializeField] private float nombreDeBonds; //compte du nombre de bonds de la balle // Servira plus tard
    [SerializeField] private float maxSpeed; // si on veut limiter la vitesse max de la balle (inutilisé)

    // Création d'un singleton. Il ne doit y avoir qu'une seule balle.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Seul le serveur gère la balle et lance sa physique.
        if (!IsServer) return;

        if (instance != null && instance != this)
        {
            Debug.LogError("Deux ballons réseau actifs sur le serveur.", this);
            return;
        }

        instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (instance == this)
            instance = null;

        base.OnNetworkDespawn();
    }

    /* Vérificaiton de la position de la balle pour voir si un but est compté
    - Seul le serveur fait la validation
    - Pas de validation si aucune partie en cours

    - Détection de la position de la balle. S'il y a un but :
    --- À implémenter On appelle la fonction pour augmenter le score du client hote (serveur) ou du client
    --- On appelle la fonction LanceBalleMilieu qui va replacer la balle
    */
    void Update()
    {
        if (!IsServer) return;

        if (!GameManager.instance.partieEnCours) return; // Il faudra créer cette variable dans le GameManager

        //but client
        if (transform.position.x < -maxDistanceX)
        {
            // Ici, il faudra aussi augmenter le score du joueur
            LanceBalleMilieu();
        }

        //but serveur (hôte)
        if (transform.position.x > maxDistanceX)
        {
            // Ici, il faudra aussi augmenter le score du joueur
            LanceBalleMilieu();
        }
    }

    /* Fonction qui amorce la séquence pour replacer la balle au milieu. Fonction publique appelée aussi par
    le script GameManager. (À implémenter)
    - Cacul du nombre de bonds à 0
    - On désactive l'interpolation du NetworkTransform pour éviter de voir l'interpolation de position de la balle
    - On replace la balle au centre de la table et on remet à 0 sa vélocité
    - Lancement d'une coroutine qui replacera et relancera la balle seulement si la partie n'est pas terminée
    */
    public void LanceBalleMilieu()
    {
        nombreDeBonds = 0;
        GetComponent<NetworkTransform>().Interpolate = false;
        transform.position = new Vector3(0f, 0.5f, 0f);
        GetComponent<Rigidbody>().linearVelocity = new Vector3(0, 0, 0);
        if (GameManager.instance.partieTerminee) return; // Il faudra créer cette variable dans le GameManager
        StartCoroutine(NouvelleBalle());
    }

    /* Coroutine qui fait suite à la fonction LanceBalleMilieu()
    - Pause d'une seconde
    - On réactive l'interpolation du NetworkTransform pour des déplacements fluides côté client
    - Vélocité de la balle tirée au hasard et application de la force sur la balle
    */
    IEnumerator NouvelleBalle()
    {
        yield return new WaitForSecondsRealtime(1f);
        GetComponent<NetworkTransform>().Interpolate = true;

        System.Random random = new System.Random();
        float aleaX = random.Next(0, 2) == 0 ? -10 : 10; //  opérateur ternaire
        float aleaZ = random.Next(0, 2) == 0 ? -10 : 10;

        GetComponent<Rigidbody>().AddForce(aleaX, 0, aleaZ, ForceMode.Impulse);
    }
}