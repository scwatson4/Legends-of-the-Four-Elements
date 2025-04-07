using AnythingWorld.Animation;
using AnythingWorld.Animation.Vehicles;
using AnythingWorld.Utilities;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    /// <summary>
    /// Plays non jumping movement animations depending on speed and type of agent.
    /// </summary>
    [System.Serializable]
    public class AnimateMovement : ActionNode
    {
        public NodeProperty<float> speed = new NodeProperty<float>();
        
        private DefaultBehaviourType _behaviourType;
        private MovementJumpLegacyController _legacyGroundAnimalAnimationController;
        private Animator _animationController;
        private VehicleAnimator _wheeledVehicleAnimationController;
        private FlyingVehicleAnimator _flyingVehicleAnimationController;
        
        private static readonly int EffortfulFlap = Animator.StringToHash("EffortfulFlap");
        private static readonly int Flap = Animator.StringToHash("Flap");
        private static readonly int InFlight = Animator.StringToHash("InFlight");
        
        /// <summary>
        /// Tries to retrieve the behaviour type and set the appropriate animation controller.
        /// </summary>
        public override void OnInit()
        {
            if (context.GameObject.TryGetComponent(out MovementDataContainer container))
            {
                _behaviourType = container.behaviourType;
                if (_behaviourType == DefaultBehaviourType.Static || _behaviourType == DefaultBehaviourType.SwimmingCreature)
                {
                    canRun = false;
                    return;
                }
            }
            else
            {
                Debug.LogWarning($"{context.GameObject.name} doesn't have MovementDataContainer component, " +
                                 "cannot get model behaviour type and animate it.");
                canRun = false;
                return;
            }

            if (!TrySetAnimationController())
            {
                Debug.LogWarning($"{context.GameObject.name} doesn't have animator controller corresponding to " +
                                 "behaviour type, cannot animate model.");
                canRun = false;
            }
        }

        protected override void OnStart(){}

        protected override void OnStop(){}

        /// <summary>
        /// Plays movement animation each frame until agent stops.
        /// </summary>
        protected override State OnUpdate()
        {
            if (!canRun)
            {
                return State.Success;
            }

            PlayMovementAnimation();
            return speed > 0 ? State.Running : State.Success;
        }

        /// <summary>
        /// Plays the appropriate movement animation based on the behaviour type.
        /// </summary>
        private void PlayMovementAnimation()
        {
            switch (_behaviourType)
            {
                case DefaultBehaviourType.GroundCreature:
                    PlayGroundAnimalAnimation();
                    break;
                case DefaultBehaviourType.GroundVehicle:
                    PlayWheeledVehicleAnimation();
                    break;
                case DefaultBehaviourType.FlyingVehicle:
                    PlayFlyingVehicleAnimation();
                    break;
                case DefaultBehaviourType.FlyingCreature:
                    PlayFlyingAnimalAnimation();
                    break;
            }
        }

        /// <summary>
        /// Plays the flying animal animation based on the speed.
        /// </summary>
        private void PlayFlyingAnimalAnimation()
        {
            if (speed > 0)
            {
                _animationController.SetBool(EffortfulFlap, true);
                _animationController.SetTrigger(Flap);
            }
            else
            {
                _animationController.SetBool(EffortfulFlap, false);
            }
        }

        /// <summary>
        /// Plays the flying vehicle animation based on the speed.
        /// </summary>
        private void PlayFlyingVehicleAnimation()
        {
            if (speed > 0.1)
            {
                _flyingVehicleAnimationController.Accelerate();
            }
            else
            {
                _flyingVehicleAnimationController.Decelerate();
            }
        }

        /// <summary>
        /// Plays the wheeled vehicle animation based on the speed.
        /// </summary>
        private void PlayWheeledVehicleAnimation()
        {
            _wheeledVehicleAnimationController.SetVelocity(speed);
        }

        /// <summary>
        /// Plays the ground animal animation based on the speed.
        /// </summary>
        private void PlayGroundAnimalAnimation()
        {
            if (_legacyGroundAnimalAnimationController)
            {
                _legacyGroundAnimalAnimationController.BlendMovementAnimationOnSpeed(speed);
            }
            else
            {
                _animationController.BlendMovementAnimationOnSpeed(speed);
            }
        }

        /// <summary>
        /// Tries to set the appropriate animation controller based on the behaviour type.
        /// </summary>
        private bool TrySetAnimationController()
        {
            switch (_behaviourType)
            {
                case DefaultBehaviourType.GroundCreature:
                    return TrySetGroundAnimalAnimationController();
                case DefaultBehaviourType.GroundVehicle:
                    return TrySetWheeledVehicleAnimationController();
                case DefaultBehaviourType.FlyingVehicle:
                    return TrySetFlyingVehicleAnimationController();
                case DefaultBehaviourType.FlyingCreature:
                    return TrySetFlyingAnimalAnimationController();
                default:
                    return false;
            }
        }

        /// <summary>
        /// Tries to set the flying animal animation controller.
        /// </summary>
        private bool TrySetFlyingAnimalAnimationController()
        {
            _animationController = context.GameObject.GetComponentInChildren<Animator>(); 

            if (_animationController)
            {
                _animationController.SetBool(InFlight, true);
                return true;
            }
            
            var flyingAnimationController = context.GameObject.GetComponentInChildren<FlyingAnimationController>();
            if (flyingAnimationController)
            {
                flyingAnimationController.Fly();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to set the flying vehicle animation controller.
        /// </summary>
        private bool TrySetFlyingVehicleAnimationController()
        {
            _flyingVehicleAnimationController = context.GameObject.GetComponentInChildren<FlyingVehicleAnimator>();
            return _flyingVehicleAnimationController;
        }

        /// <summary>
        /// Tries to set the wheeled vehicle animation controller.
        /// </summary>
        private bool TrySetWheeledVehicleAnimationController()
        {
            _wheeledVehicleAnimationController = context.GameObject.GetComponentInChildren<VehicleAnimator>();
            return _wheeledVehicleAnimationController;
        }

        /// <summary>
        /// Tries to set the ground animal animation controller.
        /// </summary>
        private bool TrySetGroundAnimalAnimationController()
        {
            _legacyGroundAnimalAnimationController = context.GameObject.GetComponentInChildren<MovementJumpLegacyController>();
            if (_legacyGroundAnimalAnimationController)
            {
                return true;
            }
            
            _animationController = context.GameObject.GetComponentInChildren<Animator>();
            if (_animationController)
            {
                return true;
            } 
            return false;
        }
    }
}
