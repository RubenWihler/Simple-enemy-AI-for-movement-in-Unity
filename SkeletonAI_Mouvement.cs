using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-30)]
public class SkeletonAI_Mouvement : EnemyMouvement
{

    [Header("-- Cast --")]
    public Transform groundCast_01;
    public Transform groundCast_02;
    public Transform groundCast_03;
    [Space]

    public LayerMask groundLayer;

    public Collider2D col;

    public Transform skeltonHead;

    [Header("-- Jump --")]
    public float maxJump;
    public float jumpHeight;
    public float jumpMomentum;


    [Header("-- Patrol --")]
    public Vector3[] path;

    
    public int currentPathPoint = 0;
    public bool isJumping = false;
    public bool leftPath = false;


    private Transform player;
    private float detectionReach;
    private CapsuleCollider2D playerCol;

    void Start()
    {
        detectionReach = master.enemyData.detectionReach;
        player = PlayerController.Player;
        playerCol = PlayerController.Player.gameObject.GetComponent<CapsuleCollider2D>();
        
    }

    void FixedUpdate()
    {
        Physics2D.IgnoreCollision(col, playerCol);

        RaycastHit2D hit = Physics2D.BoxCast(groundCast_01.position, master.enemyData.groundCastSize, 0f, Vector2.down, 0.2f, groundLayer);
        master.isGrounded = hit;
        master.animator.SetBool("IsGrounded", master.isGrounded);
        master.animator.SetFloat("VelocityX", master.rb.velocity.x);
        master.animator.SetFloat("VelocityY", master.rb.velocity.y);

        if (master.currentStat == EnemyAI.EnemyAIStat.Passive && !isJumping)
        {
            if (PlayerDetectioCheck(detectionReach))
            {
                master.animator.SetTrigger("Trigger");

                master.currentStat = EnemyAI.EnemyAIStat.Chassing;

                Chassing();

                return;
            }

            Patrol();
        }
        else if (master.currentStat == EnemyAI.EnemyAIStat.Chassing)
        {
            if (!PlayerDetectioCheck(detectionReach * 1.5f))
            {
                master.currentStat = EnemyAI.EnemyAIStat.Passive;
                return;

            }
            else
            {
                Chassing();

            }
            
        }

    }

    public void Patrol()
    {
        if (!leftPath)
        {
            PathCheck();

            int i = path[currentPathPoint].x - groundCast_01.position.x > 0f ? 1 : -1;

            if(WallCheck())
            {
                leftPath = true;
                return;

            }
    
            Vector2 force = new Vector2(path[currentPathPoint].x - groundCast_01.position.x, 0f).normalized;
            force = force * speed * Time.deltaTime;

            master.rb.AddForce(force);

            CheckDirection(force);
        
        }

    }

    public void Chassing()
    {
        if(!isJumping)
        {   
            WallCheck();

            Vector2 force = new Vector2(player.position.x - groundCast_01.position.x, 0f).normalized;
            force = 2f * speed * Time.deltaTime * force;

            master.rb.AddForce(force);

            CheckDirection(force);

            

        }

    }

    /*
    private void SetPlayerLookAt()
	{
        Vector3 h = skeltonHead.position;

        Vector3 lookVector = player.position - h;
        lookVector.y = h.y;
        Quaternion rot = Quaternion.LookRotation(lookVector);
        skeltonHead.rotation = Quaternion.Slerp(transform.rotation, rot, 1);
    }
    */

    public bool PlayerDetectioCheck(float reach)
    {
        if(Vector2.Distance((Vector2)PlayerController.Player.position, (Vector2)transform.position) <= reach)
        {
            return true;

        }

        return false;

    }

    public void CheckDirection(Vector2 force)
    {
        if (force.x > 0.01f)
        {
            master.direction = Vector2.right;
            master.visual.transform.localScale = master.defaultVisualScal;

        }
        else if (force.x < -0.01f)
        {
            master.direction = Vector2.left;
            master.visual.transform.localScale = new Vector3(-master.defaultVisualScal.x, master.visual.transform.localScale.y, master.visual.transform.localScale.z);
        }

    }

    protected virtual void PathCheck()
    {
        if (path[currentPathPoint].x - groundCast_01.position.x <= 2f && path[currentPathPoint].x - groundCast_01.position.x >= -2f)
        {
            if (currentPathPoint >= path.Length -1)
            {
                currentPathPoint = 0;

            }
            else
            {
                currentPathPoint++;

            }

        }

    }

    protected virtual bool WallCheck()
    {
        if (master.rb.velocity.x <= 0.2f && master.rb.velocity.x >= -0.2f)
        {
            Vector2 o = new Vector2(groundCast_01.position.x, groundCast_01.position.y + 0.4f);

            RaycastHit2D hit = Physics2D.Raycast(o, master.direction, 2f, groundLayer);
            Debug.DrawRay(o, new Vector3(2f * master.direction.x, 0f, 0f), Color.red);

            if (hit)
            {
                Debug.Log("WallCheck");
                Vector2 o2 = new Vector2(groundCast_01.position.x, groundCast_01.position.y + maxJump);

                RaycastHit2D hit2 = Physics2D.Raycast(o2, master.direction, 2f, groundLayer);

                if (hit2)
                {
                    return true;

                }
                else
                {
                    StartCoroutine(Jump());
                    return false;

                }

            }
            else
            {
                return false;

            }

        }
        else
        {
            return false;

        }
        
    }

    public virtual IEnumerator Jump()
    {
        Debug.Log("Jump");

        isJumping = true;
        bool ready = false;

        Vector2 dir = master.direction;

        float targetX = groundCast_01.position.x - (jumpMomentum * dir.x);

        while (!ready)
        {
            float f = targetX - groundCast_01.position.x;
            Debug.Log("targetX : " + targetX);

            if (f >= -0.5f && f <= 0.5f)
            {
                ready = true;

            }
            else
            {
                Vector2 force = new Vector2(-dir.x * speed * 3 * Time.deltaTime, 0f);
                master.rb.AddForce(force);

                CheckDirection(force);
            }
            
            
            yield return new WaitForSeconds(Time.deltaTime);

        }

        master.rb.AddForce(new Vector2(speed * dir.x * Time.deltaTime, 0f));
        CheckDirection(dir);

        yield return new WaitForSeconds(0.1f);

        bool hasLeftGround = false;
        bool land = false;
        
        master.rb.AddForce(new Vector2(200f * dir.x, jumpHeight), ForceMode2D.Impulse);
        
        while (!land)
        {
            if(!master.isGrounded && !hasLeftGround)
            {
                hasLeftGround = true;

            }
            else if (hasLeftGround && master.isGrounded)
            {
                land = true;
            }

            Vector2 force = new Vector2(speed * dir.x * Time.deltaTime, 0f);
            master.rb.AddForce(force);

            CheckDirection(force);

            yield return new WaitForSeconds(Time.deltaTime);

        }

        isJumping = false;
        
    }

}
