using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{
    [SerializeField]    float rcsThrust = 100f;
    [SerializeField]    float mainThrust = 50f;

    enum State { Alive, Dying, Transcending }
    State state = State.Alive;
    public AudioClip[] smashes;
    public AudioClip lifting;
    public AudioClip landing;

    private Rigidbody rigidBody;
    private AudioSource audioSource;
    private Animator droneAnimation;
    private AudioClip smash;
    private bool alreadyLaunched = false;

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
                Invoke("LoadNextLevel", 1f);
                break;
            default:
                DeadState();
                Vector3 direction = other.contacts[0].point - transform.position;
                direction = -direction.normalized;
                GetComponent<Rigidbody>().AddForce(direction * knockBackForce);
                break;
        }
    }
       
    //Vertical movement control of drone (boosting to the up only, with a sound)
    private void Thrust()
    {
        if (Input.GetKey(KeyCode.Space) && state == State.Alive)
        {
            if (alreadyLaunched == false)
            {
                droneAnimation.gameObject.GetComponent<Animator>().enabled = true;
                alreadyLaunched = true;
            }
            else
            {
                rigidBody.AddRelativeForce(Vector3.up * mainThrust);
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(lifting, 3);
                }
            }            
        }        
    }

    //Horizontal incline controling of drone
    private void Rotate()
    {
        rigidBody.freezeRotation = true;
        
        float rotationThisFrame = rcsThrust * Time.deltaTime;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {            
            transform.Rotate(Vector3.left * rotationThisFrame);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.right * rotationThisFrame);
        }
    }

    //Dead status after collision with obstacles: stops the engine sound and animation, plays the random smash sound and turns off the freezing of all positions and rotations
    private void DeadState()
    {
        state = State.Dying;
        audioSource.Stop();
        smash = smashes[UnityEngine.Random.Range(0, smashes.Length)];
        audioSource.PlayOneShot(smash, 0.5f);
        StartCoroutine(deactivateDrone());
        rigidBody.constraints = RigidbodyConstraints.None;
        //rigidBody.constraints = RigidbodyConstraints.FreezePositionZ;
        Invoke("RestartToFirstLevel", 1.5f);
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
    IEnumerator deactivateDrone()
    {
        Physics.gravity = new Vector3(0, -50, 0);
        yield return new WaitForSeconds(0.2f);
        audioSource.PlayOneShot(landing, 3f);
        droneAnimation.gameObject.GetComponent<Animator>().enabled = false;
    }
}
