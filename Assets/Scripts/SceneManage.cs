using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;  
public class SceneManage : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject amphi;
    public GameObject amphi2;
    public GameObject Accueil;
    public Canvas canvas1;
    public Canvas canvas2;
    public Canvas canvas3;
    public Canvas canvas4;

    private AudioSource audioSource;
    public AudioClip clip;

    public Transform playerRig;

    public Transform teleportDestination;
    void Start()
    {
        canvas4.enabled = false;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CLassactivate()
    {
        amphi.SetActive(true);
        canvas1.enabled = false;
        canvas2.enabled = false;
        canvas3.enabled = false;
        Accueil.SetActive(false);
        canvas4.enabled = true;

    }

    public void Amphiactivate()
    {
        amphi2.SetActive(true);
        canvas1.enabled = false;
        canvas2.enabled = false;
        canvas3.enabled = false;
        Accueil.SetActive(false);
        canvas4.enabled = true;
        if (audioSource != null && clip != null)
        {
            audioSource.Play();
        }
        
    }

    public void Exitactive()
    {
        amphi.SetActive(false);
        amphi2.SetActive(false);

        Accueil.SetActive(true);

        canvas1.enabled = true;
        canvas2.enabled = true;
        canvas3.enabled = true;

        canvas4.enabled = false;
        audioSource.Stop();

        playerRig.position = teleportDestination.position;
        playerRig.rotation = teleportDestination.rotation;
    }




}
