﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SquirrelController : NetworkBehaviour
{
    //for internal referencing
    private Transform Target;
    private Transform loc;
    private LayerMask caster;
    private Animator anim;
    private SpriteRenderer rendy;
    public Transform moveSpot;

    //variables
    private readonly float FollowRange = 10;
    private readonly float PatrolRange = 3;
    private float counter; 

    private Vector2 InitialPosition;
    private Vector2 direction;

    private float PatrolSpeed, FollowSpeed, AttackSpeed;

    private Rigidbody2D rb;

    public GameObject Nut;

    void Start()
    {
        loc = transform;
        rendy = GetComponent<SpriteRenderer>();
        caster = 1 << LayerMask.NameToLayer("Player");
        anim = GetComponent<Animator>();

        InitialPosition.x = transform.position.x;
        InitialPosition.y = transform.position.y;

        PatrolSpeed = 0.5f;
        FollowSpeed = 2;
        AttackSpeed = 3;

        moveSpot.position = new Vector2(Random.Range(InitialPosition.x - PatrolRange, InitialPosition.x + PatrolRange), Random.Range(InitialPosition.y - PatrolRange, InitialPosition.y + PatrolRange));

        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        SearchForTarget();
        if (Target == null)
        {
            Patrol();
        }
        else
        {
            float distance = Vector3.Distance(gameObject.transform.position, Target.transform.position);
            if (distance > 4f)
            {
                Follow();
            }
            else
            {
                Attack();
            }

        }
        Orientation();
    }

    /***********************************
     *
     * Functions
     *
     ***********************************/

    /* SearchForTarget: looks for a player in the FollowRange
     *******************************************************/

    void SearchForTarget()
    {
        if (!isServer)
            return;

        if (Target == null)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(loc.position, FollowRange, caster);

            if (hitColliders.Length > 0)
            {
                int randomint = Random.Range(0, hitColliders.Length);

                if (hitColliders[randomint].GetComponent<PlayerController>().getHealth() <= 0)
                {
                    Target = null;
                }
                else
                {
                    Target = hitColliders[randomint].transform;
                }
            }
        }
    }

    public void PushedBack()
    {
        // pushed back
        Vector2 pushbackdirection = Target.transform.position - gameObject.transform.position;
        pushbackdirection.Normalize();
        rb.AddForce(-pushbackdirection * 5, ForceMode2D.Impulse);

    }

    //Colliding with the player will cause damage to the player
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.layer.Equals(8))
        {
            other.gameObject.GetComponent<PlayerController>().TakeDamage(GetComponent<Health>().getAttackDamage());
        }
    }

    void Orientation()
    {
        anim.SetBool("Move", true);

        Transform goTo;
        if (Target == null)
            goTo = moveSpot;
        else
            goTo = Target;

        if (Mathf.Abs(loc.position.x - goTo.position.x) > Mathf.Abs(loc.position.y - goTo.position.y))
        {
            anim.SetBool("Side", true);
            anim.SetBool("Up", false);
            anim.SetBool("Down", false);

            if ((loc.position.x - goTo.position.x) < 0)
            {
                rendy.flipX = false; // invoke the change on the Server as you already named the function
                //CmdProvideFlipStateToServer(rendy.flipX);
            }
            else if ((loc.position.x - goTo.position.x) > 0)
            {
                rendy.flipX = true; // invoke the change on the Server as you already named the function
                //CmdProvideFlipStateToServer(rendy.flipX);
            }
        }
        else if (Mathf.Abs((loc.position.x - goTo.position.x)) < Mathf.Abs((loc.position.y - goTo.position.y)))
        {
            anim.SetBool("Side", false);
            if ((loc.position.y - goTo.position.y) < 0)
            {
                anim.SetBool("Up", true);
                anim.SetBool("Down", false);
            }
            else
            {
                anim.SetBool("Up", false);
                anim.SetBool("Down", true);
            }
        }
    }

    /* Patrol: enemy walks towards random move spots
     **********************************************/

    void Patrol()
    {
        // direction of the patrol: towards the 'move spot'
        direction = Vector2.MoveTowards(transform.position, moveSpot.position, PatrolSpeed * Time.deltaTime);
        transform.position = direction;

        // if the enemy reaches the 'move spot'
        if (Vector2.Distance(transform.position, moveSpot.position) < 0.2f)
        {
            // create a new random 'move spot'
            moveSpot.position = new Vector2(Random.Range(InitialPosition.x - PatrolRange, InitialPosition.x + PatrolRange), Random.Range(InitialPosition.y - PatrolRange, InitialPosition.y + PatrolRange));
        }
    }

    /* Follow: enemy follows a player
     ********************************/

    void Follow()
    {
        if (Target != null && isServer)
        {
            // direction of the follow: towards the position of the player
            direction = Vector2.MoveTowards(new Vector2(loc.position.x, loc.position.y), Target.position, FollowSpeed * Time.deltaTime);
            transform.position = direction;
        }
    }

    /* Attack: enemy attacks a player
     *******************************/

    void Attack()
    {
        if (counter > 0)
            counter -= Time.deltaTime;
        else if (isServer)
        {
            GameObject temp = Instantiate(Nut, transform.position, transform.rotation);
            temp.GetComponent<NutController>().setDamage(GetComponent<Health>().getAttackDamage());
            NetworkServer.Spawn(temp);
            temp.GetComponent<NutController>().Shoot(Target.transform);
            counter = 2.5f;
            Target = null;
        }
            
    }

    /***********************************
     *
     * Network
     *
     ***********************************/

    [Command]
    void CmdProvideFlipStateToServer(bool state)
    {
        rendy.flipX = state; // make the change local on the server
        //RpcSendFlipState(state); // forward the change also to all clients
    }

    [ClientRpc]
    void RpcSendFlipState(bool state)
    {
        if (isLocalPlayer) return; // skip this function on the LocalPlayer because he is the one who originally invoked this
        rendy.flipX = state; // make the change local on all clients
    }

}
