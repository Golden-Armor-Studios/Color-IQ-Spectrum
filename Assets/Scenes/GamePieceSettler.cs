using UnityEngine;

public class GamePieceSettler : MonoBehaviour
{
    private const float SupportCheckDistance = 0.55f;
    private const float SettleVelocityThreshold = 0.05f;
    private const float ReleaseKickSpeed = -0.5f;

    private Rigidbody2D rigidBody;
    private int supportMask;
    private bool wasSettled;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        supportMask = Physics2D.DefaultRaycastLayers;
    }

    void FixedUpdate()
    {
        if (rigidBody == null)
        {
            return;
        }

        RaycastHit2D supportHit = Physics2D.Raycast(rigidBody.position, Vector2.down, SupportCheckDistance, supportMask);
        bool hasSupport = supportHit.collider != null;

        if (!hasSupport)
        {
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (wasSettled)
            {
                wasSettled = false;
                Vector2 velocity = rigidBody.linearVelocity;
                if (velocity.y > ReleaseKickSpeed)
                {
                    velocity.y = ReleaseKickSpeed;
                }
                rigidBody.linearVelocity = velocity;
            }
            return;
        }

        float verticalSpeed = Mathf.Abs(rigidBody.linearVelocity.y);
        if (verticalSpeed <= SettleVelocityThreshold)
        {
            rigidBody.linearVelocity = Vector2.zero;
            rigidBody.angularVelocity = 0f;
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation |
                                    RigidbodyConstraints2D.FreezePositionX |
                                    RigidbodyConstraints2D.FreezePositionY;
            wasSettled = true;
        }
        else
        {
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            wasSettled = false;
        }
    }
}
