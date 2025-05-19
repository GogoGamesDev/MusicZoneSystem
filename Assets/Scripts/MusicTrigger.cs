using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [Header("Music Parameters")]
    public MusicManager musicManager; // Referencia al script MusicManager
    public string zoneName; // Nombre de la zona de música a manejar
    
    [Header("Visual Color Parameters")]
    [SerializeField] private bool isRed = false;
    [SerializeField] private bool isBlue = false;
    [SerializeField] private bool isYellow = false;
    [SerializeField] private bool isGreen = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            musicManager.InMusic(zoneName);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            musicManager.OutMusic(zoneName);
        }
    }

    void OnDrawGizmos()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            if(isRed)
            {
                Gizmos.color = new Color(1, 0, 0, 0.15f); 
            }
            if(isYellow)
            {
                Gizmos.color = new Color(1, 1, 0, 0.15f); 
            }
            if(isBlue)
            {
                Gizmos.color = new Color(0, 0, 1, 0.15f); 
            }
            if(isGreen)
            {
                Gizmos.color = new Color(0, 1, 0, 0.15f); 
            }
            
            Gizmos.DrawCube(transform.position, collider.bounds.size);
        }
    }
}
