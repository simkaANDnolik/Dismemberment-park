
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform player;
    private NavMeshAgent agent;
    private Vector3 randomDirection;
    private float changeDirectionTimer;
    private float minChange = 1f;
    private float maxChange = 5f;
    public Animator animator;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChageDirection();
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, player.position) <= 10 && Vector3.Distance(transform.position, player.position) > 3)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetBool("Walk", true);
        }
        else if (Vector3.Distance(transform.position, player.position) <= 3 && Vector3.Distance(transform.position, player.position) > 0)
        {
            agent.isStopped = true;
            animator.SetBool("Walk", false);
            animator.SetTrigger("Bite");
        }
        else
        {
            agent.isStopped = false;
            animator.SetBool("Walk", true);
            changeDirectionTimer -= Time.deltaTime;
            if (changeDirectionTimer <= 0f)
            {
                ChageDirection();
            }
            agent.isStopped = false;
            animator.SetBool("Walk", true);
            agent.SetDestination(transform.position + randomDirection);
            agent.isStopped = false;
            animator.SetBool("Walk", true);
        }
    }

    void ChageDirection()
    {
        randomDirection = Random.insideUnitSphere * 10;
        changeDirectionTimer = Random.RandomRange(minChange, maxChange);
    }
}