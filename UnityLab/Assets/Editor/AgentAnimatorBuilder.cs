using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Hero.Editor
{
    public class AgentAnimatorBuilder : EditorWindow
    {
        private const string AssetPath = "Assets/DoubleL/Demo/Anim";
        private const string ControllerFolder = "Assets/Animators";
        private const string PrefabPath = "Assets/Prefabs/Human.prefab";

        [MenuItem("Hero/Build Asset-based Animator (LR Attack)")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder))
                AssetDatabase.CreateFolder("Assets", "Animators");

            string controllerPath = $"{ControllerFolder}/Agent.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // 1. Parameters
            controller.AddParameter("VelocityX", AnimatorControllerParameterType.Float);
            controller.AddParameter("VelocityZ", AnimatorControllerParameterType.Float);
            controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Block", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // 2. 2D Blend Tree (Freeform Directional)
            BlendTree blendTree;
            AnimatorState movementState = controller.CreateBlendTreeInController("Movement", out blendTree);
            blendTree.blendType = BlendTreeType.FreeformDirectional2D;
            blendTree.blendParameter = "VelocityX";
            blendTree.blendParameterY = "VelocityZ";

            // Add Clips (Idle, Walk, Run)
            AddBTChild(blendTree, "OneHand_Up_Idle", 0, 0);
            
            // Walk (Threshold 2.0)
            AddBTChild(blendTree, "OneHand_Up_Walk_F_InPlace", 0, 2);
            AddBTChild(blendTree, "OneHand_Up_Walk_B_InPlace", 0, -2);
            AddBTChild(blendTree, "OneHand_Up_Walk_L_InPlace", -2, 0);
            AddBTChild(blendTree, "OneHand_Up_Walk_R_InPlace", 2, 0);

            // Run (Threshold 4.0)
            AddBTChild(blendTree, "OneHand_Up_Run_F_InPlace", 0, 4);
            AddBTChild(blendTree, "OneHand_Up_Run_B_InPlace", 0, -4);
            AddBTChild(blendTree, "OneHand_Up_Run_L_InPlace", -4, 0);
            AddBTChild(blendTree, "OneHand_Up_Run_R_InPlace", 4, 0);

            // 3. Combat States (Only Left and Right)
            // Attacks: 1:Left, 2:Right
            SetupState(sm, "OneHand_Up_Attack_2_InPlace", "Attack_Left", "Attack", 1, movementState);
            SetupState(sm, "OneHand_Up_Attack_1_InPlace", "Attack_Right", "Attack", 2, movementState);

            // Block / Hit / Dead
            SetupState(sm, "OneHand_Up_Shield_Block_Idle", "Block", "Block", -1, movementState);
            SetupState(sm, "Hit_F_1_InPlace", "HitStun", "Hit", -1, movementState);

            AnimatorState deadState = sm.AddState("Dead");
            // Use procedural fallback if Dead clip missing
            deadState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Dead.anim");
            AnimatorStateTransition deadT = sm.AddAnyStateTransition(deadState);
            deadT.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            deadT.duration = 0.1f;

            // 4. Update Prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                Animator anim = prefab.GetComponent<Animator>();
                if (anim == null) anim = prefab.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Hero] Animator built successfully with LR attacks at {controllerPath}");
        }

        private static void AddBTChild(BlendTree bt, string name, float x, float y)
        {
            bt.AddChild(GetClip(name), new Vector2(x, y));
        }

        private static AnimationClip GetClip(string name)
        {
            string path = $"{AssetPath}/{name}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) Debug.LogWarning($"[Hero] Clip not found: {path}");
            return clip;
        }

        private static void SetupState(AnimatorStateMachine sm, string clipName, string stateName, string trigger, int dir, AnimatorState next)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            AnimatorState state = sm.AddState(stateName);
            state.motion = clip;
            
            var transition = sm.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
            if (dir != -1) transition.AddCondition(AnimatorConditionMode.Equals, dir, "Direction");
            transition.duration = 0.1f;
            transition.canTransitionToSelf = false;

            var exit = state.AddTransition(next);
            exit.hasExitTime = true;
            exit.exitTime = 0.85f;
            exit.duration = 0.15f;
        }
    }
}
