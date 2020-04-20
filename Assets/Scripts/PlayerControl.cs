using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerControl : MonoBehaviour
{
    [SerializeField] float rcsThrust = 100f;
    [SerializeField] float mainThrust = 150f;
    [SerializeField] float loadLevelDelay = 2f;

    [SerializeField] ParticleSystem engineParticles;
    [SerializeField] ParticleSystem collisionParticles;
    [SerializeField] ParticleSystem deathParticles;
    [SerializeField] ParticleSystem victoryParticles;
    
    enum State { Alive, Dying, Transcending }
    State state = State.Alive;

    public AudioClip[] smashes;
    public AudioClip landing;
    public AudioClip nextLevel;
    public float battery = 100f;

    private Rigidbody rigidBody;
    private AudioSource audioSource;
    private Animator droneAnimation;
    private AudioClip smash;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        droneAnimation = GetComponent<Animator>();

        droneAnimation.gameObject.GetComponent<Animator>().enabled = false;
    }

    private void Update()
    {
        if (state == State.Alive)
        {
            Thrust();
            Rotate();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        float knockBackForce = 1200f;

        if (state != State.Alive)
        {
            return;
        }

        switch (other.gameObject.tag)
        {
            case "Friendly":
                Debug.Log("Safe");
                break;
            case "Battery":
                Debug.Log("Charged");
                break;
            case "Pickup":
                Debug.Log("The item is picked up!");
                break;
            case "Finish":
                Debug.Log("Level is done");
                state = State.Transcending;
                audioSource.PlayOneShot(nextLevel, 5);
                victoryParticles.Play();
                Physics.gravity = new Vector3(0, -9.81f, 0);
                Invoke("LoadNextLevel", loadLevelDelay);
                break;
            default:
                DeadState();
                Vector3 direction = other.contacts[0].point - transform.position;
                direction = -direction.normalized;
                collisionParticles.Play();
                GetComponent<Rigidbody>().AddForce(direction * knockBackForce);
                break;
        }
    }

    //Vertical movement control of drone (boosting to the up only, with a sound)
    public void Thrust()
    {
        if (CrossPlatformInputManager.GetButton("Thrust"))
        {
            droneAnimation.gameObject.GetComponent<Animator>().enabled = true;
            transform.GetChild(10).gameObject.SetActive(false);
            transform.GetChild(11).gameObject.SetActive(true);
            rigidBody.AddRelativeForce(Vector3.up * mainThrust);

            engineParticles.Play();
        }        
    }

    //Horizontal incline controling of drone
    public void Rotate()
    {
        rigidBody.freezeRotation = true;
        float rotationThisFrame = rcsThrust * Time.deltaTime;

        if (CrossPlatformInputManager.GetButton("LeftRotate"))
        {
            transform.Rotate(Vector3.left * rotationThisFrame);
        }

        if (CrossPlatformInputManager.GetButton("RightRotate"))
        {
            transform.Rotate(Vector3.right * rotationThisFrame);
        }        
    }

    //Dead status after collision with obstacles: stops the engine sound and animation, plays the random smash sound and turns off the freezing of all positions and rotations
    private void DeadState()
    {
        state = State.Dying;
        deathParticles.Play();
        audioSource.Stop();
        smash = smashes[UnityEngine.Random.Range(0, smashes.Length)];
        audioSource.PlayOneShot(smash, 0.5f);
        StartCoroutine(DeactivateDrone());
        rigidBody.constraints = RigidbodyConstraints.None;
        //rigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
        Invoke("RestartToFirstLevel", loadLevelDelay);
    }

    //After achievement the goal of the current level, starts the loading of the next one consistently
    private void LoadNextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    //Returning the player to the very beginning of the game despite of the current progression
    private void RestartToFirstLevel()
    {
        SceneManager.LoadScene(0);
        Physics.gravity = new Vector3(0, -9.81f, 0);
    }

    //Numerator for delayed playing of death sound right after the collision knocking back (to avoid the synchronized playback of two those sounds at once)
    IEnumerator DeactivateDrone()
    {
        Physics.gravity = new Vector3(0, -20, 0);
        transform.GetChild(11).gameObject.SetActive(false);
        transform.GetChild(10).gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        audioSource.PlayOneShot(landing, 3f);
        droneAnimation.gameObject.GetComponent<Animator>().enabled = false;
    }
}

