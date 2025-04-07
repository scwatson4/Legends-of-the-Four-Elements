using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MAA
{
    public class DisplayAnimation : MonoBehaviour
    {
        public GameObject Model;
        public RuntimeAnimatorController controller;

        private Animator animator;
        private AnimatorOverrideController animatorOverrideController;
        private GameObject model;
        private Vector3 defPosition;
        private Quaternion defRotation;
        private float animTime = 0;

        public AnimationClip animClip;

        void Awake()
        {
            SetUpModel();
        }

        void Update()
        {
            animTime += Time.deltaTime;
            if(animTime > animClip.length)
                ResetAnimation();
        }

        void ChangeAnimationClip(string clip, AnimationClip animationClip)
        {
            if(animationClip != null)
                animatorOverrideController[clip] = animationClip;
        }
        void ResetAnimation()
        {
            animTime = 0;
            SetUpModel();
        }
        void SetUpModel()
        {
            if (transform.childCount > 0)
                Destroy(transform.GetChild(0).gameObject);
            
            model = Instantiate(Model, transform);

            animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            if(animatorOverrideController != null)
                Destroy(animatorOverrideController);
            animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = animatorOverrideController;
            
            animator.applyRootMotion = true;

            animator.Play("Display");

            ChangeAnimationClip("Fight_Idle", animClip);
        }
    }
}