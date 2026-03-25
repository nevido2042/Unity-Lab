using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimationGenerator : EditorWindow
{
    [MenuItem("Hero/Generate Agent Animations")]
    public static void Generate()
    {
        string animDir = "Assets/Animations";
        string controllerDir = "Assets/Animators";
        string prefabPath = "Assets/Prefabs/Human.prefab";

        if (!Directory.Exists(Application.dataPath + "/Animations")) Directory.CreateDirectory(Application.dataPath + "/Animations");
        if (!Directory.Exists(Application.dataPath + "/Animators")) Directory.CreateDirectory(Application.dataPath + "/Animators");

        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("Prefab not found!"); return; }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Animator anim = instance.GetComponent<Animator>();

        string rightShoulder = GetPath(instance.transform, anim, HumanBodyBones.RightUpperArm, "RightArm");
        string rightArm = GetPath(instance.transform, anim, HumanBodyBones.RightLowerArm, "RightForeArm");
        string spine = GetPath(instance.transform, anim, HumanBodyBones.Spine, "Spine");
        
        if (string.IsNullOrEmpty(rightShoulder)) rightShoulder = "";
        if (string.IsNullOrEmpty(rightArm)) rightArm = rightShoulder;
        if (string.IsNullOrEmpty(spine)) spine = "";

        // 1. Generate Clips
        GenerateClip(animDir, "Idle", cb => CreateIdle(cb, spine));
        GenerateClip(animDir, "Walk_Fwd", cb => CreateWalk(cb, spine, rightShoulder, 1f));
        GenerateClip(animDir, "Walk_Bwd", cb => CreateWalk(cb, spine, rightShoulder, -1f));
        GenerateClip(animDir, "Walk_Left", cb => CreateStrafe(cb, spine, rightShoulder, -1f));
        GenerateClip(animDir, "Walk_Right", cb => CreateStrafe(cb, spine, rightShoulder, 1f));
        
        GenerateClip(animDir, "Attack_Left", cb => CreateSwing(cb, rightShoulder, rightArm, new Vector3(0, 0, 90), new Vector3(0, -90, -45)));
        GenerateClip(animDir, "Attack_Right", cb => CreateSwing(cb, rightShoulder, rightArm, new Vector3(0, -90, -45), new Vector3(0, 0, 90)));
        GenerateClip(animDir, "Attack_Up", cb => CreateSwing(cb, rightShoulder, rightArm, new Vector3(0, 0, 150), new Vector3(0, 0, -30)));
        GenerateClip(animDir, "Attack_Thrust", cb => CreateThrust(cb, rightShoulder, rightArm));

        GenerateClip(animDir, "Block", cb => CreateBlock(cb, rightShoulder, rightArm));
        GenerateClip(animDir, "Hit", cb => CreateHit(cb, spine));
        GenerateClip(animDir, "Dead", cb => CreateDead(cb, spine));

        DestroyImmediate(instance);

        // 2. Controller Setup
        string controllerPath = $"{controllerDir}/Agent.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("VelocityX", AnimatorControllerParameterType.Float);
        controller.AddParameter("VelocityZ", AnimatorControllerParameterType.Float);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Block", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Blend Tree for Movement
        BlendTree blendTree;
        AnimatorState movementState = controller.CreateBlendTreeInController("Movement", out blendTree);
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "VelocityX";
        blendTree.blendParameterY = "VelocityZ";

        blendTree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Idle.anim"), new Vector2(0, 0));
        blendTree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Walk_Fwd.anim"), new Vector2(0, 5));
        blendTree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Walk_Bwd.anim"), new Vector2(0, -5));
        blendTree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Walk_Left.anim"), new Vector2(-5, 0));
        blendTree.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Walk_Right.anim"), new Vector2(5, 0));

        // Attack States
        SetupAttackState(sm, "Attack_Left", animDir, 1, movementState);
        SetupAttackState(sm, "Attack_Right", animDir, 2, movementState);
        SetupAttackState(sm, "Attack_Up", animDir, 3, movementState);
        SetupAttackState(sm, "Attack_Thrust", animDir, 4, movementState);

        // Block, Hit, Dead
        SetupTriggerState(sm, "Block", animDir, "Block", movementState);
        SetupTriggerState(sm, "HitStun", animDir, "Hit", movementState, "Hit");
        
        AnimatorState deadState = sm.AddState("Dead");
        deadState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Dead.anim");
        AnimatorStateTransition deadT = sm.AddAnyStateTransition(deadState);
        deadT.AddCondition(AnimatorConditionMode.If, 0, "Dead");

        Animator prefabAnim = prefab.GetComponent<Animator>();
        if (prefabAnim == null) prefabAnim = prefab.AddComponent<Animator>();
        prefabAnim.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(prefab);

        AssetDatabase.SaveAssets();
        Debug.Log("Enhanced Agent Animations (BlendTree & Transitions) Generated!");
    }

    static void SetupAttackState(AnimatorStateMachine sm, string stateName, string animDir, int dirVal, AnimatorState returnState)
    {
        AnimatorState state = sm.AddState(stateName);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/{stateName}.anim");
        
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t.AddCondition(AnimatorConditionMode.Equals, dirVal, "Direction");
        t.duration = 0.1f;

        AnimatorStateTransition ret = state.AddTransition(returnState);
        ret.hasExitTime = true;
        ret.exitTime = 0.8f;
        ret.duration = 0.2f;
    }

    static void SetupTriggerState(AnimatorStateMachine sm, string stateName, string animDir, string triggerName, AnimatorState returnState, string motionName = null)
    {
        AnimatorState state = sm.AddState(stateName);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/{(motionName ?? stateName)}.anim");
        
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        t.duration = 0.1f;

        AnimatorStateTransition ret = state.AddTransition(returnState);
        ret.hasExitTime = true;
        ret.exitTime = 0.8f;
        ret.duration = 0.2f;
    }

    static string GetPath(Transform root, Animator a, HumanBodyBones bone, string fallback)
    {
        if (a != null && a.isHuman) {
            Transform t = a.GetBoneTransform(bone);
            if (t != null) return AnimationUtility.CalculateTransformPath(t, root);
        }
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach(var t in all) {
            if (t.name.ToLower().Contains(fallback.ToLower())) return AnimationUtility.CalculateTransformPath(t, root);
        }
        return "";
    }

    static void GenerateClip(string dir, string name, System.Action<AnimationClip> buildClip)
    {
        string path = $"{dir}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            clip.ClearCurves();
        }
        buildClip(clip);
        EditorUtility.SetDirty(clip);
    }

    static void SetEuler(AnimationClip clip, string path, float time, Vector3 val, ref AnimationCurve cx, ref AnimationCurve cy, ref AnimationCurve cz)
    {
        cx.AddKey(time, val.x);
        cy.AddKey(time, val.y);
        cz.AddKey(time, val.z);
    }
    
    static void ApplyEuler(AnimationClip clip, string path, AnimationCurve cx, AnimationCurve cy, AnimationCurve cz)
    {
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x", cx);
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.y", cy);
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.z", cz);
    }

    static void CreateIdle(AnimationClip clip, string spine)
    {
        AnimationCurve sx=new AnimationCurve(),sy=new AnimationCurve(),sz=new AnimationCurve();
        float dur = 2.0f;
        for (float t = 0; t <= dur; t += 0.1f) {
            float breath = Mathf.Sin(t * Mathf.PI * 2 / dur) * 3f;
            SetEuler(clip, spine, t, new Vector3(breath, 0, 0), ref sx, ref sy, ref sz);
        }
        ApplyEuler(clip, spine, sx, sy, sz);
        clip.wrapMode = WrapMode.Loop;
    }

    static void CreateWalk(AnimationClip clip, string spine, string rA, float dir)
    {
        AnimationCurve sx=new AnimationCurve(),sy=new AnimationCurve(),sz=new AnimationCurve();
        AnimationCurve rx=new AnimationCurve(),ry=new AnimationCurve(),rz=new AnimationCurve();
        float dur = 1.0f;
        for (float t = 0; t <= dur; t += 0.05f) {
            float bob = Mathf.Sin(t * Mathf.PI * 2 * 2f) * 5f;
            float swing = Mathf.Cos(t * Mathf.PI * 2) * 30f * dir;
            SetEuler(clip, spine, t, new Vector3(bob, 0, 0), ref sx, ref sy, ref sz);
            SetEuler(clip, rA, t, new Vector3(swing, 0, 0), ref rx, ref ry, ref rz);
        }
        ApplyEuler(clip, spine, sx, sy, sz);
        ApplyEuler(clip, rA, rx, ry, rz);
        clip.wrapMode = WrapMode.Loop;
    }

    static void CreateStrafe(AnimationClip clip, string spine, string rA, float dir)
    {
        AnimationCurve sx=new AnimationCurve(),sy=new AnimationCurve(),sz=new AnimationCurve();
        AnimationCurve rx=new AnimationCurve(),ry=new AnimationCurve(),rz=new AnimationCurve();
        float dur = 1.0f;
        for (float t = 0; t <= dur; t += 0.05f) {
            float bob = Mathf.Sin(t * Mathf.PI * 2 * 2f) * 5f;
            SetEuler(clip, spine, t, new Vector3(0, 0, bob * dir), ref sx, ref sy, ref sz);
            SetEuler(clip, rA, t, new Vector3(10 * dir, 0, 0), ref rx, ref ry, ref rz);
        }
        ApplyEuler(clip, spine, sx, sy, sz);
        ApplyEuler(clip, rA, rx, ry, rz);
        clip.wrapMode = WrapMode.Loop;
    }

    static void CreateSwing(AnimationClip clip, string rA, string rFA, Vector3 windup, Vector3 release)
    {
        AnimationCurve ax=new AnimationCurve(),ay=new AnimationCurve(),az=new AnimationCurve();
        AnimationCurve fx=new AnimationCurve(),fy=new AnimationCurve(),fz=new AnimationCurve();
        
        SetEuler(clip, rA, 0f, Vector3.zero, ref ax, ref ay, ref az);
        SetEuler(clip, rFA, 0f, Vector3.zero, ref fx, ref fy, ref fz);
        
        SetEuler(clip, rA, 0.2f, windup, ref ax, ref ay, ref az); 
        SetEuler(clip, rFA, 0.2f, new Vector3(0, 45, 0), ref fx, ref fy, ref fz); 
        
        SetEuler(clip, rA, 0.4f, release, ref ax, ref ay, ref az); 
        SetEuler(clip, rFA, 0.4f, Vector3.zero, ref fx, ref fy, ref fz); 
        
        SetEuler(clip, rA, 0.8f, Vector3.zero, ref ax, ref ay, ref az); 
        
        ApplyEuler(clip, rA, ax, ay, az);
        ApplyEuler(clip, rFA, fx, fy, fz);
    }

    static void CreateThrust(AnimationClip clip, string rA, string rFA)
    {
        AnimationCurve ax=new AnimationCurve(),ay=new AnimationCurve(),az=new AnimationCurve();
        AnimationCurve fx=new AnimationCurve(),fy=new AnimationCurve(),fz=new AnimationCurve();
        
        SetEuler(clip, rA, 0f, Vector3.zero, ref ax, ref ay, ref az);
        SetEuler(clip, rFA, 0f, Vector3.zero, ref fx, ref fy, ref fz);
        
        SetEuler(clip, rA, 0.2f, new Vector3(-30, 0, -30), ref ax, ref ay, ref az); 
        SetEuler(clip, rFA, 0.2f, new Vector3(0, 90, 0), ref fx, ref fy, ref fz); 
        
        SetEuler(clip, rA, 0.4f, new Vector3(30, 0, 90), ref ax, ref ay, ref az);
        SetEuler(clip, rFA, 0.4f, Vector3.zero, ref fx, ref fy, ref fz);
        
        SetEuler(clip, rA, 0.8f, Vector3.zero, ref ax, ref ay, ref az);
        
        ApplyEuler(clip, rA, ax, ay, az);
        ApplyEuler(clip, rFA, fx, fy, fz);
    }

    static void CreateBlock(AnimationClip clip, string rA, string rFA)
    {
        AnimationCurve ax=new AnimationCurve(),ay=new AnimationCurve(),az=new AnimationCurve();
        AnimationCurve fx=new AnimationCurve(),fy=new AnimationCurve(),fz=new AnimationCurve();
        
        SetEuler(clip, rA, 0f, Vector3.zero, ref ax, ref ay, ref az);
        SetEuler(clip, rFA, 0f, Vector3.zero, ref fx, ref fy, ref fz);
        
        SetEuler(clip, rA, 0.1f, new Vector3(0, -90, -45), ref ax, ref ay, ref az); 
        SetEuler(clip, rFA, 0.1f, new Vector3(0, 90, 0), ref fx, ref fy, ref fz); 
        
        SetEuler(clip, rA, 1.0f, new Vector3(0, -90, -45), ref ax, ref ay, ref az); 
        SetEuler(clip, rFA, 1.0f, new Vector3(0, 90, 0), ref fx, ref fy, ref fz);
        
        ApplyEuler(clip, rA, ax, ay, az);
        ApplyEuler(clip, rFA, fx, fy, fz);
    }

    static void CreateHit(AnimationClip clip, string sp)
    {
        AnimationCurve sx=new AnimationCurve(),sy=new AnimationCurve(),sz=new AnimationCurve();
        SetEuler(clip, sp, 0f, Vector3.zero, ref sx, ref sy, ref sz);
        SetEuler(clip, sp, 0.1f, new Vector3(-45, 0, 0), ref sx, ref sy, ref sz); 
        SetEuler(clip, sp, 0.4f, Vector3.zero, ref sx, ref sy, ref sz);
        ApplyEuler(clip, sp, sx, sy, sz);
    }

    static void CreateDead(AnimationClip clip, string sp)
    {
        AnimationCurve sx=new AnimationCurve(),sy=new AnimationCurve(),sz=new AnimationCurve();
        AnimationCurve rx=new AnimationCurve(),ry=new AnimationCurve(),rz=new AnimationCurve();
        
        SetEuler(clip, sp, 0f, Vector3.zero, ref sx, ref sy, ref sz);
        SetEuler(clip, sp, 0.5f, new Vector3(60, 0, 0), ref sx, ref sy, ref sz); 
        SetEuler(clip, sp, 1.0f, new Vector3(90, 0, 0), ref sx, ref sy, ref sz); 
        
        SetEuler(clip, "", 0f, Vector3.zero, ref rx, ref ry, ref rz);
        SetEuler(clip, "", 0.5f, new Vector3(90, 0, 0), ref rx, ref ry, ref rz); 
        
        AnimationCurve py=AnimationCurve.Linear(0, 0, 0.5f, -0.9f);
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", py);

        ApplyEuler(clip, sp, sx, sy, sz);
        ApplyEuler(clip, "", rx, ry, rz);
    }
}
