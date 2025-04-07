using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Animation
{
    public class AnythingMovementController : MonoBehaviour
    {
        Rigidbody rb;
        Animator animator;
        MovementJumpLegacyController _legacyController;

        Vector3 movement;
        Vector3 euler;
        Quaternion root = Quaternion.identity;

        //input
        public float horizontalInput = 0;
        public float verticalInput = 0;

        [Header("Speed")]
        public float maxSpeed = 3;
        public float turnSpeed = 2;
        public float jumpHeight;

        bool hasHInput;
        bool hasVInput;
        bool isWalking;
        bool isGround;
        bool doubleJump;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            _legacyController = GetComponentInChildren<MovementJumpLegacyController>();

            isGround = true;
        }

        void Update()
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");

            SetInput();

            Move();

            if (Input.GetButtonDown("Jump"))
            {
                Jump();
            }

            SetAnim();
        }

        void SetInput()
        {
            movement = transform.forward * verticalInput;
            movement.Normalize();

            euler.Set(0, horizontalInput, 0);
            euler.Normalize();

            hasHInput = !Mathf.Approximately(horizontalInput, 0f);
            hasVInput = !Mathf.Approximately(verticalInput, 0f);

            isWalking = hasHInput || hasVInput;
        }

        void Turn()
        {
            root = Quaternion.Euler((euler * turnSpeed) * Time.deltaTime);
            //rb.MoveRotation(rb.rotation * root);
            rb.rotation *= root;
        }

        void Move()
        {
            if (isWalking)
            {
                movement = movement.normalized * maxSpeed * Time.deltaTime;

                //rb.MovePosition(rb.position + movement);
                rb.linearVelocity = movement;
                Turn();
            }
        }

        void Jump()
        {
            if (isGround == true)
            {
                rb.AddForce(movement + (transform.up * jumpHeight), ForceMode.Impulse);
            }
            else if (doubleJump == true)
            {
                doubleJump = false;
                rb.AddForce(movement + (transform.up * jumpHeight), ForceMode.Impulse);
            }
        }

        void SetAnim()
        {
            /*animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsGround", isGround);*/
            _legacyController.walkThreshold = 0.1f;
            _legacyController.runThreshold = 0.8f;
            _legacyController.BlendMovementAnimationOnSpeed(rb.linearVelocity.magnitude);
        }

        void OnCollisionStay(Collision other)
        {
            if (other.collider.CompareTag("ground"))
            {
                isGround = true;
                doubleJump = true;
            }
        }

        void OnCollisionExit(Collision other)
        {
            if (other.collider.CompareTag("ground"))
            {
                isGround = false;
            }
        }
    }
}